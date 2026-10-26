#nullable disable
using UnityEngine;
using UnityEngine.AI;

namespace RollingGiant.CrewControl;

/// <summary>
/// Steers a host-owned enemy from player input. The enemy keeps running its own AI scripts (so animations,
/// sounds and attacks keep working); every frame we override where its NavMeshAgent is heading.
/// </summary>
internal sealed class PossessedEnemy {
    public readonly EnemyAI Enemy;
    public readonly string DisplayName;
    public readonly float CamDistance;
    public readonly float CamHeight;

    public bool AiControlled { get; private set; }

    private readonly RollingGiantAI _rollingGiant;
    private bool _forceMove;
    private float _walkSpeed = 4f;
    private float _runSpeed = 8f;

    public bool IsAlive => Enemy != null && !Enemy.isEnemyDead;
    public bool SupportsForceMove => _rollingGiant != null;
    public bool SupportsStopSound => _rollingGiant != null;

    public PossessedEnemy(EnemyAI enemy, string displayName) {
        Enemy = enemy;
        DisplayName = displayName;
        _rollingGiant = enemy as RollingGiantAI;

        var renderers = enemy.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0) {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            CamDistance = Mathf.Clamp(bounds.size.magnitude * 1.3f, 4f, 14f);
            CamHeight = Mathf.Clamp(bounds.size.y * 0.7f, 1.5f, 6f);
        } else {
            CamDistance = 5f;
            CamHeight = 2.5f;
        }

        if (_rollingGiant != null) {
            _walkSpeed = 6f;
            _runSpeed = 10f;
        }

        SetAiControlled(false);
    }

    public void SetAiControlled(bool value) {
        AiControlled = value;
        if (_rollingGiant != null) {
            _rollingGiant.IsPlayerControlled = !value;
            _rollingGiant.ForceMoveWhileLookedAt = false;
            _rollingGiant.PlayerControlledSpeed = 0f;
            if (!value) _rollingGiant.SwitchToBehaviourState(1);
        }
        if (value) _forceMove = false;
    }

    public void SetForceMove(bool value) {
        _forceMove = value;
        if (_rollingGiant != null) _rollingGiant.ForceMoveWhileLookedAt = value;
    }

    public void PrimaryAction() {
        if (_rollingGiant != null) _rollingGiant.PlayLocalStopSound();
    }

    public void Release() {
        if (_rollingGiant != null) {
            _rollingGiant.IsPlayerControlled = false;
            _rollingGiant.ForceMoveWhileLookedAt = false;
            _rollingGiant.PlayerControlledSpeed = 0f;
        }
    }

    /// <summary>Called every LateUpdate (after the enemy's own Update) with a world-space direction.</summary>
    public void Steer(Vector3 direction, bool sprint) {
        if (!IsAlive || AiControlled) return;

        var agent = Enemy.agent;
        var stoppedByLook = _rollingGiant != null && _rollingGiant.IsStoppedByPlayerLook && !_forceMove;
        var wantsMove = direction.sqrMagnitude > 0.01f && !stoppedByLook;
        var speed = sprint ? _runSpeed : _walkSpeed;

        // The AI's own destination handling must not fight with ours.
        Enemy.moveTowardsDestination = false;
        Enemy.movingTowardsTargetPlayer = false;

        if (agent != null && !agent.enabled) {
            EntranceNavigationHelper.EnsureWalkableAgentSettings(agent, includeShip: true);
            var filter = new NavMeshQueryFilter {
                agentTypeID = agent.agentTypeID,
                areaMask = agent.areaMask
            };
            if (NavMesh.SamplePosition(Enemy.transform.position, out var reattachHit, 1.1f, filter)
                && Mathf.Abs(reattachHit.position.y - Enemy.transform.position.y) < 0.65f) {
                Enemy.transform.position = reattachHit.position;
                agent.enabled = true;
                if (!agent.isOnNavMesh) agent.Warp(reattachHit.position);
            }
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh) {
            agent.isStopped = false;
            if (wantsMove) {
                agent.speed = speed;
                agent.acceleration = 60f;
                agent.angularSpeed = 360f;
                agent.SetDestination(agent.transform.position + direction.normalized * 3f);
            } else {
                agent.speed = 0f;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
            }
            if (_rollingGiant != null) {
                _rollingGiant.PlayerControlledSpeed = stoppedByLook ? 0f : agent.velocity.magnitude;
            }
        } else if (wantsMove) {
            // Grounded physical movement when temporarily off-NavMesh (e.g. raised entrance platforms).
            var moveDir = direction.normalized;
            var physMask = StartOfRound.Instance != null
                ? StartOfRound.Instance.collidersAndRoomMaskAndDefault
                : Physics.DefaultRaycastLayers;
            var step = moveDir * (speed * Time.deltaTime);
            var currentPos = Enemy.transform.position;

            if (!Physics.Raycast(currentPos + Vector3.up * 1.0f, moveDir, step.magnitude + 0.45f, physMask, QueryTriggerInteraction.Ignore)) {
                var nextPos = currentPos + step;
                if (Physics.Raycast(nextPos + Vector3.up * 1.4f, Vector3.down, out var groundHit, 3.6f, physMask, QueryTriggerInteraction.Ignore)
                    && groundHit.point.y >= currentPos.y - 1.5f) {
                    nextPos.y = groundHit.point.y;
                }
                Enemy.transform.position = nextPos;
                Enemy.serverPosition = nextPos;
            }

            Enemy.transform.rotation = Quaternion.Slerp(
                Enemy.transform.rotation, Quaternion.LookRotation(moveDir), 8f * Time.deltaTime);
            if (_rollingGiant != null) _rollingGiant.PlayerControlledSpeed = speed;
        } else if (_rollingGiant != null) {
            _rollingGiant.PlayerControlledSpeed = 0f;
        }
    }

    /// <summary>Moves the monster through the nearest facility entrance, if one is close enough.</summary>
    public bool TryUseEntrance() {
        if (!IsAlive) return false;

        if (_rollingGiant != null) {
            return _rollingGiant.TryTeleportOutsideFromController(out _);
        }

        var entrances = Object.FindObjectsOfType<EntranceTeleport>();
        EntranceTeleport nearest = null;
        var best = 6f;
        foreach (var entrance in entrances) {
            if (entrance == null) continue;
            if (entrance.isEntranceToBuilding != Enemy.isOutside) continue;
            var point = entrance.entrancePoint != null ? entrance.entrancePoint.position : entrance.transform.position;
            var distance = Mathf.Min(
                Vector3.Distance(Enemy.transform.position, point),
                Vector3.Distance(Enemy.transform.position, entrance.transform.position));
            if (distance < best) {
                best = distance;
                nearest = entrance;
            }
        }
        if (nearest == null) return false;

        if (!EntranceNavigationHelper.TryFindPairedEntrance(nearest, entrances, out var target) || target == null) {
            return false;
        }

        var arrival = EntranceNavigationHelper.ResolveSafeArrivalPosition(
            target,
            Enemy.agent,
            allowOffMeshPlatformFallback: true,
            out var arrivalYaw);
        var rotation = Quaternion.Euler(0f, arrivalYaw, 0f);

        Enemy.SetEnemyOutside(target.isEntranceToBuilding);
        EntranceNavigationHelper.PlaceAgentSafely(Enemy.transform, Enemy.agent, arrival, rotation, includeShip: true);
        Enemy.serverPosition = Enemy.transform.position;
        return true;
    }
}
