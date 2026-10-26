using UnityEngine;
using UnityEngine.AI;

namespace RollingGiant;

internal static class EntranceNavigationHelper {
    public static void EnsureWalkableAgentSettings(NavMeshAgent? agent, bool includeShip) {
        if (agent == null) return;

        if (NavMesh.GetSettingsCount() > 0) {
            var humanoidTypeId = NavMesh.GetSettingsByIndex(0).agentTypeID;
            if (agent.agentTypeID != humanoidTypeId) {
                var wasEnabled = agent.enabled;
                if (wasEnabled) agent.enabled = false;
                agent.agentTypeID = humanoidTypeId;
                if (wasEnabled) agent.enabled = true;
            }
        }

        var mask = agent.areaMask;
        if (mask == 0) mask = NavMesh.AllAreas;

        AddArea(ref mask, "Walkable");
        AddArea(ref mask, "SmallSpace");
        AddArea(ref mask, "MediumSpace");
        AddArea(ref mask, "Climb");
        if (includeShip) {
            AddArea(ref mask, "PlayerShip");
        }

        agent.areaMask = mask;
    }

    private static void AddArea(ref int mask, string areaName) {
        var area = NavMesh.GetAreaFromName(areaName);
        if (area >= 0 && area < 32) {
            mask |= 1 << area;
        }
    }

    public static bool TryFindPairedEntrance(
        EntranceTeleport currentSideEntrance,
        EntranceTeleport[] allEntrances,
        out EntranceTeleport? targetEntrance) {
        targetEntrance = currentSideEntrance.exitScript;
        if (targetEntrance != null && targetEntrance.isEntranceToBuilding != currentSideEntrance.isEntranceToBuilding) {
            return true;
        }

        if (currentSideEntrance.FindExitPoint() && currentSideEntrance.exitScript != null) {
            targetEntrance = currentSideEntrance.exitScript;
            return true;
        }

        foreach (var candidate in allEntrances) {
            if (candidate != null
                && candidate.entranceId == currentSideEntrance.entranceId
                && candidate.isEntranceToBuilding != currentSideEntrance.isEntranceToBuilding) {
                targetEntrance = candidate;
                return true;
            }
        }

        targetEntrance = null;
        return false;
    }

    /// <summary>
    /// Finds a safe arrival position at a paired facility entrance without snapping downward
    /// through raised exterior catwalks/grates (such as Experimentation's Main Entrance platform)
    /// onto enclosed subfloor terrain underneath.
    /// </summary>
    public static Vector3 ResolveSafeArrivalPosition(
        EntranceTeleport targetEntrance,
        NavMeshAgent? agent,
        bool allowOffMeshPlatformFallback,
        out float arrivalYaw) {
        var refTransform = targetEntrance.entrancePoint != null
            ? targetEntrance.entrancePoint
            : targetEntrance.transform;

        var rawDoorPos = refTransform.position;
        var forward = refTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) {
            forward = targetEntrance.transform.forward;
            forward.y = 0f;
        }
        if (forward.sqrMagnitude < 0.001f) {
            forward = Vector3.forward;
        }
        forward.Normalize();
        arrivalYaw = Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y;

        var physMask = StartOfRound.Instance != null
            ? StartOfRound.Instance.collidersAndRoomMaskAndDefault
            : Physics.DefaultRaycastLayers;

        // Raycast downward from just above the player teleport point so we hit the TOP of any
        // elevated catwalk / walkway first, never the terrain subfloor underneath it.
        var floorPoint = rawDoorPos;
        if (Physics.Raycast(rawDoorPos + forward * 0.6f + Vector3.up * 1.5f, Vector3.down, out var floorHit, 4.2f, physMask, QueryTriggerInteraction.Ignore)
            || Physics.Raycast(rawDoorPos + Vector3.up * 1.5f, Vector3.down, out floorHit, 4.2f, physMask, QueryTriggerInteraction.Ignore)) {
            floorPoint = floorHit.point;
        }

        var humanoidTypeId = NavMesh.GetSettingsCount() > 0
            ? NavMesh.GetSettingsByIndex(0).agentTypeID
            : (agent != null ? agent.agentTypeID : 0);
        var areaMask = agent != null && agent.areaMask != 0 ? agent.areaMask : NavMesh.AllAreas;
        AddArea(ref areaMask, "Walkable");
        AddArea(ref areaMask, "SmallSpace");
        AddArea(ref areaMask, "MediumSpace");
        AddArea(ref areaMask, "Climb");

        var filter = new NavMeshQueryFilter {
            agentTypeID = humanoidTypeId,
            areaMask = areaMask
        };

        var right = Vector3.Cross(Vector3.up, forward).normalized;
        float[] forwardSteps = { 0.5f, 1.1f, 1.8f, 2.6f, 3.6f, 4.8f };
        float[] lateralSteps = { 0f, -1.0f, 1.0f, -2.0f, 2.0f };
        var doorChestOrigin = floorPoint + forward * 0.25f + Vector3.up * 1.05f;

