#nullable disable
using ControlCompany.Core.Extentions;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class ForestGiantEnemyController : EnemyAIController
{
	private ForestGiantAI forestGiantAI;

	private const int FOREST_GIANT_DEFAULT_MODE = 0;

	private const int FOREST_GIANT_CHASE_MODE = 1;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		forestGiantAI = (ForestGiantAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		bool flag = IsEatingPlayer();
		controlEnemyRotation = !flag;
		controlEnemyMovement = !flag;
	}

	protected override void Move()
	{
		if (!IsEatingPlayer())
		{
			base.Move();
		}
	}

	protected override void UseSecondarySkillAction()
	{
	}

	protected override void ReleaseSecondarySkillAction()
	{
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}

	public bool IsEatingPlayer()
	{
		return forestGiantAI.GetFieldValue<bool>("inEatingPlayerAnimation");
	}
}



