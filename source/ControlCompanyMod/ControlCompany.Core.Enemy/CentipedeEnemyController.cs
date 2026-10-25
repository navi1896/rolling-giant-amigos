#nullable disable
using ControlCompany.Core.Extentions;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class CentipedeEnemyController : EnemyAIController
{
	private CentipedeAI centipedeAI;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		centipedeAI = (CentipedeAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		bool flag = IsClingingToSomething();
		controlEnemyRotation = !flag;
		controlEnemyMovement = !flag;
	}

	protected override void Move()
	{
		if (!IsClingingToSomething())
		{
			base.Move();
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if (((EnemyAI)centipedeAI).currentBehaviourStateIndex == 1)
		{
			SwitchAIState(2);
		}
	}

	protected override void UseSecondarySkillAction()
	{
		if (!IsClingingToSomething())
		{
			centipedeAI.Call("RaycastToCeiling");
			SwitchAIState(1);
		}
	}

	public bool IsClingingToSomething()
	{
		return (Object)(object)centipedeAI.clingingToPlayer != (Object)null || ((EnemyAI)centipedeAI).inSpecialAnimation || centipedeAI.GetFieldValue<bool>("clingingToDeadBody") || centipedeAI.GetFieldValue<bool>("clingingToCeiling") || centipedeAI.GetFieldValue<bool>("startedCeilingAnimationCoroutine") || centipedeAI.GetFieldValue<bool>("inDroppingOffPlayerAnim");
	}

	public override string GetPrimarySkillName()
	{
		return "Drop";
	}

	public override string GetSecondarySkillName()
	{
		return "Attach to ceiling";
	}
}



