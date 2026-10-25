#nullable disable
using ControlCompany.Core;
using HarmonyLib;
using UnityEngine;

namespace ControlCompany.Patches;

[HarmonyPatch(typeof(TimeOfDay))]
internal class TimeOfDayPatch
{
	[HarmonyPatch("VoteShipToLeaveEarly")]
	[HarmonyPrefix]
	private static bool VoteToLeaveEarly()
	{
		if ((Object)(object)ControlCenter.Instance != (Object)null)
		{
			return ControlCenter.Instance.CurrentMode == ControlCenter.Mode.HOST;
		}
		return true;
	}
}



