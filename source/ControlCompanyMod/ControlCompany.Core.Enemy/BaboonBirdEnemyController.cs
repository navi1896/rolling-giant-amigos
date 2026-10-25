#nullable disable
using ControlCompany.Core.Extentions;
using Unity.Netcode;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class BaboonBirdEnemyController : EnemyAIController
{
	private BaboonBirdAI baboonBirdAI;

	private float grabRange = 1.5f;

	private Vector3 originalBaboonCampPosition;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		baboonBirdAI = (BaboonBirdAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
	}

	protected override void EnableAIControl(bool enabled)
	{
		if (enabled)
		{
			BaboonBirdAI.baboonCampPosition = originalBaboonCampPosition;
		}
		else
		{
			originalBaboonCampPosition = BaboonBirdAI.baboonCampPosition;
			BaboonBirdAI.baboonCampPosition = new Vector3(1000f, 1000f, 1000f);
		}
		base.EnableAIControl(enabled);
	}

	protected override void UseSecondarySkillAction()
	{
		if ((Object)(object)baboonBirdAI.heldScrap == (Object)null)
		{
			GrabNearbyItem();
		}
		else
		{
			DropCurrentItem();
		}
	}

	public void GrabNearbyItem()
	{
		if ((Object)(object)baboonBirdAI.heldScrap != (Object)null)
		{
			return;
		}
		Collider[] array = Physics.OverlapSphere(enemyGameObject.transform.position, grabRange);
		NetworkObject val = null;
		float num = float.PositiveInfinity;
		for (int i = 0; i < array.Length; i++)
		{
			GrabbableObject componentInChildren = ((Component)array[i]).GetComponentInChildren<GrabbableObject>();
			if (!((Object)(object)componentInChildren != (Object)null) || !(bool)baboonBirdAI.Call("CanGrabScrap", componentInChildren))
			{
				continue;
			}
			NetworkObject component = ((Component)componentInChildren).GetComponent<NetworkObject>();
			if ((Object)(object)component != (Object)null)
			{
				float num2 = Vector3.Distance(((Component)componentInChildren).transform.position, ((Component)this).transform.position);
				if (num2 < num)
				{
					num = num2;
					val = component;
				}
			}
		}
		if ((Object)(object)val != (Object)null)
		{
			((EnemyAI)baboonBirdAI).SwitchToBehaviourState(1);
			baboonBirdAI.Call("GrabItemAndSync", val);
		}
	}

	public void DropCurrentItem()
	{
		if (!((Object)(object)baboonBirdAI.heldScrap == (Object)null))
		{
			baboonBirdAI.Call("DropHeldItemAndSync");
		}
	}

	public override void DestroyAndCleanUp()
	{
		if (!((EnemyAI)baboonBirdAI).isEnemyDead && (Object)(object)baboonBirdAI.heldScrap != (Object)null)
		{
			DropCurrentItem();
		}
		base.DestroyAndCleanUp();
	}

	public override string GetSecondarySkillName()
	{
		return ((Object)(object)baboonBirdAI.heldScrap == (Object)null) ? "Grab item" : "Drop item";
	}
}



