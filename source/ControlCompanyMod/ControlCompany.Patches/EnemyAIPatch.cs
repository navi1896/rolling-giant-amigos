#nullable disable
using ControlCompany.Core;
using HarmonyLib;
using UnityEngine;

namespace ControlCompany.Patches;

[HarmonyPatch(typeof(EnemyAI))]
internal class EnemyAIPatch
{
	[HarmonyPatch("ChangeOwnershipOfEnemy")]
	[HarmonyPrefix]
	private static bool ChangeOwnershipOfEnemyPrefix(EnemyAI __instance, ref ulong newOwnerClientId)
	{
		if ((Object)(object)ControlCenter.Instance != (Object)null && ControlCenter.Instance.CurrentMode == ControlCenter.Mode.ENEMY)
		{
			if ((Object)(object)ControlCenter.Instance.CurrentControlledEnemy != (Object)null)
			{
				ulong actualClientId = ControlCenter.Instance.GetHostPlayer().actualClientId;
				bool flag = actualClientId == newOwnerClientId || ControlCenter.Instance.CurrentControlledEnemy.IsAIControlled;
				EnemyController currentControlledEnemy = ControlCenter.Instance.CurrentControlledEnemy;
				if (currentControlledEnemy is EnemyAIController enemyAIController)
				{
					flag = flag || (Object)(object)enemyAIController.EnemyAI != (Object)(object)__instance;
				}
				return flag;
			}
			return true;
		}
		return true;
	}

	[HarmonyPatch("ChangeEnemyOwnerServerRpc")]
	[HarmonyPrefix]
	private static bool ChangeEnemyOwnerServerRpcPrefix(EnemyAI __instance, ref ulong clientId)
	{
		if ((Object)(object)ControlCenter.Instance != (Object)null && ControlCenter.Instance.CurrentMode == ControlCenter.Mode.ENEMY)
		{
			if ((Object)(object)ControlCenter.Instance.CurrentControlledEnemy != (Object)null)
			{
				ulong actualClientId = ControlCenter.Instance.GetHostPlayer().actualClientId;
				bool flag = actualClientId == clientId || ControlCenter.Instance.CurrentControlledEnemy.IsAIControlled;
				EnemyController currentControlledEnemy = ControlCenter.Instance.CurrentControlledEnemy;
				if (currentControlledEnemy is EnemyAIController enemyAIController)
				{
					flag = flag || (Object)(object)enemyAIController.EnemyAI != (Object)(object)__instance;
				}
				return flag;
			}
			return true;
		}
		return true;
	}
}



