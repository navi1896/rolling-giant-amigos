#nullable disable
using ControlCompany.Core.Extentions;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class MaskedPlayerEnemyController : EnemyAIController
{
	private MaskedPlayerEnemy maskedEnemy;

	private float entranceDoorOpenRange = 2.5f;

	private float useEntranceDoorCooldown = 2f;

	private float timeSinceLastEntranceDoorUse = 0f;

	private bool canUseEntranceDoorOnCollision = true;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		maskedEnemy = (MaskedPlayerEnemy)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
		canJump = true;
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		bool fieldValue = maskedEnemy.GetFieldValue<bool>("inKillAnimation");
		bool fieldValue2 = maskedEnemy.GetFieldValue<bool>("crouching");
		controlEnemyRotation = !fieldValue && !fieldValue2;
		if (!canUseEntranceDoorOnCollision)
		{
			timeSinceLastEntranceDoorUse += Time.deltaTime;
			if (timeSinceLastEntranceDoorUse >= useEntranceDoorCooldown)
			{
				canUseEntranceDoorOnCollision = true;
				timeSinceLastEntranceDoorUse = 0f;
			}
		}
		PlayerActions.MovementActions movement = playerActions.Movement;
		if (movement.Interact.WasPressedThisFrame())
		{
			ScanNearbyAndOpenMainEntranceDoor();
			return;
		}
		movement = playerActions.Movement;
		if (movement.Crouch.WasPressedThisFrame())
		{
			Crouch(!fieldValue2);
		}
	}

	protected override void Move()
	{
		bool fieldValue = maskedEnemy.GetFieldValue<bool>("inKillAnimation");
		bool fieldValue2 = maskedEnemy.GetFieldValue<bool>("crouching");
		if (!fieldValue && !fieldValue2)
		{
			base.Move();
		}
	}

	protected override void UseSecondarySkillAction()
	{
	}

	private void Crouch(bool crouching)
	{
		maskedEnemy.SetCrouchingServerRpc(crouching);
	}

	private void ScanNearbyAndOpenMainEntranceDoor()
	{
		Collider[] array = Physics.OverlapSphere(((Component)this).transform.position, entranceDoorOpenRange);
		for (int i = 0; i < array.Length; i++)
		{
			EntranceTeleport componentInChildren = ((Component)array[i]).GetComponentInChildren<EntranceTeleport>();
			if ((Object)(object)componentInChildren != (Object)null && componentInChildren.entranceId == 0)
			{
				UseEntranceDoor();
			}
		}
	}

	private void UseEntranceDoor()
	{
		((Collider)characterController).enabled = false;
		Vector3 position = RoundManager.FindMainEntrancePosition(true, !((EnemyAI)maskedEnemy).isOutside);
		((Component)this).transform.position = position;
		maskedEnemy.Call("TeleportMaskedEnemyAndSync", RoundManager.FindMainEntrancePosition(true, !((EnemyAI)maskedEnemy).isOutside), !((EnemyAI)maskedEnemy).isOutside);
		((Collider)characterController).enabled = true;
		canUseEntranceDoorOnCollision = false;
		ControlCenter.Instance.IsGhostIndoors = !((EnemyAI)maskedEnemy).isOutside;
	}

	protected override void OnControllerColliderHit(ControllerColliderHit hit)
	{
		base.OnControllerColliderHit(hit);
		if (canUseEntranceDoorOnCollision)
		{
			EntranceTeleport componentInChildren = hit.gameObject.GetComponentInChildren<EntranceTeleport>();
			if ((Object)(object)componentInChildren != (Object)null && componentInChildren.entranceId == 0)
			{
				UseEntranceDoor();
			}
		}
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}
}



