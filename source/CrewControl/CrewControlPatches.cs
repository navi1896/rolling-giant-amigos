#nullable disable
using GameNetcodeStuff;
using HarmonyLib;

namespace RollingGiant.CrewControl;

/// <summary>
/// Small Harmony hooks used by Crew Control. They are applied one by one so a missing method in a future
/// game version only disables that hook instead of breaking the whole plugin.
/// </summary>
internal static class CrewControlPatches {
    public static void Apply(Harmony harmony) {
        Hook(harmony, typeof(PlayerControllerB), "PlayerLookInput", nameof(BlockLocalLook), prefix: true);
        Hook(harmony, typeof(HUDManager), "CanPlayerScan", nameof(BlockScan), prefix: false);
        Hook(harmony, typeof(EnemyAI), "ChangeOwnershipOfEnemy", nameof(KeepOwnership), prefix: true);
        Hook(harmony, typeof(EnemyAI), "ChangeEnemyOwnerServerRpc", nameof(KeepOwnership), prefix: true);
        Hook(harmony, typeof(TimeOfDay), "VoteShipToLeaveEarly", nameof(BlockWhileActive), prefix: true);
    }

    private static void Hook(Harmony harmony, System.Type type, string method, string handler, bool prefix) {
        try {
            var original = AccessTools.Method(type, method);
            if (original == null) {
                Plugin.Log.LogWarning($"Crew Control: {type.Name}.{method} not found, hook skipped.");
                return;
            }
            var hook = new HarmonyMethod(typeof(CrewControlPatches), handler);
            if (prefix) harmony.Patch(original, prefix: hook);
            else harmony.Patch(original, postfix: hook);
        } catch (System.Exception ex) {
            Plugin.Log.LogWarning($"Crew Control: hook {type.Name}.{method} failed: {ex.Message}");
        }
    }

    private static bool BlockLocalLook(PlayerControllerB __instance) {
        if (!CrewControlManager.IsActive) return true;
        var local = GameNetworkManager.Instance != null ? GameNetworkManager.Instance.localPlayerController : null;
        return __instance != local;
    }

    private static void BlockScan(ref bool __result) {
        if (CrewControlManager.IsActive) __result = false;
    }

    private static bool KeepOwnership(EnemyAI __instance) {
        return !CrewControlManager.IsPossessed(__instance);
    }

    private static bool BlockWhileActive() {
        return !CrewControlManager.IsActive;
    }
}
