#nullable disable
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ControlCompany.Core.Input;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ControlCompany.Core.Config;

internal class ConfigManager : MonoBehaviour
{
	public static ConfigManager Instance;

	private bool loadedConfig = false;

	private const string KEYBIND_CONFIG = "Keybinds";

	private const string KEYBIND_CONFIG_HOST_MODE = "Keybinds.PlayerMode";

	private const string KEYBIND_CONFIG_GHOST_MODE = "Keybinds.GhostMode";

	private const string KEYBIND_CONFIG_ENEMY_MODE = "Keybinds.MonsterMode";

	private const string GENERAL_CONIG = "General";

	private bool forcePushToTalkWhileInMonsterMode = true;

	private bool disableModForSelf = false;

	private bool useEnemySpawningBehaviour = false;

	private bool disableWhileUsingTerminal = true;

	public bool ForcePushToTalkWhileInMonsterMode => forcePushToTalkWhileInMonsterMode;

	public bool DisableModForSelf => disableModForSelf;

	public bool UseEnemySpawningBehaviour => useEnemySpawningBehaviour;

	public bool DisableWhileUsingTerminal => disableWhileUsingTerminal;

	private void Awake()
	{
		if ((Object)(object)Instance == (Object)null)
		{
			Instance = this;
		}
	}

	private void Update()
	{
		if (!loadedConfig && InputKeys.EnsureDefaults() && ControlCompanyPlugin.Instance != null)
		{
			loadedConfig = LoadPlayerConfig(reload: false);
		}
	}

	public bool LoadPlayerConfig(bool reload)
	{
		if (!InputKeys.EnsureDefaults())
		{
			return false;
		}
		if (ControlCompanyPlugin.Instance == null)
		{
			return false;
		}
		ConfigFile config = ControlCompanyPlugin.Instance.CustomConfigFile ?? ((BaseUnityPlugin)ControlCompanyPlugin.Instance).Config;
		if (config == null)
		{
			return false;
		}
		if (reload)
		{
			config.Reload();
		}
		LoadConfig(config);
		loadedConfig = true;
		return true;
	}

