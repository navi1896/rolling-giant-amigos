#nullable disable
using ControlCompany.Core;
using ControlCompany.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace RollingGiant.Compatibility;

internal class RollingGiantEnemyController : EnemyAIController
{
    private RollingGiantAI rollingGiant;
    private bool holdingForceMove;

    public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
    {
        rollingGiant = GetEnemyAIFromEnemyGameObject(enemyGameObject) as RollingGiantAI;
        base.InitializeEnemyController(playerActions, enemyGameObject);
        if (rollingGiant != null)
        {
            rollingGiant.IsPlayerControlled = true;
            rollingGiant.ForceMoveWhileLookedAt = false;
            rollingGiant.SwitchToBehaviourState(1);
        }
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        if (rollingGiant == null) return;

        rollingGiant.ForceMoveWhileLookedAt = holdingForceMove;
        bool stopped = rollingGiant.IsStoppedByPlayerLook && !holdingForceMove;
        controlEnemyRotation = !stopped;

        if (!isAIControlled)
        {
            rollingGiant.PlayerControlledSpeed = stopped ? 0f : characterController.velocity.magnitude;
            bool pressedInteract = (InputKeys.CONTROLLER_USE_KEY != null && ((ButtonControl)InputKeys.CONTROLLER_USE_KEY).wasPressedThisFrame)
                || playerActions.Movement.Interact.WasPressedThisFrame();
            if (pressedInteract)
            {
                if (rollingGiant.TryTeleportOutsideFromController(out var destination))
                {
                    ((Collider)characterController).enabled = false;
                    transform.position = destination;
                    ((Collider)characterController).enabled = true;
                    if (ControlCenter.Instance != null)
                    {
                        ControlCenter.Instance.IsGhostIndoors = !rollingGiant.isOutside;
                    }
                    EnableLight(!rollingGiant.isOutside);
                }
            }
        }
    }

    public override void Move()
    {
        if (rollingGiant == null || !rollingGiant.IsStoppedByPlayerLook || holdingForceMove)
        {
            base.Move();
        }
        else
        {
            rollingGiant.PlayerControlledSpeed = 0f;
        }
    }

    public override void ToggleAIControlTrue()
    {
        if (rollingGiant != null)
        {
            rollingGiant.IsPlayerControlled = false;
            rollingGiant.PlayerControlledSpeed = 0f;
            rollingGiant.ForceMoveWhileLookedAt = false;
        }
        base.ToggleAIControlTrue();
    }

    public override void ToggleAIControlFalse()
    {
        base.ToggleAIControlFalse();
        if (rollingGiant != null)
        {
            rollingGiant.IsPlayerControlled = true;
        }
    }

    public override void DestroyAndCleanUp()
    {
        if (rollingGiant != null)
        {
            rollingGiant.IsPlayerControlled = false;
            rollingGiant.PlayerControlledSpeed = 0f;
            rollingGiant.ForceMoveWhileLookedAt = false;
        }
        base.DestroyAndCleanUp();
    }

    public override void UsePrimarySkillAction()
    {
        rollingGiant?.PlayLocalStopSound();
    }

    public override void UseSecondarySkillAction()
    {
        holdingForceMove = true;
        if (rollingGiant != null)
        {
            rollingGiant.ForceMoveWhileLookedAt = true;
        }
    }

    public override void ReleaseSecondarySkillAction()
    {
        CheckInitialized();
        if (!isAIControlled)
        {
            holdingForceMove = false;
            if (rollingGiant != null)
            {
                rollingGiant.ForceMoveWhileLookedAt = false;
            }
        }
    }

    public override string GetPrimarySkillName() => "Stop sound";

    public override string GetSecondarySkillName() => "(HOLD) Force move";
}
