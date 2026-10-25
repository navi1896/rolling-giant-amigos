#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ControlCompany.Core;
using ControlCompany.Core.Config;
using ControlCompany.Core.Enemy;
using ControlCompany.Core.Input;
using ControlCompany.Core.UI;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RollingGiant.Compatibility;

internal static class ControlCompanyEarlyGuard
{
    private static bool _patched;

    public static void Patch(Harmony harmony)
    {
        if (_patched || harmony == null) return;
        _patched = true;

        var updateMethod = AccessTools.Method(typeof(ConfigManager), "Update");
        if (updateMethod != null)
        {
            harmony.Patch(
                updateMethod,
                prefix: new HarmonyMethod(typeof(ControlCompanyEarlyGuard), nameof(ConfigManagerUpdatePrefix)));
        }
    }

    private static bool ConfigManagerUpdatePrefix()
    {
        return Keyboard.current != null && Mouse.current != null;
    }
}

internal static class ControlCompanyCompat
{
    private static bool _initialized;

    public static void Initialize(Harmony harmony)
    {
        if (_initialized || harmony == null) return;
        if (Keyboard.current == null || Mouse.current == null) return;
        _initialized = true;

        // 1. Register Rolling Giant attributes, allowlist, and custom controller in official ControlCompany
        if (!EnemyConfiguration.ALLOWLISTED_ENEMIES.Contains(typeof(RollingGiantAI)))
        {
            EnemyConfiguration.ALLOWLISTED_ENEMIES.Add(typeof(RollingGiantAI));
        }
        EnemyConfiguration.ENEMY_ATTRIBUTES[typeof(RollingGiantAI)] = new EnemyAttributes
        {
            WalkSpeed = 6f,
            RunSpeed = 10f,
            CamDistance = -5f,
            CamHeight = 3.5f,
            EnemyName = "Rolling Giant"
        };
        EnemyConfiguration.ENEMY_CONTROLLERS[typeof(RollingGiantAI)] =
            controller => controller.AddComponent<RollingGiantEnemyController>();

        // 2. Patch UIManager.HostIsNearOtherPlayers to fix ControlCompany 1.3.3 NullReferenceException in LC V81
        var nearPlayersMethod = AccessTools.Method(typeof(UIManager), "HostIsNearOtherPlayers");
        if (nearPlayersMethod != null)
        {
            harmony.Patch(
                nearPlayersMethod,
                prefix: new HarmonyMethod(typeof(ControlCompanyCompat), nameof(HostIsNearOtherPlayersPrefix)));
        }

        // 3. Patch EnemyConfiguration.IsEnemyAllowlisted to support all modded monsters automatically
        var allowlistedMethod = AccessTools.Method(typeof(EnemyConfiguration), nameof(EnemyConfiguration.IsEnemyAllowlisted));
        if (allowlistedMethod != null)
        {
            harmony.Patch(
                allowlistedMethod,
                prefix: new HarmonyMethod(typeof(ControlCompanyCompat), nameof(IsEnemyAllowlistedPrefix)));
        }

        // 4. Patch ControlCenter lifecycle & round initialization
        var onEnableMethod = AccessTools.Method(typeof(ControlCenter), "OnEnable");
        if (onEnableMethod != null)
        {
            harmony.Patch(
                onEnableMethod,
                postfix: new HarmonyMethod(typeof(ControlCompanyCompat), nameof(EnsureKeybindsLoadedPostfix)));
        }

        var startOfRoundInitMethod = AccessTools.Method(typeof(ControlCenter), nameof(ControlCenter.StartOfRoundInitialization));
        if (startOfRoundInitMethod != null)
        {
            harmony.Patch(
                startOfRoundInitMethod,
                postfix: new HarmonyMethod(typeof(ControlCompanyCompat), nameof(StartOfRoundInitializationPostfix)));
        }

        var inputUpdateMethod = AccessTools.Method(typeof(ControlCenter), "InputUpdate");
        if (inputUpdateMethod != null)
        {
            harmony.Patch(
                inputUpdateMethod,
                postfix: new HarmonyMethod(typeof(ControlCompanyCompat), nameof(InputUpdatePostfix)));
        }

        var extCallMethod = AccessTools.Method(typeof(ControlCompany.Core.Extentions.ExtensionMethods), "Call");
        if (extCallMethod != null)
        {
            harmony.Patch(
                extCallMethod,
                prefix: new HarmonyMethod(typeof(ControlCompanyCompat), nameof(ExtensionCallPrefix)));
        }

        Plugin.Log.LogInfo("ControlCompany compatibility bridge initialized (Rolling Giant + modded monsters + V81 UI fix).");
    }

    private static bool ExtensionCallPrefix(object o, string methodName, object[] args, ref object __result)
    {
        __result = null;
        if (o == null || string.IsNullOrEmpty(methodName)) return false;
        var method = o.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (method != null)
        {
            __result = method.Invoke(o, args);
        }
        return false;
    }

