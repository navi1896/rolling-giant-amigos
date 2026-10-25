#nullable disable
using ControlCompany.Core;
using HarmonyLib;
using UnityEngine;

namespace ControlCompany.Patches;

[HarmonyPatch(typeof(SoundManager))]
internal class SoundManagerPatch
{
	[HarmonyPatch("SetEarsRinging")]
	[HarmonyPrefix]
	private static bool SetEarsRingingPrefix()
	{
		if ((Object)(object)ControlCenter.Instance != (Object)null)
		{
			return ControlCenter.Instance.CurrentMode == ControlCenter.Mode.HOST;
		}
		return true;
	}
}



