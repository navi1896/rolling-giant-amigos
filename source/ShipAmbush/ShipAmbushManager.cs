#nullable disable
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace RollingGiant.ShipAmbush;

/// <summary>
/// Server-side coordinator for the dynamic 2.0 Ship Blackout Ambush:
/// - Only when the ship is landed on a moon (not Company Building) and the hydraulic ship door is OPEN.
/// - Only when a single living player is ALONE inside the ship looking at the Terminal or watching the ship monitors.
/// - 100% guarantees NO living player (inside or outside the ship) is looking at the ship doorway.
/// - Rolls a 10% chance (or Host presses F8 to force it for the player in the ship): spawns the Rolling Giant silently
///   in the open doorway, turns off the ship lights, waits motionless until seen, then charges in to kill them.
/// </summary>
internal sealed class ShipAmbushManager : MonoBehaviour {
    public static ShipAmbushManager Instance { get; private set; }

    private Terminal _cachedTerminal;
    private HangarShipDoor _cachedHangarDoor;

    private bool _triggeredThisRound;
    private bool _forcedAmbushPending;
    private bool _spawningAmbush;
    private bool _wasLandedLastFrame;
    private float _landedAtTime;
    private float _distractedDuration;
    private float _nextRandomRollTime;

    public static void Create() {
        if (Instance != null) return;
        var go = new GameObject("RollingGiant_ShipAmbush");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<ShipAmbushManager>();
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    private void Update() {
        if (!ShipAmbushSettings.Enabled.Value) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        var round = StartOfRound.Instance;
        var roundManager = RoundManager.Instance;
        if (round == null || roundManager == null || roundManager.currentLevel == null) return;

        // Reset state between rounds in orbit.
        if (round.inShipPhase || round.shipIsLeaving || !round.shipHasLanded) {
            if (round.inShipPhase) {
                _triggeredThisRound = false;
            }
            _forcedAmbushPending = false;
            _wasLandedLastFrame = false;
            _distractedDuration = 0f;
            return;
        }

        // Skip 71-Gordion (Company Building, level ID 3).
        if (round.currentLevelID == 3) return;

        if (!_wasLandedLastFrame) {
            _wasLandedLastFrame = true;
            _landedAtTime = Time.time;
            _distractedDuration = 0f;
            _nextRandomRollTime = Time.time + Mathf.Max(5f, ShipAmbushSettings.MinSecondsAfterLanding.Value);
        }

        // Host shortcut (F8): arms/forces the interaction for whoever is inside the ship, even if the host is
        // outside, inside the facility, dead/spectating, or flying in Ghost Mode.
        var testKey = ShipAmbushSettings.HostTestKey.Value;
        if (testKey != Key.None && Keyboard.current != null && Keyboard.current[testKey].wasPressedThisFrame) {
            _forcedAmbushPending = true;
            Plugin.Log.LogInfo("Ship Blackout Ambush armed by host (F8): waiting for open ship door + unseen doorway for the player in the ship.");
        }

        if (_spawningAmbush) return;
        if (!_forcedAmbushPending && _triggeredThisRound) return;

        // MUST only happen when the ship door is left OPEN ("solo cuando la puerta quede abierta pls").
        if (!IsShipDoorOpen(round)) {
            _distractedDuration = 0f;
            return;
        }

        // For random rolls, ship lights must be ON before the event so turning them off creates the blackout cue.
        if (!_forcedAmbushPending && (round.shipRoomLights == null || !round.shipRoomLights.areLightsOn)) {
            return;
        }

        if (!_forcedAmbushPending && CrewControl.CrewControlManager.IsActive) {
            return;
        }

        var terminal = GetTerminal();
        if (!TryFindLoneShipVictim(round, terminal, allowAnyFacingAwayWhenForced: _forcedAmbushPending, out var victim)) {
            _distractedDuration = 0f;
            return;
        }

        if (!TryFindUnseenDoorwaySpawn(round, terminal, victim, out var doorwayPos, out var inwardYaw)) {
            return;
        }

        if (_forcedAmbushPending) {
            _forcedAmbushPending = false;
            _triggeredThisRound = true;
            Plugin.Log.LogInfo($"Ship Blackout Ambush force-triggered by host (F8) for ship player: {victim.playerUsername}");
            StartCoroutine(ExecuteAmbushCoroutine(round, doorwayPos, inwardYaw, victim));
            return;
        }

        _distractedDuration += Time.deltaTime;
        if (Time.time - _landedAtTime < ShipAmbushSettings.MinSecondsAfterLanding.Value) return;
        if (_distractedDuration < ShipAmbushSettings.MinDistractedSeconds.Value) return;
        if (Time.time < _nextRandomRollTime) return;

        // Schedule next eligible check window in 5 minutes (300s) so the 10% roll happens once every 5 minutes.
        _nextRandomRollTime = Time.time + Mathf.Max(15f, ShipAmbushSettings.RollIntervalSeconds.Value);

        var chance = Mathf.Clamp01(ShipAmbushSettings.ChancePercent.Value / 100f);
        var roll = Random.value;
        if (roll >= chance) {
            return;
        }

        _triggeredThisRound = true;
        Plugin.Log.LogInfo($"Ship Blackout Ambush triggered! (roll={roll:F3} < {chance:F2}, victim={victim.playerUsername})");
        StartCoroutine(ExecuteAmbushCoroutine(round, doorwayPos, inwardYaw, victim));
    }

    private IEnumerator ExecuteAmbushCoroutine(StartOfRound round, Vector3 doorwayPos, float inwardYaw, PlayerControllerB victim) {
        _spawningAmbush = true;
        RollingGiantAI giant = null;

        try {
            if (Plugin.EnemyTypeOutside == null || RoundManager.Instance == null) {
                yield break;
            }

            var netRef = RoundManager.Instance.SpawnEnemyGameObject(
                doorwayPos,
                inwardYaw,
                -1,
                Plugin.EnemyTypeOutside);

            if (netRef.TryGet(out var netObj)) {
                giant = netObj.GetComponentInChildren<RollingGiantAI>();
                if (giant != null) {
                    giant.PrepareShipAmbushBeforeStart(doorwayPos, inwardYaw);
                }
            }

            var waited = 0f;
            while (giant == null && waited < 2f) {
                if (netRef.TryGet(out var spawnedNetObj)) {
                    giant = spawnedNetObj.GetComponentInChildren<RollingGiantAI>();
                    if (giant != null) {
                        giant.PrepareShipAmbushBeforeStart(doorwayPos, inwardYaw);
                        break;
                    }
                }
                waited += Time.deltaTime;
                yield return null;
            }
        } finally {
            if (giant == null) {
                _spawningAmbush = false;
            }
        }

        if (giant == null) {
            Plugin.Log.LogWarning("Ship Blackout Ambush could not spawn RollingGiant outside prefab.");
            yield break;
        }

        // Wait one frame so RollingGiantAI.Start() finishes initializing audio/scale, then lock ambush state.
        yield return null;
        _spawningAmbush = false;

        if (giant == null || giant.isEnemyDead || round == null || round.inShipPhase || round.shipIsLeaving) {
            yield break;
        }

        if (!IsShipDoorOpen(round)) {
            giant.DespawnAmbushGiant();
            yield break;
        }

        var targetId = victim != null ? (int)victim.playerClientId : 0;
        giant.BeginShipAmbushOnServer(doorwayPos, inwardYaw, targetId);

        // Turn OFF the ship lights AFTER the Giant is already standing motionless in the open doorway!
        if (round.shipRoomLights != null && round.shipRoomLights.areLightsOn) {
            round.shipRoomLights.SetShipLightsBoolean(false);
        }
    }

    public static bool IsShipDoorOpen(StartOfRound round) {
        if (round == null || round.hangarDoorsClosed) return false;
        if (round.shipDoorsAnimator != null && round.shipDoorsAnimator.GetBool("Closed")) return false;
        return true;
    }

    public static bool IsHostGhostBody(PlayerControllerB player) {
        if (!CrewControl.CrewControlManager.IsActive) return false;
        var local = GameNetworkManager.Instance != null ? GameNetworkManager.Instance.localPlayerController : null;
        return player != null && player == local;
    }

    public static bool IsPlayerUsingTerminal(PlayerControllerB player, Terminal terminal) {
        if (player == null || player.isPlayerDead || !player.IsSpawned) return false;

        var local = GameNetworkManager.Instance != null ? GameNetworkManager.Instance.localPlayerController : null;
        if (player == local) {
            return player.inTerminalMenu;
        }

        if (terminal == null) return false;
        if (terminal.placeableObject == null || !terminal.placeableObject.inUse) return false;
        if (!player.inSpecialInteractAnimation) return false;

        var distToTerminal = Vector3.Distance(player.transform.position, terminal.transform.position);
        return distToTerminal <= 2.7f;
    }

    private Terminal GetTerminal() {
        if (_cachedTerminal == null) {
            _cachedTerminal = FindObjectOfType<Terminal>();
        }
        return _cachedTerminal;
    }

    private HangarShipDoor GetHangarDoor() {
        if (_cachedHangarDoor == null) {
            _cachedHangarDoor = FindObjectOfType<HangarShipDoor>();
        }
        return _cachedHangarDoor;
    }

    private static bool TryFindLoneShipVictim(
        StartOfRound round,
        Terminal terminal,
        bool allowAnyFacingAwayWhenForced,
        out PlayerControllerB victim) {
        victim = null;
        if (round.allPlayerScripts == null) return false;

        // Count active living players inside the ship (ignoring the host's parked body if the host is flying in Ghost Mode).
        PlayerControllerB soleShipPlayer = null;
        var shipPlayerCount = 0;

        foreach (var p in round.allPlayerScripts) {
            if (p == null || !p.IsSpawned || !p.isPlayerControlled || p.isPlayerDead) continue;
            if (IsHostGhostBody(p)) continue;

            var inShip = p.isInHangarShipRoom
                || (round.shipInnerRoomBounds != null && round.shipInnerRoomBounds.bounds.Contains(p.transform.position + Vector3.up * 0.25f));
            if (!inShip) continue;

            shipPlayerCount++;
            soleShipPlayer = p;
        }

        if (shipPlayerCount == 0) return false;
        if (ShipAmbushSettings.OnlyWhenAloneInShip.Value && shipPlayerCount > 1) {
            return false;
        }

        var doorRef = round.shipDoorNode != null
            ? round.shipDoorNode.position
            : (round.outsideDoorPosition != null ? round.outsideDoorPosition.position : round.transform.position);

        Vector3? monitorPos = null;
        if (round.mapScreen != null) {
            monitorPos = round.mapScreen.mesh != null
                ? round.mapScreen.mesh.bounds.center
                : round.mapScreen.transform.position;
        }

        foreach (var player in round.allPlayerScripts) {
            if (player == null || !player.IsSpawned || !player.isPlayerControlled || player.isPlayerDead) continue;
            if (IsHostGhostBody(player)) continue;
            if (!player.isInHangarShipRoom) continue;
            if (ShipAmbushSettings.OnlyWhenAloneInShip.Value && player != soleShipPlayer) continue;

            // Condition A: Using the ship computer (Terminal)
            if (IsPlayerUsingTerminal(player, terminal)) {
                victim = player;
                return true;
            }

            // Condition B: Watching the ship security / radar camera monitors with back/side to the ship door
            if (monitorPos.HasValue && player.gameplayCamera != null) {
                var camPos = player.gameplayCamera.transform.position;
                var camFwd = player.gameplayCamera.transform.forward;
                var distToMonitor = Vector3.Distance(camPos, monitorPos.Value);
                if (distToMonitor <= 5.2f) {
                    var angleToMonitor = Vector3.Angle(camFwd, monitorPos.Value - camPos);
                    var angleToDoor = Vector3.Angle(camFwd, (doorRef + Vector3.up * 1.4f) - camPos);
                    if (angleToMonitor <= 38f && angleToDoor >= 85f) {
                        victim = player;
                        return true;
                    }
                }
            }

            // Condition C: If Host explicitly pressed F8 to force the ambush for the person in the ship,
            // allow triggering as soon as that person inside the ship has their back/side turned away from the doorway!
            if (allowAnyFacingAwayWhenForced && player.gameplayCamera != null) {
                var camPos = player.gameplayCamera.transform.position;
                var camFwd = player.gameplayCamera.transform.forward;
                var angleToDoor = Vector3.Angle(camFwd, (doorRef + Vector3.up * 1.4f) - camPos);
                if (angleToDoor >= 80f) {
                    victim = player;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryFindUnseenDoorwaySpawn(
        StartOfRound round,
        Terminal terminal,
        PlayerControllerB victim,
        out Vector3 doorwaySpawnPos,
        out float inwardYaw) {
        doorwaySpawnPos = Vector3.zero;
        inwardYaw = 0f;

        var hangarDoor = GetHangarDoor();
        var interiorCenter = round.middleOfShipNode != null
            ? round.middleOfShipNode.position
            : (round.shipInnerRoomBounds != null ? round.shipInnerRoomBounds.bounds.center : round.transform.position);

        var doorThreshold = round.shipDoorNode != null
            ? round.shipDoorNode.position
            : (hangarDoor != null && hangarDoor.outsideDoorPoint != null
                ? hangarDoor.outsideDoorPoint.position
                : (round.outsideDoorPosition != null ? round.outsideDoorPosition.position : interiorCenter));

        var inwardDir = interiorCenter - doorThreshold;
        inwardDir.y = 0f;
        if (inwardDir.sqrMagnitude < 0.001f) {
            inwardDir = round.transform.forward;
            inwardDir.y = 0f;
        }
        inwardDir.Normalize();
        var outwardDir = -inwardDir;
        inwardYaw = Quaternion.LookRotation(inwardDir, Vector3.up).eulerAngles.y;

        var shipFloorY = round.playerSpawnPositions != null
            && round.playerSpawnPositions.Length > 0
            && round.playerSpawnPositions[0] != null
                ? round.playerSpawnPositions[0].position.y
                : doorThreshold.y;

        var physMask = round.collidersAndRoomMaskAndDefault;
        var victimEye = victim != null && victim.gameplayCamera != null
            ? victim.gameplayCamera.transform.position
            : interiorCenter + Vector3.up * 1.6f;

        var humanoidTypeId = NavMesh.GetSettingsCount() > 0 ? NavMesh.GetSettingsByIndex(0).agentTypeID : 0;
        var filter = new NavMeshQueryFilter {
            agentTypeID = humanoidTypeId,
            areaMask = NavMesh.AllAreas
        };

        float[] outwardOffsets = { 0.85f, 0.50f, 1.25f, 0.25f, 1.60f };
        Vector3? bestCandidate = null;

        foreach (var offset in outwardOffsets) {
            var baseXZ = new Vector3(
                doorThreshold.x + outwardDir.x * offset,
                shipFloorY,
                doorThreshold.z + outwardDir.z * offset);

            var floorPos = baseXZ;
            if (Physics.Raycast(baseXZ + Vector3.up * 1.8f, Vector3.down, out var floorHit, 3.2f, physMask, QueryTriggerInteraction.Ignore)) {
                if (Mathf.Abs(floorHit.point.y - shipFloorY) > 0.85f) {
                    continue;
                }
                floorPos = floorHit.point;
            }

            if (NavMesh.SamplePosition(floorPos, out var navHit, 0.9f, filter)
                && Mathf.Abs(navHit.position.y - floorPos.y) <= 0.55f) {
                floorPos.y = navHit.position.y;
            }

            if (!IsSpotCompletelyUnseenByAllPlayers(round, terminal, floorPos)) {
                continue;
            }

            bestCandidate ??= floorPos;

            if (!Physics.Linecast(victimEye, floorPos + Vector3.up * 1.65f, physMask, QueryTriggerInteraction.Ignore)) {
                doorwaySpawnPos = floorPos;
                return true;
            }
        }

        if (bestCandidate.HasValue) {
            doorwaySpawnPos = bestCandidate.Value;
            return true;
        }

        return false;
    }

    private static bool IsSpotCompletelyUnseenByAllPlayers(StartOfRound round, Terminal terminal, Vector3 candidatePos) {
        if (round.allPlayerScripts == null) return false;

        var testFeet = candidatePos + Vector3.up * 0.4f;
        var testBody = candidatePos + Vector3.up * 1.6f;
        var testHead = candidatePos + Vector3.up * 2.6f;

        foreach (var player in round.allPlayerScripts) {
            if (player == null || !player.IsSpawned || !player.isPlayerControlled || player.isPlayerDead) continue;
            if (IsHostGhostBody(player)) continue;
            if (player.isInsideFactory) continue;

            var cam = player.gameplayCamera != null ? player.gameplayCamera.transform : player.transform;
            var toBody = testBody - cam.position;
            var dist = toBody.magnitude;

            if (dist < 2.2f) return false;

            if (IsPlayerUsingTerminal(player, terminal)) {
                if (Vector3.Angle(cam.forward, toBody) < 68f) {
                    return false;
                }
                continue;
            }

            if (player.isInHangarShipRoom || dist < 18f) {
                if (Vector3.Angle(cam.forward, toBody) < 75f
                    || Vector3.Angle(cam.forward, testHead - cam.position) < 75f
                    || Vector3.Angle(cam.forward, testFeet - cam.position) < 75f) {
                    return false;
                }
            }

            if (player.HasLineOfSightToPosition(testBody, 70f, 80)
                || player.HasLineOfSightToPosition(testHead, 70f, 80)) {
                return false;
            }
        }

        return true;
    }
}
