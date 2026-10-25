#nullable disable
using System;
using ControlCompany.Core.Config;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ControlCompany.Core.Enemy;

internal class ControllableEnemy
{
	public GameObject EnemyPrefab { get; set; }

	public string EnemyName { get; set; }

	public bool OverrideEnemyPrefab { get; set; }

	public GameObject EnemyGameObject { get; set; }

	public bool OverrideEnemySpawnLocation { get; set; }

	public bool Allowlisted { get; set; }

	public Vector3 EnemySpawnLocationOverride { get; set; }

	public Func<GameObject, GameObject, EnemyController> CreateAndAttachEnemyControllerFunc { get; set; }

	public static ControllableEnemy FromSpawnableEnemy(SpawnableEnemyWithRarity spawnableEnemy)
	{
		try
		{
			GameObject val = spawnableEnemy?.enemyType?.enemyPrefab ?? null;
			if ((Object)(object)val == (Object)null)
			{
				return null;
			}
			EnemyAI componentInChildren = val.GetComponentInChildren<EnemyAI>();
			string enemyName = (((Object)(object)componentInChildren == (Object)null) ? spawnableEnemy.enemyType.enemyName : componentInChildren.GetEnemyName());
			return new ControllableEnemy
			{
				EnemyName = enemyName,
				EnemyPrefab = val,
				OverrideEnemyPrefab = false,
				OverrideEnemySpawnLocation = false,
				Allowlisted = componentInChildren.IsEnemyAllowlisted(),
				CreateAndAttachEnemyControllerFunc = delegate(GameObject enemyGameObject, GameObject enemyController)
				{
					EnemyAI componentInChildren2 = enemyGameObject.GetComponentInChildren<EnemyAI>();
					return componentInChildren2.GetAndAttachEnemyController(enemyController);
				}
			};
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static ControllableEnemy FromEnemyType(EnemyType enemyType)
	{
		try
		{
			GameObject val = enemyType?.enemyPrefab ?? null;
			if ((Object)(object)val == (Object)null) return null;
			EnemyAI componentInChildren = val.GetComponentInChildren<EnemyAI>();
			string enemyName = (((Object)(object)componentInChildren == (Object)null) ? enemyType.enemyName : componentInChildren.GetEnemyName());
			return new ControllableEnemy
			{
				EnemyName = enemyName,
				EnemyPrefab = val,
				OverrideEnemyPrefab = false,
				OverrideEnemySpawnLocation = false,
				Allowlisted = ((Object)(object)componentInChildren == (Object)null || componentInChildren.IsEnemyAllowlisted()),
				CreateAndAttachEnemyControllerFunc = delegate(GameObject enemyGameObject, GameObject enemyController)
				{
					EnemyAI componentInChildren2 = enemyGameObject.GetComponentInChildren<EnemyAI>();
					return componentInChildren2.GetAndAttachEnemyController(enemyController);
				}
			};
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static ControllableEnemy FromEnemyAI(EnemyAI enemyAI)
	{
		GameObject enemyPrefab = enemyAI.enemyType.enemyPrefab;
		string enemyName = (((Object)(object)enemyAI == (Object)null) ? enemyAI.enemyType.enemyName : enemyAI.GetEnemyName());
		return new ControllableEnemy
		{
			EnemyName = enemyName,
			EnemyPrefab = enemyPrefab,
			OverrideEnemyPrefab = false,
			OverrideEnemySpawnLocation = false,
			Allowlisted = enemyAI.IsEnemyAllowlisted(),
			CreateAndAttachEnemyControllerFunc = delegate(GameObject enemyGameObject, GameObject enemyController)
			{
				EnemyAI componentInChildren = enemyGameObject.GetComponentInChildren<EnemyAI>();
				return componentInChildren.GetAndAttachEnemyController(enemyController);
			}
		};
	}
}



