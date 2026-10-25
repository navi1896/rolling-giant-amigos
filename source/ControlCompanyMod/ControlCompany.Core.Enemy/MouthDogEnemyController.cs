#nullable disable
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class MouthDogEnemyController : EnemyAIController
{
	private MouthDogAI mouthDogAI;

	private const int MOUTH_DOG_LUNGE = 2;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		mouthDogAI = (MouthDogAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void UseSecondarySkillAction()
	{
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}

	private void Lunge()
	{
		((EnemyAI)mouthDogAI).SwitchToBehaviourState(2);
	}

	private bool IsLunging()
	{
		return ((EnemyAI)mouthDogAI).currentBehaviourStateIndex == 2;
	}
}



