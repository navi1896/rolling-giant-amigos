#nullable disable
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class PufferEnemyController : EnemyAIController
{
	private PufferAI pufferAI;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		pufferAI = (PufferAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void UsePrimarySkillAction()
	{
		if (((EnemyAI)pufferAI).currentBehaviourStateIndex != 1)
		{
			SwitchAIState(1);
		}
		pufferAI.StompServerRpc();
	}

	protected override void UseSecondarySkillAction()
	{
		if (((EnemyAI)pufferAI).currentBehaviourStateIndex != 1)
		{
			SwitchAIState(1);
		}
		pufferAI.ShakeTailServerRpc();
	}

	public override string GetPrimarySkillName()
	{
		return "Stomp";
	}

	public override string GetSecondarySkillName()
	{
		return "Smoke";
	}
}



