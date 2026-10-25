#nullable disable
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class SandWormEnemyController : EnemyAIController
{
	private SandWormAI sandWormAI;

	private bool usedEmerge = false;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		sandWormAI = (SandWormAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void Move()
	{
		if (!IsEmerged() && !usedEmerge)
		{
			base.Move();
		}
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		controlEnemyRotation = !IsEmerged() && !usedEmerge;
		if (IsEmerged())
		{
			usedEmerge = true;
		}
	}

	protected override void UseSecondarySkillAction()
	{
		if (!IsEmerged() && !usedEmerge)
		{
			usedEmerge = true;
			sandWormAI.StartEmergeAnimation();
		}
	}

	public override string GetSecondarySkillName()
	{
		return usedEmerge ? "" : "Emerge";
	}

	private bool IsEmerged()
	{
		return sandWormAI.inEmergingState || sandWormAI.emerged;
	}
}



