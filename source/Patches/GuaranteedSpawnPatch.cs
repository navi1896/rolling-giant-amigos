using System.Linq;
using HarmonyLib;
using RollingGiant.Settings;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace RollingGiant.Patches;

[HarmonyPatch]
public static class GuaranteedSpawnPatch {
    // The in-game clock starts at 6:00 AM; hour 11 is therefore 5:00 PM.
    private const int FivePmHourIndex = 11;
    private const float RetryDelaySeconds = 5f;

    private static bool _spawnedThisRound;
    private static float _retryAfter;

    [HarmonyPatch(typeof(TimeOfDay), "Update")]
    [HarmonyPostfix]
    private static void SpawnAtFivePm(TimeOfDay __instance) {
        if (!CustomConfig.SpawnGuaranteedAtFivePm) return;

        var startOfRound = StartOfRound.Instance;
        if (startOfRound == null) return;

        // Reset while the ship is between rounds. Company is intentionally excluded,
        // matching the mod's existing in-level guard.
        if (startOfRound.inShipPhase) {
            _spawnedThisRound = false;
            _retryAfter = 0f;
            return;
        }

        if (startOfRound.currentLevelID == 3 || !__instance.currentDayTimeStarted || !__instance.timeHasStarted) return;
        if (_spawnedThisRound || __instance.hour < FivePmHourIndex) return;

        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer) return;
        if (Time.realtimeSinceStartup < _retryAfter) return;

        _retryAfter = Time.realtimeSinceStartup + RetryDelaySeconds;

        try {
            var roundManager = RoundManager.Instance;
            if (roundManager == null || Plugin.EnemyTypeOutside == null) return;

            if (roundManager.SpawnedEnemies.Any(enemy => enemy != null && enemy is RollingGiantAI)) {
                _spawnedThisRound = true;
                return;
            }

            roundManager.GetOutsideAINodes();
            var spawnNodes = Plugin.EnemyTypeOutside.WaterType switch {
                EnemyWaterType.WaterOnly => roundManager.outsideAIWaterNodes,
                EnemyWaterType.LandOnly => roundManager.outsideAIDryNodes,
                _ => roundManager.outsideAINodes
            };

            if (spawnNodes == null || spawnNodes.Length == 0) {
                Plugin.Log.LogWarning("5:00 PM Rolling Giant spawn is waiting for exterior AI nodes.");
                return;
            }

            var node = spawnNodes[Random.Range(0, spawnNodes.Length)];
            if (node == null) return;

            var seed = new System.Random(startOfRound.randomMapSeed + FivePmHourIndex);
            var position = roundManager.GetRandomNavMeshPositionInBoxPredictable(
                node.transform.position,
                10f,
                default(NavMeshHit),
                seed);
            if (position == Vector3.zero) position = node.transform.position;

            // SpawnEnemyGameObject's explicit EnemyType overload is server-only and
            // network-spawns the prefab for every connected player.
            roundManager.SpawnEnemyGameObject(
                position,
                Random.Range(0f, 360f),
                -3,
                Plugin.EnemyTypeOutside);

            _spawnedThisRound = true;
            Plugin.Log.LogInfo($"Spawned the Rolling Giant outside at 5:00 PM (game hour {__instance.hour}).");
        }
        catch (System.Exception exception) {
            Plugin.Log.LogError($"Failed to spawn the scheduled exterior Rolling Giant: {exception}");
        }
    }
}
