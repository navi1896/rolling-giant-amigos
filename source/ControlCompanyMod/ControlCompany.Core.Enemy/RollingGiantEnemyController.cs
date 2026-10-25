#nullable disable
using RollingGiant;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using ControlCompany.Core.Input;

namespace ControlCompany.Core.Enemy;

internal class RollingGiantEnemyController : EnemyAIController
{
	private RollingGiantAI rollingGiant;

	private bool holdingForceMove = false;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		rollingGiant = (RollingGiantAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
		if (rollingGiant != null)
		{
			rollingGiant.IsPlayerControlled = true;
			rollingGiant.ForceMoveWhileLookedAt = false;
			rollingGiant.SwitchToBehaviourState(1);
		}
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		if (rollingGiant == null)
		{
			return;
		}
		rollingGiant.ForceMoveWhileLookedAt = holdingForceMove;
		bool stopped = rollingGiant.IsStoppedByPlayerLook && !holdingForceMove;
		controlEnemyRotation = !stopped;
		if (!isAIControlled)
		{
			rollingGiant.PlayerControlledSpeed = stopped ? 0f : characterController.velocity.magnitude;
			if (((ButtonControl)InputKeys.CONTROLLER_USE_KEY).wasPressedThisFrame)
			{
				if (rollingGiant.TryTeleportOutsideFromController(out var destination))
				{
					((Collider)characterController).enabled = false;
					((Component)this).transform.position = destination;
					((Collider)characterController).enabled = true;
					ControlCenter.Instance.IsGhostIndoors = !rollingGiant.isOutside;
					EnableLight(!rollingGiant.isOutside);
				}
			}
		}
	}

	protected override void Move()
	{
		if (rollingGiant == null || !rollingGiant.IsStoppedByPlayerLook || holdingForceMove)
		{
			base.Move();
		}
		else if (rollingGiant != null)
		{
			rollingGiant.PlayerControlledSpeed = 0f;
		}
	}

	protected override void ToggleAIControlTrue()
	{
		if (rollingGiant != null)
		{
			rollingGiant.IsPlayerControlled = false;
			rollingGiant.PlayerControlledSpeed = 0f;
			rollingGiant.ForceMoveWhileLookedAt = false;
		}
		base.ToggleAIControlTrue();
	}

	protected override void ToggleAIControlFalse()
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

	protected override void UsePrimarySkillAction()
	{
		rollingGiant?.PlayLocalStopSound();
	}

	protected override void UseSecondarySkillAction()
	{
		holdingForceMove = true;
		if (rollingGiant != null)
		{
			rollingGiant.ForceMoveWhileLookedAt = true;
		}
	}

	protected override void ReleaseSecondarySkillAction()
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

	public override string GetPrimarySkillName()
	{
		return "Stop sound";
	}

	public override string GetSecondarySkillName()
	{
		return "(HOLD) Force move";
	}
}