	private void LoadConfig(ConfigFile config)
	{
		ControlCompanyPlugin.Instance.logger?.LogInfo((object)"Loading config");
		useEnemySpawningBehaviour = config.Bind<bool>("General", "use_enemy_spawning_behaviour", false, (ConfigDescription)null).Value;
		disableWhileUsingTerminal = config.Bind<bool>("General", "disable_while_using_terminal", true, (ConfigDescription)null).Value;
		disableModForSelf = config.Bind<bool>("General", "disable_mod_for_self", false, (ConfigDescription)null).Value;
		forcePushToTalkWhileInMonsterMode = config.Bind<bool>("General", "force_push_to_talk_in_monster_mode", true, (ConfigDescription)null).Value;
		ConfigEntry<string> keyEntry = config.Bind<string>("Keybinds", "extended_mode_key", "0", (ConfigDescription)null);
		ConfigEntry<string> keyEntry2 = config.Bind<string>("Keybinds", "exit_mode_key", "esc", (ConfigDescription)null);
		ConfigEntry<string> keyEntry3 = config.Bind<string>("Keybinds", "open_door", "e", (ConfigDescription)null);
		ConfigEntry<string> keyEntry4 = config.Bind<string>("Keybinds", "toggle_ui", "u", (ConfigDescription)null);
		ConfigEntry<string> keyEntry5 = config.Bind<string>("Keybinds", "cycle_left_key", "o", (ConfigDescription)null);
		ConfigEntry<string> keyEntry6 = config.Bind<string>("Keybinds", "cycle_right_key", "p", (ConfigDescription)null);
		ConfigEntry<string> keyEntry7 = config.Bind<string>("Keybinds", "listen_from_player", "alt", (ConfigDescription)null);
		InputKeys.CONTROLLER_USE_KEY = GetKeyControlFromConfig(keyEntry3, InputKeys.CONTROLLER_USE_KEY);
		InputKeys.EXTENDED_MODE_KEY = GetKeyControlFromConfig(keyEntry, InputKeys.EXTENDED_MODE_KEY);
		InputKeys.EXIT_KEY = GetKeyControlFromConfig(keyEntry2, InputKeys.EXIT_KEY);
		InputKeys.TOGGLE_UI_KEY = GetKeyControlFromConfig(keyEntry4, InputKeys.TOGGLE_UI_KEY);
		InputKeys.CYCLE_KEY_LEFT = GetKeyControlFromConfig(keyEntry5, InputKeys.CYCLE_KEY_LEFT);
		InputKeys.CYCLE_KEY_RIGHT = GetKeyControlFromConfig(keyEntry6, InputKeys.CYCLE_KEY_RIGHT);
		InputKeys.HOST_LISTEN_KEY = GetKeyControlFromConfig(keyEntry7, InputKeys.HOST_LISTEN_KEY);
		ConfigEntry<string> keyEntry8 = config.Bind<string>("Keybinds.PlayerMode", "ghost_mode_key", "f1", (ConfigDescription)null);
		InputKeys.HOST_GHOST_MODE_KEY = GetKeyControlFromConfig(keyEntry8, InputKeys.HOST_GHOST_MODE_KEY);
		ConfigEntry<string> keyEntry9 = config.Bind<string>("Keybinds.PlayerMode", "extended_teleport_to_entrance_key", "9", (ConfigDescription)null);
		ConfigEntry<string> keyEntry10 = config.Bind<string>("Keybinds.PlayerMode", "extended_give_credits_key", "8", (ConfigDescription)null);
		InputKeys.HOST_TELEPORT_TO_ENTRANCE_KEY = GetKeyControlFromConfig(keyEntry9, InputKeys.HOST_TELEPORT_TO_ENTRANCE_KEY);
		InputKeys.HOST_GIVE_CREDITS_KEY = GetKeyControlFromConfig(keyEntry10, InputKeys.HOST_GIVE_CREDITS_KEY);
		ConfigEntry<string> keyEntry11 = config.Bind<string>("Keybinds.GhostMode", "jump", "space", (ConfigDescription)null);
		InputKeys.CONTROLLER_JUMP_KEY = GetKeyControlFromConfig(keyEntry11, InputKeys.CONTROLLER_JUMP_KEY);
		ConfigEntry<string> keyEntry12 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_1", "f1", (ConfigDescription)null);
		ConfigEntry<string> keyEntry13 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_2", "f2", (ConfigDescription)null);
		ConfigEntry<string> keyEntry14 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_3", "f3", (ConfigDescription)null);
		ConfigEntry<string> keyEntry15 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_4", "f4", (ConfigDescription)null);
		ConfigEntry<string> keyEntry16 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_5", "f5", (ConfigDescription)null);
		ConfigEntry<string> keyEntry17 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_6", "f6", (ConfigDescription)null);
		ConfigEntry<string> keyEntry18 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_7", "f7", (ConfigDescription)null);
		ConfigEntry<string> keyEntry19 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_8", "f8", (ConfigDescription)null);
		ConfigEntry<string> keyEntry20 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_9", "f9", (ConfigDescription)null);
		ConfigEntry<string> keyEntry21 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_10", "f10", (ConfigDescription)null);
		ConfigEntry<string> keyEntry22 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_11", "f11", (ConfigDescription)null);
		ConfigEntry<string> keyEntry23 = config.Bind<string>("Keybinds.GhostMode", "control_enemy_12", "f12", (ConfigDescription)null);
		InputKeys.GHOST_SPAWN_ENEMY_1 = GetKeyControlFromConfig(keyEntry12, InputKeys.GHOST_SPAWN_ENEMY_1);
		InputKeys.GHOST_SPAWN_ENEMY_2 = GetKeyControlFromConfig(keyEntry13, InputKeys.GHOST_SPAWN_ENEMY_2);
		InputKeys.GHOST_SPAWN_ENEMY_3 = GetKeyControlFromConfig(keyEntry14, InputKeys.GHOST_SPAWN_ENEMY_3);
		InputKeys.GHOST_SPAWN_ENEMY_4 = GetKeyControlFromConfig(keyEntry15, InputKeys.GHOST_SPAWN_ENEMY_4);
		InputKeys.GHOST_SPAWN_ENEMY_5 = GetKeyControlFromConfig(keyEntry16, InputKeys.GHOST_SPAWN_ENEMY_5);
		InputKeys.GHOST_SPAWN_ENEMY_6 = GetKeyControlFromConfig(keyEntry17, InputKeys.GHOST_SPAWN_ENEMY_6);
		InputKeys.GHOST_SPAWN_ENEMY_7 = GetKeyControlFromConfig(keyEntry18, InputKeys.GHOST_SPAWN_ENEMY_7);
		InputKeys.GHOST_SPAWN_ENEMY_8 = GetKeyControlFromConfig(keyEntry19, InputKeys.GHOST_SPAWN_ENEMY_8);
		InputKeys.GHOST_SPAWN_ENEMY_9 = GetKeyControlFromConfig(keyEntry20, InputKeys.GHOST_SPAWN_ENEMY_9);
		InputKeys.GHOST_SPAWN_ENEMY_10 = GetKeyControlFromConfig(keyEntry21, InputKeys.GHOST_SPAWN_ENEMY_10);
		InputKeys.GHOST_SPAWN_ENEMY_11 = GetKeyControlFromConfig(keyEntry22, InputKeys.GHOST_SPAWN_ENEMY_11);
		InputKeys.GHOST_SPAWN_ENEMY_12 = GetKeyControlFromConfig(keyEntry23, InputKeys.GHOST_SPAWN_ENEMY_12);
		InputKeys.GHOST_SPAWN_ENEMY_KEYS = new List<KeyControl>
		{
			InputKeys.GHOST_SPAWN_ENEMY_1,
			InputKeys.GHOST_SPAWN_ENEMY_2,
			InputKeys.GHOST_SPAWN_ENEMY_3,
			InputKeys.GHOST_SPAWN_ENEMY_4,
			InputKeys.GHOST_SPAWN_ENEMY_5,
			InputKeys.GHOST_SPAWN_ENEMY_6,
			InputKeys.GHOST_SPAWN_ENEMY_7,
			InputKeys.GHOST_SPAWN_ENEMY_8,
			InputKeys.GHOST_SPAWN_ENEMY_9,
			InputKeys.GHOST_SPAWN_ENEMY_10,
			InputKeys.GHOST_SPAWN_ENEMY_11,
			InputKeys.GHOST_SPAWN_ENEMY_12
		};
		ConfigEntry<string> keyEntry24 = config.Bind<string>("Keybinds.MonsterMode", "toggle_ai_key", "f1", (ConfigDescription)null);
		InputKeys.ENEMY_TOGGLE_AI_KEY = GetKeyControlFromConfig(keyEntry24, InputKeys.ENEMY_TOGGLE_AI_KEY);
		ConfigEntry<string> keyEntry25 = config.Bind<string>("Keybinds.MonsterMode", "toggle_light", "l", (ConfigDescription)null);
		InputKeys.ENEMY_TOGGLE_LIGHT_KEY = GetKeyControlFromConfig(keyEntry25, InputKeys.ENEMY_TOGGLE_LIGHT_KEY);
		ControlCompanyPlugin.Instance.logger?.LogInfo((object)"Done loading keybinds");
	}

	private KeyControl GetKeyControlFromConfig(ConfigEntry<string> keyEntry, KeyControl defaultKey)
	{
		try
		{
			KeyControl val = Keyboard.current?.FindKeyOnCurrentKeyboardLayout(keyEntry.Value);
			if (val != null)
			{
				return val;
			}
			ControlCompanyPlugin.Instance.logger?.LogWarning((object)$"Cannot rebind key for {((ConfigEntryBase)keyEntry).Definition.Key} to {keyEntry.Value}: Cannot find key {((ConfigEntryBase)keyEntry).Definition.Key} on keyboard layout. Using default key ${defaultKey}");
			return defaultKey;
		}
		catch (Exception ex)
		{
			ControlCompanyPlugin.Instance.logger?.LogWarning((object)$"Cannot rebind key for {((ConfigEntryBase)keyEntry).Definition.Key} to {keyEntry.Value}: {ex.Message}. Using default key ${defaultKey}");
			return defaultKey;
		}
	}
}
