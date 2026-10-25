#nullable disable
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ControlCompany.Core.Input;

public static class InputKeys
{
	public static KeyControl EXIT_KEY;

	public static KeyControl EXTENDED_MODE_KEY;

	public static KeyControl TOGGLE_UI_KEY;

	public static KeyControl CYCLE_KEY_LEFT;

	public static KeyControl CYCLE_KEY_RIGHT;

	public static KeyControl HOST_LISTEN_KEY;

	public static KeyControl HOST_GHOST_MODE_KEY;

	public static KeyControl HOST_GIVE_CREDITS_KEY;

	public static KeyControl HOST_TELEPORT_TO_ENTRANCE_KEY;

	public static KeyControl GHOST_SPAWN_ENEMY_1;

	public static KeyControl GHOST_SPAWN_ENEMY_2;

	public static KeyControl GHOST_SPAWN_ENEMY_3;

	public static KeyControl GHOST_SPAWN_ENEMY_4;

	public static KeyControl GHOST_SPAWN_ENEMY_5;

	public static KeyControl GHOST_SPAWN_ENEMY_6;

	public static KeyControl GHOST_SPAWN_ENEMY_7;

	public static KeyControl GHOST_SPAWN_ENEMY_8;

	public static KeyControl GHOST_SPAWN_ENEMY_9;

	public static KeyControl GHOST_SPAWN_ENEMY_10;

	public static KeyControl GHOST_SPAWN_ENEMY_11;

	public static KeyControl GHOST_SPAWN_ENEMY_12;

	public static List<KeyControl> GHOST_SPAWN_ENEMY_KEYS = new List<KeyControl>();

	public static KeyControl ENEMY_TOGGLE_AI_KEY;

	public static ButtonControl ENEMY_PRIMARY_SKILL_BUTTON;

	public static ButtonControl ENEMY_SECONDARY_SKILL_BUTTON;

	public static KeyControl LOCK_ENEMY_ROTATION_KEY;

	public static KeyControl ENEMY_TOGGLE_LIGHT_KEY;

	public static KeyControl CONTROLLER_USE_KEY;

	public static KeyControl CONTROLLER_JUMP_KEY;

	private static bool defaultsInitialized = false;

	public static bool EnsureDefaults()
	{
		if (defaultsInitialized && EXIT_KEY != null && ENEMY_PRIMARY_SKILL_BUTTON != null && GHOST_SPAWN_ENEMY_KEYS != null && GHOST_SPAWN_ENEMY_KEYS.Count > 0)
		{
			return true;
		}
		if (Keyboard.current == null || Mouse.current == null)
		{
			return false;
		}
		EXIT_KEY = Keyboard.current.escapeKey;
		EXTENDED_MODE_KEY = Keyboard.current.digit0Key;
		TOGGLE_UI_KEY = Keyboard.current.uKey;
		CYCLE_KEY_LEFT = Keyboard.current.oKey;
		CYCLE_KEY_RIGHT = Keyboard.current.pKey;
		HOST_LISTEN_KEY = Keyboard.current.leftAltKey;
		HOST_GHOST_MODE_KEY = Keyboard.current.f1Key;
		HOST_GIVE_CREDITS_KEY = Keyboard.current.digit8Key;
		HOST_TELEPORT_TO_ENTRANCE_KEY = Keyboard.current.digit9Key;
		GHOST_SPAWN_ENEMY_1 = Keyboard.current.f1Key;
		GHOST_SPAWN_ENEMY_2 = Keyboard.current.f2Key;
		GHOST_SPAWN_ENEMY_3 = Keyboard.current.f3Key;
		GHOST_SPAWN_ENEMY_4 = Keyboard.current.f4Key;
		GHOST_SPAWN_ENEMY_5 = Keyboard.current.f5Key;
		GHOST_SPAWN_ENEMY_6 = Keyboard.current.f6Key;
		GHOST_SPAWN_ENEMY_7 = Keyboard.current.f7Key;
		GHOST_SPAWN_ENEMY_8 = Keyboard.current.f8Key;
		GHOST_SPAWN_ENEMY_9 = Keyboard.current.f9Key;
		GHOST_SPAWN_ENEMY_10 = Keyboard.current.f10Key;
		GHOST_SPAWN_ENEMY_11 = Keyboard.current.f11Key;
		GHOST_SPAWN_ENEMY_12 = Keyboard.current.f12Key;
		GHOST_SPAWN_ENEMY_KEYS = new List<KeyControl>
		{
			GHOST_SPAWN_ENEMY_1,
			GHOST_SPAWN_ENEMY_2,
			GHOST_SPAWN_ENEMY_3,
			GHOST_SPAWN_ENEMY_4,
			GHOST_SPAWN_ENEMY_5,
			GHOST_SPAWN_ENEMY_6,
			GHOST_SPAWN_ENEMY_7,
			GHOST_SPAWN_ENEMY_8,
			GHOST_SPAWN_ENEMY_9,
			GHOST_SPAWN_ENEMY_10,
			GHOST_SPAWN_ENEMY_11,
			GHOST_SPAWN_ENEMY_12
		};
		ENEMY_TOGGLE_AI_KEY = Keyboard.current.f1Key;
		ENEMY_PRIMARY_SKILL_BUTTON = Mouse.current.leftButton;
		ENEMY_SECONDARY_SKILL_BUTTON = Mouse.current.rightButton;
		LOCK_ENEMY_ROTATION_KEY = Keyboard.current.rightAltKey;
		ENEMY_TOGGLE_LIGHT_KEY = Keyboard.current.lKey;
		CONTROLLER_USE_KEY = Keyboard.current.eKey;
		CONTROLLER_JUMP_KEY = Keyboard.current.spaceKey;
		defaultsInitialized = true;
		return true;
	}
}
