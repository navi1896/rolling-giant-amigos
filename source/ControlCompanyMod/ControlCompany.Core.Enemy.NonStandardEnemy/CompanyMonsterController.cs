#nullable disable
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ControlCompany.Core.Enemy.NonStandardEnemy;

internal class CompanyMonsterController : EnemyController
{
	private DepositItemsDesk deskMonster;

	private float attackCooldownTimer = 0f;

	private float attackCooldownTime = 8f;

	private bool canAttack = true;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		if (!isInitialized)
		{
			Initialize(playerActions, CameraMode.THIRD_PERSON);
			deskMonster = enemyGameObject.GetComponentInChildren<DepositItemsDesk>();
			if ((Object)(object)deskMonster == (Object)null)
			{
				throw new Exception("Company desk gameobject does not have DepositItemsDesk script attached.");
			}
			canMove = false;
			canJump = false;
			controlEnemyRotation = false;
			controlEnemyMovement = false;
			EnableAIControl(enabled: false);
		}
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		if (base.IsInitialized && !canAttack)
		{
			attackCooldownTime += Time.deltaTime;
			if (attackCooldownTime >= attackCooldownTimer)
			{
				attackCooldownTime = 0f;
				canAttack = true;
			}
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if (canAttack)
		{
			canAttack = false;
			deskMonster.AttackPlayersServerRpc();
		}
	}

	public override string GetPrimarySkillName()
	{
		return "Tentacle attack";
	}

	protected override bool ShouldDestroyEnemyGameObjectOnCleanUp()
	{
		return false;
	}
}



