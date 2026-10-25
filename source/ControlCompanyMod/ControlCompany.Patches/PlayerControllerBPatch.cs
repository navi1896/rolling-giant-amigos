#nullable disable
using ControlCompany.Core;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ControlCompany.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
internal class PlayerControllerBPatch
{
	[HarmonyPatch("PlayerLookInput")]
	[HarmonyPrefix]
	private static bool PlayerLookInputPrefix()
	{
		if ((Object)(object)ControlCenter.Instance != (Object)null)
		{
			return ControlCenter.Instance.CurrentMode == ControlCenter.Mode.HOST;
		}
		return true;
	}
}



