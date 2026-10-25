#nullable disable
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ControlCompany.Core.Input;

public static class InputKeys
{
	public static KeyControl EXIT_KEY = Keyboard.current.escapeKey;

	public static KeyControl EXTENDED_MODE_KEY = Keyboard.current.digit0Key;

	public static KeyControl TOGGLE_UI_KEY = Keyboard.current.uKey;

	public static KeyControl CYCLE_KEY_LEFT = Keyboard.current.oKey;

	public static KeyControl CYCLE_KEY_RIGHT = Keyboard.current.pKey;

	public static KeyControl HOST_LISTEN_KEY = Keyboard.current.leftAltKey;

	public static KeyControl HOST_GHOST_MODE_KEY = Keyboard.current.f1Key;

	public static KeyControl HOST_GIVE_CREDITS_KEY = Keyboard.current.digit8Key;

	public static KeyControl HOST_TELEPORT_TO_ENTRANCE_KEY = Keyboard.current.digit9Key;

	public static KeyControl GHOST_SPAWN_ENEMY_1 = Keyboard.current.f1Key;

	public static KeyControl GHOST_SPAWN_ENEMY_2 = Keyboard.current.f2Key;

	public static KeyControl GHOST_SPAWN_ENEMY_3 = Keyboard.current.f3Key;

	public static KeyControl GHOST_SPAWN_ENEMY_4 = Keyboard.current.f4Key;

	public static KeyControl GHOST_SPAWN_ENEMY_5 = Keyboard.current.f5Key;

	public static KeyControl GHOST_SPAWN_ENEMY_6 = Keyboard.current.f6Key;

	public static KeyControl GHOST_SPAWN_ENEMY_7 = Keyboard.current.f7Key;

	public static KeyControl GHOST_SPAWN_ENEMY_8 = Keyboard.current.f8Key;

	public static KeyControl GHOST_SPAWN_ENEMY_9 = Keyboard.current.f9Key;

	public static KeyControl GHOST_SPAWN_ENEMY_10 = Keyboard.current.f10Key;

	public static KeyControl GHOST_SPAWN_ENEMY_11 = Keyboard.current.f11Key;

	public static KeyControl GHOST_SPAWN_ENEMY_12 = Keyboard.current.f12Key;

	public static List<KeyControl> GHOST_SPAWN_ENEMY_KEYS = new List<KeyControl>
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

	public static KeyControl ENEMY_TOGGLE_AI_KEY = Keyboard.current.f1Key;

	public static ButtonControl ENEMY_PRIMARY_SKILL_BUTTON = Mouse.current.leftButton;

	public static ButtonControl ENEMY_SECONDARY_SKILL_BUTTON = Mouse.current.rightButton;

	public static KeyControl LOCK_ENEMY_ROTATION_KEY = Keyboard.current.rightAltKey;

	public static KeyControl ENEMY_TOGGLE_LIGHT_KEY = Keyboard.current.lKey;

	public static KeyControl CONTROLLER_USE_KEY = Keyboard.current.eKey;

	public static KeyControl CONTROLLER_JUMP_KEY = Keyboard.current.spaceKey;
}



