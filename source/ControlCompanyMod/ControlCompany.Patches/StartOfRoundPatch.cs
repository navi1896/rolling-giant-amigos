#nullable disable
using ControlCompany.Core;
using ControlCompany.Core.Config;
using ControlCompany.Core.UI;
using HarmonyLib;
using UnityEngine;

namespace ControlCompany.Patches;

[HarmonyPatch]
internal class StartOfRoundPatch
{
	[HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
	[HarmonyPrefix]
	private static void ShipHasLeftPrefix()
	{
		if ((Object)(object)ControlCenter.Instance != (Object)null)
		{
			ControlCenter.Instance.EndOfRoundCleanUp();
		}
	}

	[HarmonyPatch(typeof(RoundManager), "RefreshEnemiesList")]
	[HarmonyPostfix]
	private static void StartGamePostfix()
	{
		if ((Object)(object)ControlCenter.Instance != (Object)null)
		{
			ControlCenter.Instance.StartOfRoundInitialization();
		}
	}

	[HarmonyPatch(typeof(StartOfRound), "Awake")]
	[HarmonyPriority(Priority.Last)]
	[HarmonyPostfix]
	private static void CreateControlCenter()
	{
		ControlCompanyPlugin.EnsureInitialized();
		GameObject legacyCenter = GameObject.Find("control_company_control_center");
		if ((Object)(object)legacyCenter != (Object)null)
		{
			Object.Destroy((Object)(object)legacyCenter);
		}
		if (ConfigManager.Instance == null || !ConfigManager.Instance.DisableModForSelf)
		{
			GameObject val = new GameObject("crew17_control_company_center");
			val.AddComponent<ControlCenter>();
			val.AddComponent<UIManager>();
		}
	}
}
