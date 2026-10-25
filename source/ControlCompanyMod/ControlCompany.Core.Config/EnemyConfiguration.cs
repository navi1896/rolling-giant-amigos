#nullable disable
using System;
using System.Collections.Generic;
using ControlCompany.Core.Enemy;
using ControlCompany.Core.Extentions;
using RollingGiant;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ControlCompany.Core.Config;

internal static class EnemyConfiguration
{
	public static HashSet<Type> ALLOWLISTED_ENEMIES = new HashSet<Type>
	{
		typeof(BlobAI),
		typeof(NutcrackerEnemyAI),
		typeof(HoarderBugAI),
		typeof(CrawlerAI),
		typeof(PufferAI),
		typeof(CentipedeAI),
		typeof(JesterAI),
		typeof(ForestGiantAI),
		typeof(BaboonBirdAI),
		typeof(FlowermanAI),
		typeof(MaskedPlayerEnemy),
		typeof(SpringManAI),
		typeof(SandWormAI),
		typeof(MouthDogAI),
		typeof(RadMechAI),
		typeof(FlowermanAI),
		typeof(ClaySurgeonAI),
		typeof(RollingGiantAI)
	};

	public static HashSet<Type> EXPERIMENTAL_ENEMIES = new HashSet<Type>();

	public const float DEFAULT_WALK_SPEED = 5f;

	public const float DEFAULT_RUN_SPEED = 7f;

	public const float DEFAULT_CAM_DISTANCE = -3.5f;

	public const float DEFAULT_CAM_HEIGHT = 2f;

	public const bool DEFAULT_PLAYER_CONTROLS_ROTATION = true;

	public static Dictionary<Type, EnemyAttributes> ENEMY_ATTRIBUTES = new Dictionary<Type, EnemyAttributes>
	{
		{
			typeof(BlobAI),
			new EnemyAttributes
			{
				WalkSpeed = 1f,
				RunSpeed = 2f,
				PlayerControlsRotation = false,
				EnemyName = "Blob"
			}
		},
		{
			typeof(CrawlerAI),
			new EnemyAttributes
			{
				WalkSpeed = 6f,
				RunSpeed = 12f,
				EnemyName = "Thumper"
			}
		},
		{
			typeof(JesterAI),
			new EnemyAttributes
			{
				WalkSpeed = 4f,
				RunSpeed = 6f,
				EnemyName = "Jester"
			}
		},
		{
			typeof(ForestGiantAI),
			new EnemyAttributes
			{
				WalkSpeed = 6f,
				RunSpeed = 8f,
				CamDistance = -7f,
				CamHeight = 10f,
				EnemyName = "Forest Keeper"
			}
		},
		{
			typeof(MouthDogAI),
			new EnemyAttributes
			{
				WalkSpeed = 3f,
				RunSpeed = 7f,
				CamDistance = -5f,
				CamHeight = 5f,
				EnemyName = "Eyeless Dog"
			}
		},
		{
			typeof(FlowermanAI),
			new EnemyAttributes
			{
				EnemyName = "Bracken",
				RunSpeed = 8f
			}
		},
		{
			typeof(PufferAI),
			new EnemyAttributes
			{
				EnemyName = "Spore Lizard"
			}
		},
		{
			typeof(HoarderBugAI),
			new EnemyAttributes
			{
				EnemyName = "Hoarding Bug"
			}
		},
		{
			typeof(CentipedeAI),
			new EnemyAttributes
			{
				EnemyName = "Snare Flea",
				CamHeight = 1.5f,
				CamDistance = -3f
			}
		},
		{
			typeof(SpringManAI),
			new EnemyAttributes
			{
				EnemyName = "Coil-Head",
				WalkSpeed = 5f,
				RunSpeed = 6f
			}
		},
		{
			typeof(SandSpiderAI),
			new EnemyAttributes
			{
				EnemyName = "Bunker Spider"
			}
		},
		{
			typeof(DressGirlAI),
			new EnemyAttributes
			{
				EnemyName = "Ghost Girl"
			}
		},
		{
			typeof(MaskedPlayerEnemy),
			new EnemyAttributes
			{
				EnemyName = "Masked",
				UseLerpRotation = false
			}
		},
		{
			typeof(TestEnemy),
			new EnemyAttributes
			{
				WalkSpeed = 6f,
				RunSpeed = 8f,
				EnemyName = "Obunga",
				EnemyPositionOffset = new Vector3(0f, 1.5f, 0f),
				CamDistance = -4f,
				CamHeight = 3f
			}
		},
		{
			typeof(RadMechAI),
			new EnemyAttributes
			{
				WalkSpeed = 12f,
				RunSpeed = 16f,
				CamDistance = -7f,
				CamHeight = 10f,
				EnemyName = "Old Bird"
			}
		},
		{
			typeof(FlowerSnakeEnemy),
			new EnemyAttributes
			{
				EnemyName = "Tulip Snake"
			}
		},
		{
			typeof(ClaySurgeonAI),
			new EnemyAttributes
			{
				WalkSpeed = 12f,
				RunSpeed = 16f,
				EnemyName = "Barber"
			}
		},
		{
			typeof(RollingGiantAI),
			new EnemyAttributes
			{
				WalkSpeed = 6f,
				RunSpeed = 10f,
				CamDistance = -5f,
				CamHeight = 3.5f,
				EnemyName = "Rolling Giant"
			}
		}
	};

