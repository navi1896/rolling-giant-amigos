#nullable disable
using ControlCompany.Core.Extentions;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class FlowermanEnemyController : EnemyAIController
{
	private FlowermanAI flowermanAI;

	private const int FLOWERMAN_DEFAULT_MODE = 0;

	private const int FLOWERMAN_STAND_MODE = 1;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		flowermanAI = (FlowermanAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void Move()
	{
		if (!((EnemyAI)flowermanAI).inSpecialAnimation)
		{
			base.Move();
		}
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		controlEnemyRotation = !((EnemyAI)flowermanAI).inSpecialAnimation;
	}

	protected override void UseSecondarySkillAction()
	{
		((EnemyAI)flowermanAI).SwitchToBehaviourState(1);
	}

	protected override void ReleaseSecondarySkillAction()
	{
		((EnemyAI)flowermanAI).SwitchToBehaviourState(0);
	}

	protected override void UsePrimarySkillAction()
	{
		if (flowermanAI.carryingPlayerBody)
		{
			flowermanAI.Call("DropPlayerBody");
			flowermanAI.DropPlayerBodyServerRpc();
		}
	}

	public override string GetPrimarySkillName()
	{
		return flowermanAI.carryingPlayerBody ? "Drop body" : "";
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}
}



