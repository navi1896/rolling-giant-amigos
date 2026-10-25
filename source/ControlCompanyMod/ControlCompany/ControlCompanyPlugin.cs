#nullable disable
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ControlCompany.Core.Config;
using ControlCompany.Patches;
using HarmonyLib;
using UnityEngine;

namespace ControlCompany;

[BepInPlugin(modGUID, modName, modVersion)]
public class ControlCompanyPlugin : BaseUnityPlugin
{
	public const string modGUID = "Crew17.ControlCompany";

	public const string modName = "ControlCompany (Crew17 Edition)";

	public const string modVersion = "1.3.4";

	public static ControlCompanyPlugin Instance;

	internal ManualLogSource logger;

	internal ConfigFile CustomConfigFile;

	private static bool isInitialized = false;

	private void Awake()
	{
		if ((Object)(object)Instance == (Object)null)
		{
			Instance = this;
		}
		EnsureInitialized(base.Config);
	}

	public static void EnsureInitialized(ConfigFile fallbackConfig = null)
	{
		if (isInitialized && ConfigManager.Instance != null)
		{
			return;
		}
		if (Instance == null && RollingGiant.Plugin.InstanceObject != null)
		{
			Instance = RollingGiant.Plugin.InstanceObject.GetComponent<ControlCompanyPlugin>();
			if (Instance == null)
			{
				Instance = RollingGiant.Plugin.InstanceObject.AddComponent<ControlCompanyPlugin>();
			}
		}
		var log = BepInEx.Logging.Logger.CreateLogSource(modGUID);
		if (Instance != null)
		{
			Instance.logger = log;
			string ccCfgPath = Path.Combine(Paths.ConfigPath, "ControlCompany.ControlCompany.cfg");
			Instance.CustomConfigFile = File.Exists(ccCfgPath)
				? new ConfigFile(ccCfgPath, true)
				: (fallbackConfig ?? RollingGiant.Plugin.Config);
		}
		if (ConfigManager.Instance == null)
		{
			GameObject val = new GameObject("crew17_control_company_config_manager");
			val.AddComponent<ConfigManager>();
			((Object)val).hideFlags = (HideFlags)61;
			Object.DontDestroyOnLoad((Object)(object)val);
		}
		log.LogInfo("Loaded ControlCompany (Crew17 Edition) with native Rolling Giant support.");
		isInitialized = true;
	}
}
