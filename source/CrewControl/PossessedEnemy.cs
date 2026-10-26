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
            // No usable NavMesh agent: plain transform movement.
            Enemy.transform.position += direction.normalized * (speed * Time.deltaTime);
            Enemy.transform.rotation = Quaternion.Slerp(
                Enemy.transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
            if (_rollingGiant != null) _rollingGiant.PlayerControlledSpeed = speed;
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
            var point = entrance.entrancePoint != null ? entrance.entrancePoint.position : entrance.transform.position;
            var distance = Vector3.Distance(Enemy.transform.position, point);
            if (distance < best) {
                best = distance;
                nearest = entrance;
            }
        }
        if (nearest == null) return false;

        var target = nearest.exitScript;
        if (target == null) {
            foreach (var candidate in entrances) {
                if (candidate != null && candidate.entranceId == nearest.entranceId
                    && candidate.isEntranceToBuilding != nearest.isEntranceToBuilding) {
                    target = candidate;
                    break;
                }
            }
        }
        if (target == null) return false;

        var arrival = target.entrancePoint != null ? target.entrancePoint.position : target.transform.position;
        if (NavMesh.SamplePosition(arrival, out var hit, 6f, NavMesh.AllAreas)) arrival = hit.position;

        var agent = Enemy.agent;
        if (agent != null && agent.enabled) agent.Warp(arrival);
        else Enemy.transform.position = arrival;
        Enemy.serverPosition = arrival;
        Enemy.SetEnemyOutside(target.isEntranceToBuilding);
        return true;
    }
}
