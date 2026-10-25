#nullable disable
using ControlCompany.Core.Extentions;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class FlowerSnakeEnemyController : EnemyAIController
{
	private FlowerSnakeEnemy flowerSnakeAI;

	private bool canLeap = true;

	private float leapCooldownTime = 1f;

	private float leapCooldownTimer;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		flowerSnakeAI = (FlowerSnakeEnemy)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void LateUpdate()
	{
		if (!isInitialized)
		{
			return;
		}
		base.LateUpdate();
		if (isAIControlled)
		{
			return;
		}
		bool flag = IsClingingToPlayer();
		bool fieldValue = flowerSnakeAI.GetFieldValue<bool>("leaping");
		if (flag || fieldValue)
		{
			controlEnemyMovement = false;
			controlEnemyRotation = false;
			((Component)this).transform.position = enemyGameObject.transform.position;
		}
		else
		{
			controlEnemyMovement = true;
			controlEnemyRotation = true;
		}
		if (!flag && !fieldValue && !canLeap)
		{
			leapCooldownTimer += Time.deltaTime;
			if (leapCooldownTimer >= leapCooldownTime)
			{
				leapCooldownTimer = 0f;
				canLeap = true;
			}
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if (IsClingingToPlayer())
		{
			if ((Object)(object)flowerSnakeAI.clingingToPlayer != (Object)null)
			{
				flowerSnakeAI.StopClingingServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
		else if (canLeap && !flowerSnakeAI.GetFieldValue<bool>("leaping"))
		{
			LeapForward();
		}
	}

	public override string GetPrimarySkillName()
	{
		return IsClingingToPlayer() ? "Stop clinging" : "Leap";
	}

	protected override void UseSecondarySkillAction()
	{
		if (IsClingingToPlayer() && !flowerSnakeAI.activatedFlight)
		{
		}
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}

	private bool IsClingingToPlayer()
	{
		return (Object)(object)flowerSnakeAI.clingingToPlayer != (Object)null;
	}

	private void LeapForward()
	{
		Vector3 val = ((Component)this).transform.forward * 2f;
		val += Random.insideUnitSphere * Random.Range(0.05f, 0.15f);
		val.y = Mathf.Clamp(val.y, -16f, 16f);
		val = Vector3.Normalize(val * 1000f);
		flowerSnakeAI.StartLeapOnLocalClient(val);
		flowerSnakeAI.StartLeapClientRpc(val);
	}
}



