#nullable disable
using BepInEx.Configuration;

namespace RollingGiant;

internal static class EntranceTransitionSettings {
    public static ConfigEntry<float> TransitionDelaySeconds;
    public static ConfigEntry<bool> PlayDoorSoundOnTransition;

    public static void Bind(ConfigFile config) {
        const string section = "FacilityEntranceTransition";
        TransitionDelaySeconds = config.Bind(section, "TransitionDelaySeconds", 2.5f,
            "Cooldown delay in seconds before the Rolling Giant transitions through a facility entrance/exit when chasing a player (mirrors the player door transition time).");
        PlayDoorSoundOnTransition = config.Bind(section, "PlayDoorSoundOnTransition", true,
            "Whether to play the facility door audio cue when the Rolling Giant starts transitioning through an entrance or exit door.");
    }
}
