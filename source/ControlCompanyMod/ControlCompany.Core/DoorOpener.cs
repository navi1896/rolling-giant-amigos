#nullable disable
using ControlCompany.Core.Extentions;
using UnityEngine;

namespace ControlCompany.Core;

internal class DoorOpener : MonoBehaviour
{
	public void DetectAndOpenDoors(float range)
	{
		Collider[] array = Physics.OverlapSphere(((Component)this).transform.position, range);
		for (int i = 0; i < array.Length; i++)
		{
			DoorLock componentInChildren = ((Component)array[i]).GetComponentInChildren<DoorLock>();
			if ((Object)(object)componentInChildren != (Object)null)
			{
				OpenDoor(componentInChildren);
			}
		}
	}

	public void OpenDoor(DoorLock doorLock)
	{
		if ((Object)(object)doorLock != (Object)null && !doorLock.GetFieldValue<bool>("isDoorOpened"))
		{
			doorLock.OpenDoorAsEnemyServerRpc();
			((Component)doorLock).gameObject.GetComponent<AnimatedObjectTrigger>().TriggerAnimationNonPlayer(false, true, false);
		}
	}
}



