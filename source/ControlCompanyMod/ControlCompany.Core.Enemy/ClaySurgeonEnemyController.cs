#nullable disable
using ControlCompany.Core.Extentions;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class ClaySurgeonEnemyController : EnemyAIController
{
	private ClaySurgeonAI surgeonAI;

	private bool holdingForceMove = false;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		surgeonAI = (ClaySurgeonAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		surgeonAI.SyncMasterClaySurgeonClientRpc();
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void Move()
	{
		if (IsJumpingAndSnipping() || holdingForceMove)
		{
			base.Move();
		}
	}

	private bool IsJumpingAndSnipping()
	{
		return surgeonAI.isJumping && ((EnemyAI)surgeonAI).agent.speed > 0f;
	}

	protected override void LateUpdate()
	{
		if (!isInitialized)
		{
			return;
		}
		base.LateUpdate();
		if (!isAIControlled && hasControl)
		{
			controlEnemyRotation = IsJumpingAndSnipping() || holdingForceMove;
			Vector3 val = MoveInput();
			if (val.sqrMagnitude <= 0.1f)
			{
				surgeonAI.SetFieldValue("beatTimer", surgeonAI.currentInterval / 4f);
			}
		}
	}

	protected override void UsePrimarySkillAction()
	{
	}

	public override string GetPrimarySkillName()
	{
		return "";
	}

	protected override void UseSecondarySkillAction()
	{
		holdingForceMove = true;
	}

	protected override void ReleaseSecondarySkillAction()
	{
		holdingForceMove = false;
	}

	public override string GetSecondarySkillName()
	{
		return "(HOLD) Force move";
	}
}



