#nullable disable
using ControlCompany.Core.Extentions;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class NutcrackerEnemyController : EnemyAIController
{
	private NutcrackerEnemyAI nutcrackerEnemyAI;

	private const int NUTCRACKER_DEFAULT_MODE = 0;

	private const int NUTCRACKER_SENTRY_MODE = 1;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		nutcrackerEnemyAI = (NutcrackerEnemyAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void LateUpdate()
	{
		if (isInitialized)
		{
			base.LateUpdate();
			if (!isAIControlled && IsMoving() && ((EnemyAI)nutcrackerEnemyAI).currentBehaviourStateIndex != 0)
			{
				((EnemyAI)nutcrackerEnemyAI).SwitchToBehaviourState(0);
			}
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if (!((Object)(object)nutcrackerEnemyAI.gun == (Object)null) && !nutcrackerEnemyAI.GetFieldValue<bool>("reloadingGun"))
		{
			if (nutcrackerEnemyAI.gun.shellsLoaded > 0)
			{
				FireGun();
			}
			if (nutcrackerEnemyAI.gun.shellsLoaded <= 0)
			{
				ReloadGun();
			}
		}
	}

	protected override void UseSecondarySkillAction()
	{
		SwitchAIState(1);
	}

	protected override void ReleaseSecondarySkillAction()
	{
		SwitchAIState(0);
	}

	public override void DestroyAndCleanUp()
	{
		if (!((EnemyAI)nutcrackerEnemyAI).isEnemyDead)
		{
			Object.Destroy((Object)(object)((Component)nutcrackerEnemyAI.gun).gameObject);
		}
		base.DestroyAndCleanUp();
	}

	public override string GetPrimarySkillName()
	{
		return "Fire";
	}

	public override string GetSecondarySkillName()
	{
		return "(HOLD) Sentry mode";
	}

	private void FireGun()
	{
		nutcrackerEnemyAI.gun.gunShootAudio.volume = 0.25f;
		nutcrackerEnemyAI.FireGunServerRpc();
	}

	private void ReloadGun()
	{
		nutcrackerEnemyAI.SetFieldValue("reloadingGun", value: true);
		nutcrackerEnemyAI.ReloadGunServerRpc();
	}
}