    private static bool HostIsNearOtherPlayersPrefix(PlayerControllerB hostPlayer, ref bool __result)
    {
        __result = false;
        if (hostPlayer == null || ControlCenter.Instance == null)
        {
            return false;
        }

        foreach (var p in ControlCenter.Instance.GetAlivePlayers())
        {
            if (p != null && p != hostPlayer && Vector3.Distance(hostPlayer.transform.position, p.transform.position) < 17f)
            {
                __result = true;
                break;
            }
        }

        return false;
    }

    private static bool IsEnemyAllowlistedPrefix(EnemyAI enemyAI, ref bool __result)
    {
        if (enemyAI == null)
        {
            __result = false;
            return false;
        }

        Type type = enemyAI.GetType();
        __result = EnemyConfiguration.ALLOWLISTED_ENEMIES.Contains(type) || type.Assembly != typeof(EnemyAI).Assembly;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EnsureKeybindsLoadedPostfix()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        if (InputKeys.GHOST_SPAWN_ENEMY_KEYS == null || InputKeys.GHOST_SPAWN_ENEMY_KEYS.Count == 0)
        {
            ConfigManager.Instance?.LoadPlayerConfig(reload: false);
        }

        if (InputKeys.GHOST_SPAWN_ENEMY_KEYS == null || InputKeys.GHOST_SPAWN_ENEMY_KEYS.Count == 0)
        {
            InputKeys.GHOST_SPAWN_ENEMY_KEYS = new List<KeyControl>
            {
                InputKeys.GHOST_SPAWN_ENEMY_1 ?? Keyboard.current.f1Key,
                InputKeys.GHOST_SPAWN_ENEMY_2 ?? Keyboard.current.f2Key,
                InputKeys.GHOST_SPAWN_ENEMY_3 ?? Keyboard.current.f3Key,
                InputKeys.GHOST_SPAWN_ENEMY_4 ?? Keyboard.current.f4Key,
                InputKeys.GHOST_SPAWN_ENEMY_5 ?? Keyboard.current.f5Key,
                InputKeys.GHOST_SPAWN_ENEMY_6 ?? Keyboard.current.f6Key,
                InputKeys.GHOST_SPAWN_ENEMY_7 ?? Keyboard.current.f7Key,
                InputKeys.GHOST_SPAWN_ENEMY_8 ?? Keyboard.current.f8Key,
                InputKeys.GHOST_SPAWN_ENEMY_9 ?? Keyboard.current.f9Key,
                InputKeys.GHOST_SPAWN_ENEMY_10 ?? Keyboard.current.f10Key,
                InputKeys.GHOST_SPAWN_ENEMY_11 ?? Keyboard.current.f11Key,
                InputKeys.GHOST_SPAWN_ENEMY_12 ?? Keyboard.current.f12Key
            };
        }
    }

    private static void StartOfRoundInitializationPostfix(ControlCenter __instance)
    {
        EnsureKeybindsLoadedPostfix();
        if (__instance == null) return;

        if (Plugin.EnemyTypeInside != null && Plugin.EnemyTypeInside.enemyPrefab != null)
        {
            var rgInside = ControllableEnemy.FromSpawnableEnemy(new SpawnableEnemyWithRarity(Plugin.EnemyTypeInside, 100));
            if (rgInside != null)
            {
                InsertFirstUnique(__instance.indoorEnemiesOfLevel, rgInside);
                InsertFirstUnique(__instance.allIndoorEnemies, rgInside);
            }
        }

        if (Plugin.EnemyTypeOutside != null && Plugin.EnemyTypeOutside.enemyPrefab != null)
        {
            var rgOutside = ControllableEnemy.FromSpawnableEnemy(new SpawnableEnemyWithRarity(Plugin.EnemyTypeOutside, 100));
            if (rgOutside != null)
            {
                InsertFirstUnique(__instance.outdoorEnemiesOfLevel, rgOutside);
                InsertFirstUnique(__instance.allOutdoorEnemies, rgOutside);
            }
        }
    }

    private static void InsertFirstUnique(List<ControllableEnemy> list, ControllableEnemy entry)
    {
        if (list == null || entry == null) return;
        list.RemoveAll(e => e == null || e.EnemyName == entry.EnemyName);
        list.Insert(0, entry);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InputUpdatePostfix(ControlCenter __instance)
    {
        if (__instance == null || __instance.CurrentMode != ControlCenter.Mode.HOST || !__instance.isExtendedModeEnabled)
        {
            return;
        }

        if (InputKeys.HOST_GIVE_CREDITS_KEY != null && ((ButtonControl)InputKeys.HOST_GIVE_CREDITS_KEY).wasPressedThisFrame)
        {
            var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            if (terminal != null)
            {
                terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, terminal.numberOfItemsInDropship);
            }
        }
    }
}