        foreach (var fwd in forwardSteps) {
            foreach (var lat in lateralSteps) {
                var probe = floorPoint + forward * fwd + right * lat;
                var probeFloor = probe;
                if (Physics.Raycast(probe + Vector3.up * 1.8f, Vector3.down, out var stepHit, 3.6f, physMask, QueryTriggerInteraction.Ignore)) {
                    probeFloor = stepHit.point;
                }

                // Reject if this point dropped off a ledge / below the catwalk deck.
                if (probeFloor.y < floorPoint.y - 0.85f && fwd < 2.5f) {
                    continue;
                }

                // Must have unobstructed line-of-sight from the doorway (no wall or metal floor between them).
                if (Physics.Linecast(doorChestOrigin, probeFloor + Vector3.up * 1.05f, physMask, QueryTriggerInteraction.Ignore)) {
                    continue;
                }

                if (NavMesh.SamplePosition(probeFloor, out var navHit, 1.35f, filter)) {
                    // Reject any NavMesh polygon snapped beneath the physical walkway floor!
                    if (navHit.position.y < probeFloor.y - 0.6f || navHit.position.y < floorPoint.y - 1.2f) {
                        continue;
                    }
                    if (navHit.position.y > probeFloor.y + 1.2f) {
                        continue;
                    }
                    if (Physics.Linecast(doorChestOrigin, navHit.position + Vector3.up * 1.05f, physMask, QueryTriggerInteraction.Ignore)) {
                        continue;
                    }

                    return navHit.position;
                }
            }
        }

        // If player-controlled and the physical platform right outside the door has no baked NavMesh
        // within 4.8m, keep the monster on top of the physical platform rather than burying it!
        var safePlatformSpot = floorPoint + forward * 0.8f;
        if (Physics.Raycast(safePlatformSpot + Vector3.up * 1.5f, Vector3.down, out var safeHit, 3.0f, physMask, QueryTriggerInteraction.Ignore)
            && safeHit.point.y >= floorPoint.y - 0.6f) {
            safePlatformSpot = safeHit.point;
        }

        if (allowOffMeshPlatformFallback) {
            return safePlatformSpot;
        }

        // AI fallback: find the nearest valid AI node on the destination side that is NOT under an enclosed subfloor.
        var destinationOutside = targetEntrance.isEntranceToBuilding;
        var nodes = GameObject.FindGameObjectsWithTag(destinationOutside ? "OutsideAINode" : "AINode");
        var bestDistSq = float.MaxValue;
        var bestPos = safePlatformSpot;
        var foundNode = false;

        foreach (var node in nodes) {
            if (node == null) continue;
            var nodePos = node.transform.position;
            var distSq = (nodePos - floorPoint).sqrMagnitude;
            if (distSq >= bestDistSq) continue;

            if (NavMesh.SamplePosition(nodePos, out var nodeNavHit, 4f, filter)) {
                // If outside and substantially below the entrance deck, ensure there is no catwalk roof right above it.
                if (destinationOutside
                    && nodeNavHit.position.y < floorPoint.y - 0.8f
                    && Physics.Raycast(nodeNavHit.position + Vector3.up * 0.35f, Vector3.up, 3.8f, physMask, QueryTriggerInteraction.Ignore)) {
                    continue;
                }

                bestDistSq = distSq;
                bestPos = nodeNavHit.position;
                foundNode = true;
            }
        }

        return foundNode ? bestPos : safePlatformSpot;
    }

    public static void PlaceAgentSafely(
        Transform enemyTransform,
        NavMeshAgent? agent,
        Vector3 position,
        Quaternion rotation,
        bool includeShip) {
        if (agent == null) {
            enemyTransform.SetPositionAndRotation(position, rotation);
            return;
        }

        EnsureWalkableAgentSettings(agent, includeShip);
        if (agent.enabled && agent.isOnNavMesh) {
            agent.ResetPath();
        }

        agent.enabled = false;
        enemyTransform.SetPositionAndRotation(position, rotation);

        var filter = new NavMeshQueryFilter {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        if (NavMesh.SamplePosition(position, out var warpHit, 1.5f, filter)
            && warpHit.position.y >= position.y - 0.65f
            && warpHit.position.y <= position.y + 1.2f) {
            enemyTransform.SetPositionAndRotation(warpHit.position, rotation);
            agent.enabled = true;
            if (!agent.isOnNavMesh) {
                agent.Warp(warpHit.position);
            }

            // Final subfloor guard: if Unity's NavMeshAgent still snapped downward through a raised floor, undo immediately.
            if (enemyTransform.position.y < position.y - 0.75f) {
                agent.enabled = false;
                enemyTransform.SetPositionAndRotation(position, rotation);
            } else if (agent.isOnNavMesh) {
                agent.ResetPath();
            }
        } else {
            // Stay cleanly on top of the physical platform; agent will re-enable once near same-elevation NavMesh.
            enemyTransform.SetPositionAndRotation(position, rotation);
        }
    }
}
