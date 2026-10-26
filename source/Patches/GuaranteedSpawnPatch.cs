using HarmonyLib;
using RollingGiant.Settings;
using UnityEngine;
using UnityEngine.AI;

namespace RollingGiant.Patches;

[HarmonyPatch]
public static class GuaranteedSpawnPatch {
    // HUDManager.SetClock starts at 6:00 AM and scales the remaining clock
    // minutes using TimeOfDay.numberOfHours.
    private const int FivePmClockMinute = 17 * 60;
    private const int SixAmClockMinute = 6 * 60;
    private const float RetryDelaySeconds = 5f;

    private static bool _spawnedThisRound;
    private static float _retryAfter;

    [HarmonyPatch(typeof(TimeOfDay), "Update")]
    [HarmonyPostfix]
    private static void SpawnAtFivePm(TimeOfDay __instance) {
        if (!CustomConfig.SpawnGuaranteedAtFivePm) return;

        var startOfRound = StartOfRound.Instance;
        if (startOfRound == null) return;

        // Reset while the ship is between rounds.
        if (startOfRound.inShipPhase) {
            _spawnedThisRound = false;
            _retryAfter = 0f;
            return;
        }

        if (!__instance.currentDayTimeStarted || !__instance.timeHasStarted) return;
        if (_spawnedThisRound || !HasReachedFivePm(__instance)) return;

        var networkManager = Unity.Netcode.NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer) return;
        if (Time.realtimeSinceStartup < _retryAfter) return;

        _retryAfter = Time.realtimeSinceStartup + RetryDelaySeconds;

        try {
            var roundManager = RoundManager.Instance;
            if (roundManager == null || Plugin.EnemyTypeInside == null) return;

            // Refresh after every landing; the RoundManager can retain references
            // to nodes from the previous moon until its new level finishes loading.
            var spawnNodes = GameObject.FindGameObjectsWithTag("AINode");
            roundManager.insideAINodes = spawnNodes;

            if (spawnNodes == null || spawnNodes.Length == 0) {
                Plugin.Log.LogWarning("5:00 PM Rolling Giant spawn is waiting for interior AI nodes.");
                return;
            }

            // Start near the main interior entrance so the Giant can reach the
            // paired exit and follow a player who leaves the facility.
            var entrance = RoundManager.FindMainEntranceScript(getOutsideEntrance: false);
            var anchor = entrance != null
                ? (entrance.entrancePoint != null ? entrance.entrancePoint.position : entrance.transform.position)
                : spawnNodes[0].transform.position;
            GameObject? nearestNode = null;
            var nearestDistance = float.MaxValue;
            foreach (var node in spawnNodes) {
                if (node == null) continue;
                var distance = (node.transform.position - anchor).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearestNode = node;
            }
            if (nearestNode == null) return;

            var seed = new System.Random(startOfRound.randomMapSeed + FivePmClockMinute);
            var candidatePosition = roundManager.GetRandomNavMeshPositionInBoxPredictable(
                nearestNode.transform.position,
                2.5f,
                default(NavMeshHit),
                seed);
            var position = NavMesh.SamplePosition(candidatePosition, out var navHit, 4f, NavMesh.AllAreas)
                ? navHit.position
                : nearestNode.transform.position;

            // SpawnEnemyGameObject's explicit EnemyType overload is server-only and
            // network-spawns the interior prefab for every connected player.
            roundManager.SpawnEnemyGameObject(
                position,
                Random.Range(0f, 360f),
                -1,
                Plugin.EnemyTypeInside);

            _spawnedThisRound = true;
            Plugin.Log.LogInfo($"Spawned the Rolling Giant inside near the main entrance at 5:00 PM (displayed game minute {GetClockMinute(__instance)}).");
        }
        catch (System.Exception exception) {
            Plugin.Log.LogError($"Failed to spawn the scheduled interior Rolling Giant: {exception}");
        }
    }

    private static bool HasReachedFivePm(TimeOfDay timeOfDay) => GetClockMinute(timeOfDay) >= FivePmClockMinute;

    private static int GetClockMinute(TimeOfDay timeOfDay) {
        if (timeOfDay.numberOfHours <= 0) return 0;
        // Keep the same operation order as HUDManager.SetClock so the trigger
        // matches the minute actually displayed to players.
        return (int)(timeOfDay.normalizedTimeOfDay * (60f * timeOfDay.numberOfHours)) + SixAmClockMinute;
    }
}
