#nullable disable
using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace RollingGiant.ShipAmbush;

internal static class ShipAmbushSettings {
    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<float> ChancePercent;
    public static ConfigEntry<bool> OnlyWhenAloneInShip;
    public static ConfigEntry<bool> DespawnAfterKill;
    public static ConfigEntry<float> AmbushSpeedMultiplier;
    public static ConfigEntry<bool> PlayBulbShatterOnBlackout;
    public static ConfigEntry<float> BulbShatterVolume;
    public static ConfigEntry<bool> PlayScaryViolinWhenSeen;
    public static ConfigEntry<float> ScaryViolinVolume;
    public static ConfigEntry<float> SeenPauseSeconds;
    public static ConfigEntry<float> RollIntervalSeconds;
    public static ConfigEntry<float> MinSecondsAfterLanding;
    public static ConfigEntry<float> MinDistractedSeconds;
    public static ConfigEntry<Key> HostTestKey;

    public static void Bind(ConfigFile config) {
        const string section = "ShipAmbush";
        Enabled = config.Bind(section, "Enabled", true,
            "Enables the dynamic ship blackout ambush when the ship doors are left open and a lone player inside is watching the computer or monitors.");
        ChancePercent = config.Bind(section, "ChancePercent", 10f,
            "Percentage probability (0-100) for the ship blackout ambush to trigger when checked. Default is 10%.");
        OnlyWhenAloneInShip = config.Bind(section, "OnlyWhenAloneInShip", true,
            "Requires strictly one living player to be inside the ship (completely alone) for the ambush to trigger.");
        DespawnAfterKill = config.Bind(section, "DespawnAfterKill", true,
            "If true, after killing the lone player inside the ship, the ambush Giant vanishes once no living player can see it instead of staying around the ship.");
        AmbushSpeedMultiplier = config.Bind(section, "AmbushSpeedMultiplier", 0.60f,
            "Movement speed multiplier when the Giant charges inside the ship during the ambush (0.60 = 60% of normal speed).");
        PlayBulbShatterOnBlackout = config.Bind(section, "PlayBulbShatterOnBlackout", true,
            "If true, plays an electric pop and shattered lightbulb sound effect when the ship lights snap off.");
        BulbShatterVolume = config.Bind(section, "BulbShatterVolume", 0.95f,
            "Volume (0.0 to 1.0) for the broken lightbulb sound effect.");
        PlayScaryViolinWhenSeen = config.Bind(section, "PlayScaryViolinWhenSeen", true,
            "If true, plays the iconic eerie horror violin music exclusively for the lone player inside the ship when they spot the Giant.");
        ScaryViolinVolume = config.Bind(section, "ScaryViolinVolume", 0.95f,
            "Volume (0.0 to 1.0) for the scary violin audio.");
        SeenPauseSeconds = config.Bind(section, "SeenPauseSeconds", 4.0f,
            "Duration in seconds the Giant stares motionlessly in the doorway after being spotted before charging inside the ship (was 6.0s in v2.1.1, now 4.0s).");
        RollIntervalSeconds = config.Bind(section, "RollIntervalSeconds", 300f,
            "Interval in seconds between automatic 10% ambush rolls once a player is completely alone and distracted inside the ship. Default is 300s (5 minutes).");
        MinSecondsAfterLanding = config.Bind(section, "MinSecondsAfterLanding", 25f,
            "Minimum seconds after the ship lands on a moon before random ambush checks begin.");
        MinDistractedSeconds = config.Bind(section, "MinDistractedSeconds", 2.0f,
            "Seconds a lone player inside the ship must continuously watch the terminal or security monitors before rolling.");
        HostTestKey = config.Bind(section, "HostTestKey", Key.F8,
            "Host-only key to force-trigger the ship blackout ambush for whoever is alone inside the ship (works from anywhere, including terminal, spectator, or ghost mode). Set to None to disable.");
    }
}