	private static readonly EnemyAttributes DEFAULT_ATTRIBUTES = new EnemyAttributes();

	public static Dictionary<Type, Func<GameObject, EnemyAIController>> ENEMY_CONTROLLERS = new Dictionary<Type, Func<GameObject, EnemyAIController>>
	{
		{
			typeof(NutcrackerEnemyAI),
			(GameObject controller) => controller.AddComponent<NutcrackerEnemyController>()
		},
		{
			typeof(CentipedeAI),
			(GameObject controller) => controller.AddComponent<CentipedeEnemyController>()
		},
		{
			typeof(PufferAI),
			(GameObject controller) => controller.AddComponent<PufferEnemyController>()
		},
		{
			typeof(JesterAI),
			(GameObject controller) => controller.AddComponent<JesterEnemyController>()
		},
		{
			typeof(ForestGiantAI),
			(GameObject controller) => controller.AddComponent<ForestGiantEnemyController>()
		},
		{
			typeof(MouthDogAI),
			(GameObject controller) => controller.AddComponent<MouthDogEnemyController>()
		},
		{
			typeof(HoarderBugAI),
			(GameObject controller) => controller.AddComponent<HoardingBugEnemyController>()
		},
		{
			typeof(FlowermanAI),
			(GameObject controller) => controller.AddComponent<FlowermanEnemyController>()
		},
		{
			typeof(BaboonBirdAI),
			(GameObject controller) => controller.AddComponent<BaboonBirdEnemyController>()
		},
		{
			typeof(BlobAI),
			(GameObject controller) => controller.AddComponent<BlobEnemyController>()
		},
		{
			typeof(MaskedPlayerEnemy),
			(GameObject controller) => controller.AddComponent<MaskedPlayerEnemyController>()
		},
		{
			typeof(SpringManAI),
			(GameObject controller) => controller.AddComponent<SpringManEnemyController>()
		},
		{
			typeof(SandWormAI),
			(GameObject controller) => controller.AddComponent<SandWormEnemyController>()
		},
		{
			typeof(TestEnemy),
			(GameObject controller) => controller.AddComponent<SecretTestEnemyController>()
		},
		{
			typeof(RadMechAI),
			(GameObject controller) => controller.AddComponent<RadMechEnemyController>()
		},
		{
			typeof(FlowerSnakeEnemy),
			(GameObject controller) => controller.AddComponent<FlowerSnakeEnemyController>()
		},
		{
			typeof(ClaySurgeonAI),
			(GameObject controller) => controller.AddComponent<ClaySurgeonEnemyController>()
		},
		{
			typeof(RollingGiantAI),
			(GameObject controller) => controller.AddComponent<RollingGiantEnemyController>()
		}
	};

	public static EnemyAttributes GetEnemyAttributes(this EnemyAI enemyAI)
	{
		return ENEMY_ATTRIBUTES.GetValueOrDefault(((object)enemyAI).GetType(), DEFAULT_ATTRIBUTES);
	}

	public static bool IsEnemyAllowlisted(this EnemyAI enemyAI)
	{
		return ALLOWLISTED_ENEMIES.Contains(((object)enemyAI).GetType());
	}

	public static bool IsEnemyExperimental(this EnemyAI enemyAI)
	{
		return EXPERIMENTAL_ENEMIES.Contains(((object)enemyAI).GetType());
	}

	public static string GetEnemyName(this EnemyAI enemyAI)
	{
		EnemyAttributes valueOrDefault = ENEMY_ATTRIBUTES.GetValueOrDefault(((object)enemyAI).GetType(), null);
		if (valueOrDefault == null)
		{
			return enemyAI.enemyType?.enemyName ?? ((Object)((Component)enemyAI).gameObject).name;
		}
		return valueOrDefault.EnemyName;
	}

	public static EnemyAIController GetAndAttachEnemyController(this EnemyAI enemyAI, GameObject controller)
	{
		Func<GameObject, EnemyAIController> valueOrDefault = ENEMY_CONTROLLERS.GetValueOrDefault(((object)enemyAI).GetType(), (GameObject c) => c.AddComponent<EnemyAIController>());
		return valueOrDefault(controller);
	}
}



