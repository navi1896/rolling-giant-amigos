#nullable disable
using ControlCompany.Core.Extentions;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class SpringManEnemyController : EnemyAIController
{
	private SpringManAI springMan;

	private float initialWalkSpeed;

	private float initialRunSpeed;

	private bool holdingForceMove = false;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		springMan = (SpringManAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
		initialWalkSpeed = base.WalkSpeed;
		initialRunSpeed = base.RunSpeed;
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		bool flag = springMan.GetFieldValue<bool>("hasStopped") || springMan.GetFieldValue<bool>("stoppingMovement");
		controlEnemyRotation = !flag || holdingForceMove;
		float fieldValue = springMan.GetFieldValue<float>("currentAnimSpeed");
		base.WalkSpeed = initialWalkSpeed * (1f + fieldValue / 6f);
		base.RunSpeed = initialRunSpeed * (1f + fieldValue / 6f);
	}

	protected override void Move()
	{
		if ((!springMan.GetFieldValue<bool>("hasStopped") && !springMan.GetFieldValue<bool>("stoppingMovement")) || holdingForceMove)
		{
			base.Move();
		}
	}

	protected override void UseSecondarySkillAction()
	{
		holdingForceMove = true;
	}

	protected override void ReleaseSecondarySkillAction()
	{
		CheckInitialized();
		if (!isAIControlled)
		{
			holdingForceMove = false;
		}
	}

	public override string GetSecondarySkillName()
	{
		return "(HOLD) Force move";
	}
}



