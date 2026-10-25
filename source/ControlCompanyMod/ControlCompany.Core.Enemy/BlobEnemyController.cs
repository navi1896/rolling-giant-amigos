#nullable disable
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class BlobEnemyController : EnemyAIController
{
	private BlobAI blobAI;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		blobAI = (BlobAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
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



