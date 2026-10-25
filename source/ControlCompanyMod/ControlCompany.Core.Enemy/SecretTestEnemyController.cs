#nullable disable
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class SecretTestEnemyController : EnemyAIController
{
	private TestEnemy testEnemy;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		testEnemy = (TestEnemy)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void UseSecondarySkillAction()
	{
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}
}



