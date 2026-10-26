#nullable disable
using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace RollingGiant.CrewControl;

/// <summary>
/// Configuration for Crew Control, the built-in host-only "ghost / possess a monster" system.
/// All code in the CrewControl namespace is original to this package.
/// </summary>
internal static class CrewControlSettings {
    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<Key> EnterGhostKey;
    public static ConfigEntry<Key> ExitKey;
    public static ConfigEntry<Key> PagePrevKey;
    public static ConfigEntry<Key> PageNextKey;
    public static ConfigEntry<Key> UseDoorKey;
    public static ConfigEntry<Key> ToggleAiKey;
    public static ConfigEntry<Key> GhostEntranceKey;
    public static ConfigEntry<float> LookSensitivity;

    public static readonly Key[] SelectKeys = {
        Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6,
        Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12
    };

    public static void Bind(ConfigFile config) {
        const string section = "CrewControl";
        Enabled = config.Bind(section, "Enabled", true,
            "Host-only ghost mode that lets you possess monsters. Automatically disabled when the ControlCompany mod is installed.");
        EnterGhostKey = config.Bind(section, "EnterGhostKey", Key.F1, "Enter ghost mode (host, during a round).");
        ExitKey = config.Bind(section, "ExitKey", Key.Escape, "Leave a monster (back to ghost) or leave ghost mode.");
        PagePrevKey = config.Bind(section, "PagePrevKey", Key.O, "Previous page of the monster list.");
        PageNextKey = config.Bind(section, "PageNextKey", Key.P, "Next page of the monster list.");
        UseDoorKey = config.Bind(section, "UseDoorKey", Key.E, "Use the nearest facility entrance while controlling a monster.");
        ToggleAiKey = config.Bind(section, "ToggleAiKey", Key.F1, "Hand the monster back to its AI / take control again.");
        GhostEntranceKey = config.Bind(section, "GhostEntranceKey", Key.T, "Ghost mode: jump to the other side of the main entrance.");
        LookSensitivity = config.Bind(section, "LookSensitivity", 1f, "Camera sensitivity multiplier in ghost / monster mode.");
    }

    public static bool Pressed(Key key) {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].wasPressedThisFrame;
    }

    public static bool Held(Key key) {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].isPressed;
    }
}
