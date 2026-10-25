#nullable disable
using System;
using System.Linq;
using ControlCompany.Core.Extentions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ControlCompany.Core.Enemy.NonStandardEnemy;

internal class TurretEnemyController : EnemyController
{
	private Turret turret;

	private bool holdingDownFire = false;

	private float holdingDownFireTime = 0f;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		if (!isInitialized)
		{
			Initialize(playerActions, CameraMode.THIRD_PERSON);
			turret = enemyGameObject.GetComponentInChildren<Turret>();
			if ((Object)(object)turret == (Object)null)
			{
				throw new Exception("Turret gameobject does not have Turret script attached.");
			}
			enemyGameObject.transform.position = ((Component)this).transform.position;
			enemyGameObject.transform.rotation = ((Component)this).transform.rotation;
			((Component)turret).gameObject.GetComponentsInChildren<Collider>().ToList().ForEach(delegate(Collider c)
			{
				Physics.IgnoreCollision(c, (Collider)(object)characterController);
			});
			base.enemyGameObject = enemyGameObject;
			canMove = false;
			canJump = false;
			AudioSource mainAudio = turret.mainAudio;
			mainAudio.volume *= 0.3f;
			AudioSource berserkAudio = turret.berserkAudio;
			berserkAudio.volume *= 0.3f;
			AudioSource farAudio = turret.farAudio;
			farAudio.volume *= 0.3f;
			AudioSource bulletCollisionAudio = turret.bulletCollisionAudio;
			bulletCollisionAudio.volume *= 0.3f;
			EnableAIControl(enabled: false);
		}
	}

	protected override void LateUpdate()
	{
		if (!isInitialized)
		{
			return;
		}
		base.LateUpdate();
		if (!isAIControlled)
		{
			if (controlEnemyMovement)
			{
				enemyGameObject.transform.position = ((Component)this).transform.position;
			}
			if (controlEnemyRotation && !lockEnemyRotationInput)
			{
				((Component)turret.turretRod).transform.rotation = ((Component)this).transform.rotation;
			}
			if (holdingDownFire)
			{
				holdingDownFireTime += Time.deltaTime;
				if (holdingDownFireTime < 1f)
				{
					SwitchToMode((TurretMode)1);
				}
				else
				{
					SwitchToMode((TurretMode)2);
				}
			}
			else
			{
				SwitchToMode((TurretMode)0);
			}
		}
		else
		{
			((Component)this).transform.position = enemyGameObject.transform.position;
		}
	}

	private void SwitchToMode(TurretMode mode)
	{
		if (turret.turretMode != mode)
		{
			turret.Call("SwitchTurretMode", (int)mode);
			turret.SetToModeClientRpc((int)mode);
		}
	}

	protected override void UsePrimarySkillAction()
	{
		holdingDownFire = true;
	}

	protected override void ReleasePrimarySkillAction()
	{
		holdingDownFire = false;
		holdingDownFireTime = 0f;
		SwitchToMode((TurretMode)0);
	}

	public override string GetPrimarySkillName()
	{
		return "Fire";
	}
}



