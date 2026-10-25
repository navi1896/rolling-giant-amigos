#nullable disable
using System.Linq;
using ControlCompany.Core.Config;
using ControlCompany.Core.Extentions;
using HarmonyLib;
using UnityEngine;

namespace ControlCompany.Patches;

[HarmonyPatch]
internal class LobbyListPatch
{
	public const char SERVER_NAME_FILTER = '\u200b';

	[HarmonyPatch(typeof(MenuManager), "ConfirmHostButton")]
	[HarmonyPrefix]
	private static void AddServerNameChar(MenuManager __instance)
	{
		if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.lobbyNameInputField == (Object)null || (Object)(object)ConfigManager.Instance == (Object)null)
		{
			return;
		}
		string text = __instance.lobbyNameInputField.text ?? "";
		if (!ConfigManager.Instance.DisableModForSelf && __instance.hostSettings_LobbyPublic)
		{
			if (!text.Contains('\u200b'))
			{
				text = ((text.Length > 3) ? text.Insert(text.Length - 2, '\u200b'.ToString()) : ("\u200b" + text));
			}
			if (!text.Trim().StartsWith("[MODDED]"))
			{
				text = "[MODDED] " + text;
			}
			__instance.lobbyNameInputField.text = text;
		}
		else
		{
			__instance.lobbyNameInputField.text = text.RemoveChar('\u200b');
		}
	}
}
