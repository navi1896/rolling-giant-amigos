#nullable disable
using ControlCompany.Core;
using HarmonyLib;
using UnityEngine;

namespace ControlCompany.Patches;

[HarmonyPatch(typeof(HUDManager))]
internal class HUDManagerPatch
{
	[HarmonyPatch("CanPlayerScan")]
	[HarmonyPostfix]
	private static void CanPlayerScanPostfix(ref bool __result)
	{
		if (__result && (Object)(object)ControlCenter.Instance != (Object)null)
		{
			__result = ControlCenter.Instance.CurrentMode == ControlCenter.Mode.HOST;
		}
	}

	[HarmonyPatch("UpdateHealthUI")]
	[HarmonyPrefix]
	private static bool UpdateHealthUIPrefix()
	{
		if ((Object)(object)ControlCenter.Instance != (Object)null)
		{
			return ControlCenter.Instance.CurrentMode == ControlCenter.Mode.HOST;
		}
		return true;
	}
}



