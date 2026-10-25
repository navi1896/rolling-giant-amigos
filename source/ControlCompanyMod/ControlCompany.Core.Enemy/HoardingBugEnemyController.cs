#nullable disable
using System;
using System.Collections.Generic;
using ControlCompany.Core.Extentions;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ControlCompany.Core.Enemy;

internal class HoardingBugEnemyController : EnemyAIController
{
	private HoarderBugAI hoarderBugAI;

	private List<Type> allowedGrabbableItemUse = new List<Type>
	{
		typeof(ShotgunItem),
		typeof(NoisemakerProp),
		typeof(FlashlightItem)
	};

	private float grabRange = 1.5f;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		hoarderBugAI = (HoarderBugAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void EnableAIControl(bool enabled)
	{
		if (enabled)
		{
			hoarderBugAI.Call("ChooseNestPosition");
			((EnemyAI)hoarderBugAI).SwitchToBehaviourState(0);
		}
		else
		{
			hoarderBugAI.nestPosition = new Vector3(2000f, 2000f, 2000f);
		}
		base.EnableAIControl(enabled);
	}

	protected override void LateUpdate()
	{
		if (isInitialized)
		{
			base.LateUpdate();
			if (!isAIControlled)
			{
				hoarderBugAI.nestPosition = new Vector3(2000f, 2000f, 2000f);
			}
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if (hoarderBugAI.heldItem != null)
		{
			UseHeldItem();
		}
	}

	protected override void UseSecondarySkillAction()
	{
		ControlCompanyPlugin.Instance.logger.LogMessage((object)"Using hoarder bug secondary");
		if (hoarderBugAI.heldItem == null)
		{
			GrabNearbyItem();
		}
		else
		{
			DropCurrentItem();
		}
	}

	public void UseHeldItem()
	{
		GrabbableObject itemGrabbableObject = hoarderBugAI.heldItem.itemGrabbableObject;
		Type type = ((object)itemGrabbableObject).GetType();
		if (allowedGrabbableItemUse.Contains(type))
		{
			if (type == typeof(ShotgunItem))
			{
				ShotgunItem val = (ShotgunItem)itemGrabbableObject;
				val.gunShootAudio.volume = 0.15f;
				val.shotgunRayPoint = enemyGameObject.transform;
				val.ShootGunAndSync(false);
			}
			else
			{
				itemGrabbableObject.UseItemOnClient(true);
			}
		}
	}

	public void GrabNearbyItem()
	{
		if (hoarderBugAI.heldItem != null)
		{
			return;
		}
		Collider[] array = Physics.OverlapSphere(enemyGameObject.transform.position, grabRange);
		for (int i = 0; i < array.Length; i++)
		{
			GrabbableObject componentInChildren = ((Component)array[i]).GetComponentInChildren<GrabbableObject>();
			if ((Object)(object)componentInChildren != (Object)null && hoarderBugAI.heldItem == null)
			{
				NetworkObject component = ((Component)componentInChildren).GetComponent<NetworkObject>();
				if ((Object)(object)component != (Object)null)
				{
					hoarderBugAI.targetItem = componentInChildren;
					hoarderBugAI.Call("GrabTargetItemIfClose");
					break;
				}
			}
		}
	}

	public void DropCurrentItem()
	{
		if (hoarderBugAI.heldItem != null)
		{
			hoarderBugAI.Call("DropItemAndCallDropRPC", ((Component)hoarderBugAI.heldItem.itemGrabbableObject).GetComponent<NetworkObject>(), false);
		}
	}

	public override string GetPrimarySkillName()
	{
		return (hoarderBugAI.heldItem != null && allowedGrabbableItemUse.Contains(((object)hoarderBugAI.heldItem.itemGrabbableObject).GetType())) ? "Use item" : "";
	}

	public override string GetSecondarySkillName()
	{
		return (hoarderBugAI.heldItem == null) ? "Grab item" : "Drop item";
	}
}



