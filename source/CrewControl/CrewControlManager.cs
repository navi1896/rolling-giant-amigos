#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace RollingGiant.CrewControl;

internal enum CrewMode {
    Player,
    Ghost,
    Monster
}

/// <summary>
/// Host-only ghost camera + monster possession. Original implementation for the Crew17 package.
/// </summary>
internal sealed class CrewControlManager : MonoBehaviour {
    private const int PageSize = 12;

    public static CrewControlManager Instance { get; private set; }

    public static bool IsActive => Instance != null && Instance._mode != CrewMode.Player;

    public static bool IsPossessed(EnemyAI enemy) {
        return Instance != null
            && Instance._possessed != null
            && Instance._possessed.Enemy == enemy
            && !Instance._possessed.AiControlled;
    }

    private CrewMode _mode = CrewMode.Player;
    private GameObject _rig;
    private Camera _camera;
    private AudioListener _listener;
    private float _yaw;
    private float _pitch;
    private bool _ghostIndoors;
    private int _page;
    private bool _spawning;
    private PossessedEnemy _possessed;

    private readonly List<EnemyType> _indoor = new List<EnemyType>();
    private readonly List<EnemyType> _outdoor = new List<EnemyType>();

    private GUIStyle _rightStyle;
    private GUIStyle _leftStyle;

    public static void Create() {
        if (!CrewControlSettings.Enabled.Value) return;
        if (Chainloader.PluginInfos.ContainsKey("ControlCompany.ControlCompany")) {
            Plugin.Log.LogInfo("ControlCompany detected: Crew Control (built-in ghost mode) stays disabled.");
            return;
        }
        if (Instance != null) return;

        var go = new GameObject("RollingGiant_CrewControl");
        Instance = go.AddComponent<CrewControlManager>();
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------ helpers

    private static PlayerControllerB LocalPlayer {
        get {
            var network = GameNetworkManager.Instance;
            return network != null ? network.localPlayerController : null;
        }
    }

    private static bool IsHost =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

    private static bool RoundActive() {
        var round = StartOfRound.Instance;
        var manager = RoundManager.Instance;
        return round != null && manager != null && manager.currentLevel != null
            && !round.inShipPhase && !round.shipIsLeaving;
    }

    private static string DisplayName(EnemyType type) {
        if (type == Plugin.EnemyTypeInside || type == Plugin.EnemyTypeOutside || type == Plugin.EnemyTypeOutsideDaytime) {
            return "Rolling Giant";
        }
        return type.enemyName;
    }

    // ------------------------------------------------------------------ main loop

    private void Update() {
        var local = LocalPlayer;
        if (StartOfRound.Instance == null || local == null || !IsHost) return;

        var roundActive = RoundActive();

        if (_mode != CrewMode.Player) {
            if (!roundActive || local.isPlayerDead) {
                ExitAll();
                return;
            }
            HoldHostPlayer(local);
            Look();
            if (_mode == CrewMode.Ghost) UpdateGhost();
            else UpdateMonster();
            return;
        }

        if (!roundActive || local.isPlayerDead || local.inTerminalMenu || local.isTypingChat) return;
        if (local.quickMenuManager != null && local.quickMenuManager.isMenuOpen) return;
        if (CrewControlSettings.Pressed(CrewControlSettings.EnterGhostKey.Value)) EnterGhost(local);
    }

    private void LateUpdate() {
        if (_mode == CrewMode.Player || _rig == null) return;

        if (_mode == CrewMode.Monster) {
            if (_possessed == null || !_possessed.IsAlive) {
                BackToGhost();
                return;
            }
            Steer();
            FollowMonster();
        }
    }

    // ------------------------------------------------------------------ ghost mode

    private void EnsureRig() {
        if (_rig != null) return;
        _rig = new GameObject("CrewControl_Camera");
        _camera = _rig.AddComponent<Camera>();
        _listener = _rig.AddComponent<AudioListener>();
        var source = LocalPlayer != null ? LocalPlayer.gameplayCamera : null;
        if (source != null) {
            _camera.cullingMask = source.cullingMask;
            _camera.farClipPlane = source.farClipPlane;
            _camera.fieldOfView = source.fieldOfView;
        }
        _camera.nearClipPlane = 0.1f;
        Object.DontDestroyOnLoad(_rig);
        _rig.SetActive(false);
    }

    private void EnterGhost(PlayerControllerB local) {
        BuildEnemyLists();
        EnsureRig();

        var origin = local.gameplayCamera != null ? local.gameplayCamera.transform : local.transform;
        _yaw = origin.eulerAngles.y;
        _pitch = 0f;
        _rig.transform.position = origin.position;
        _rig.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        _rig.SetActive(true);
        _ghostIndoors = local.isInsideFactory;
        _page = 0;
        _mode = CrewMode.Ghost;
        HoldHostPlayer(local);
    }

    private void HoldHostPlayer(PlayerControllerB local) {
        var round = StartOfRound.Instance;
        local.isTypingChat = true;
        if (round.activeCamera != null) round.activeCamera.enabled = false;
        if (local.activeAudioListener != null) local.activeAudioListener.enabled = false;
        if (_camera != null) _camera.enabled = true;
        if (_listener != null) {
            _listener.enabled = true;
            round.audioListener = _listener;
        }
    }

    private void ReleaseHostPlayer() {
        var local = LocalPlayer;
        var round = StartOfRound.Instance;
        if (_rig != null) _rig.SetActive(false);
        if (local == null || round == null) return;
        local.isTypingChat = false;
        if (round.activeCamera != null) round.activeCamera.enabled = true;
        if (local.activeAudioListener != null) {
            local.activeAudioListener.enabled = true;
            round.audioListener = local.activeAudioListener;
        }
    }

    private void ExitAll() {
        if (_possessed != null) {
            _possessed.Release();
            _possessed = null;
        }
        _spawning = false;
        _mode = CrewMode.Player;
        ReleaseHostPlayer();
    }

    private void Look() {
        var mouse = Mouse.current;
        if (mouse == null) return;
        var delta = mouse.delta.ReadValue();
        var sensitivity = 0.12f * CrewControlSettings.LookSensitivity.Value;
        _yaw += delta.x * sensitivity;
        _pitch = Mathf.Clamp(_pitch - delta.y * sensitivity, -85f, 85f);
    }

    private bool IsIndoors(Vector3 position) {
        try {
            var inside = RoundManager.FindMainEntrancePosition(true, false);
            var outside = RoundManager.FindMainEntrancePosition(true, true);
            return Mathf.Abs(position.y - inside.y) < Mathf.Abs(position.y - outside.y);
        } catch {
            var local = LocalPlayer;
            return local != null && local.isInsideFactory;
        }
    }

    private void UpdateGhost() {
        var keyboard = Keyboard.current;
        var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        var move = Vector3.zero;
        if (keyboard != null) {
            if (keyboard.wKey.isPressed) move += rotation * Vector3.forward;
            if (keyboard.sKey.isPressed) move += rotation * Vector3.back;
            if (keyboard.dKey.isPressed) move += rotation * Vector3.right;
            if (keyboard.aKey.isPressed) move += rotation * Vector3.left;
            if (keyboard.spaceKey.isPressed) move += Vector3.up;
            if (keyboard.leftCtrlKey.isPressed) move += Vector3.down;
        }
        var speed = CrewControlSettings.Held(Key.LeftShift) ? 24f : 9f;
        if (move.sqrMagnitude > 0.01f) _rig.transform.position += move.normalized * (speed * Time.deltaTime);
        _rig.transform.rotation = rotation;
        _ghostIndoors = IsIndoors(_rig.transform.position);

        if (CrewControlSettings.Pressed(CrewControlSettings.ExitKey.Value)) {
            ExitAll();
            return;
        }

        if (CrewControlSettings.Pressed(CrewControlSettings.GhostEntranceKey.Value)) {
            try {
                var target = RoundManager.FindMainEntrancePosition(true, _ghostIndoors);
                _rig.transform.position = target + Vector3.up * 1.5f;
            } catch (System.Exception ex) {
                Plugin.Log.LogWarning($"Crew Control: could not find the main entrance: {ex.Message}");
            }
        }

        var list = _ghostIndoors ? _indoor : _outdoor;
        var pages = Mathf.Max(1, Mathf.CeilToInt(list.Count / (float)PageSize));
        if (CrewControlSettings.Pressed(CrewControlSettings.PagePrevKey.Value)) _page = (_page - 1 + pages) % pages;
        if (CrewControlSettings.Pressed(CrewControlSettings.PageNextKey.Value)) _page = (_page + 1) % pages;
        _page = Mathf.Clamp(_page, 0, pages - 1);

        if (_spawning) return;
        for (var i = 0; i < PageSize; i++) {
            var index = _page * PageSize + i;
            if (index >= list.Count) break;
            if (CrewControlSettings.Pressed(CrewControlSettings.SelectKeys[i])) {
                StartCoroutine(Possess(list[index]));
                break;
            }
        }
    }

    private void BuildEnemyLists() {
        _indoor.Clear();
        _outdoor.Clear();

        static void Add(List<EnemyType> list, EnemyType type) {
            if (type == null || type.enemyPrefab == null) return;
            if (type.enemyPrefab.GetComponentInChildren<EnemyAI>() == null) return;
            if (list.Any(x => x == type || x.enemyName == type.enemyName)) return;
            list.Add(type);
        }

        Add(_indoor, Plugin.EnemyTypeInside);
        Add(_outdoor, Plugin.EnemyTypeOutside);

        var levels = StartOfRound.Instance.levels;
        if (levels == null) return;
        foreach (var level in levels) {
            if (level == null) continue;
            foreach (var entry in level.Enemies) Add(_indoor, entry.enemyType);
            foreach (var entry in level.OutsideEnemies) Add(_outdoor, entry.enemyType);
            foreach (var entry in level.DaytimeEnemies) Add(_outdoor, entry.enemyType);
        }
    }

    // ------------------------------------------------------------------ monster mode

    private IEnumerator Possess(EnemyType type) {
        _spawning = true;
        EnemyAI enemy = null;
        try {
            var position = _rig.transform.position;
            if (NavMesh.SamplePosition(position, out var hit, 25f, NavMesh.AllAreas)) position = hit.position;
            var reference = RoundManager.Instance.SpawnEnemyGameObject(position, _yaw, -1, type);

            var waited = 0f;
            while (waited < 3f) {
                if (reference.TryGet(out var networkObject)) {
                    enemy = networkObject.GetComponentInChildren<EnemyAI>();
                    if (enemy != null && enemy.agent != null) break;
                }
                waited += Time.deltaTime;
                yield return null;
            }
        } finally {
            if (enemy == null) _spawning = false;
        }

        if (enemy == null) {
            Plugin.Log.LogWarning($"Crew Control: could not spawn {DisplayName(type)}.");
            yield break;
        }

        // Let the enemy run its Start() before we take over.
        yield return null;

        _spawning = false;
        if (_mode != CrewMode.Ghost) yield break;
        _possessed = new PossessedEnemy(enemy, DisplayName(type));
        _mode = CrewMode.Monster;
        _pitch = 15f;
        _yaw = enemy.transform.eulerAngles.y;
    }

    private void UpdateMonster() {
        if (_possessed == null) {
            BackToGhost();
            return;
        }

        if (CrewControlSettings.Pressed(CrewControlSettings.ExitKey.Value)) {
            BackToGhost();
            return;
        }

        if (CrewControlSettings.Pressed(CrewControlSettings.ToggleAiKey.Value)) {
            _possessed.SetAiControlled(!_possessed.AiControlled);
        }

        if (_possessed.AiControlled) return;

        if (CrewControlSettings.Pressed(CrewControlSettings.UseDoorKey.Value)) {
            _possessed.TryUseEntrance();
        }

        var mouse = Mouse.current;
        if (mouse != null) {
            if (mouse.leftButton.wasPressedThisFrame) _possessed.PrimaryAction();
            if (_possessed.SupportsForceMove) {
                if (mouse.rightButton.wasPressedThisFrame) _possessed.SetForceMove(true);
                if (mouse.rightButton.wasReleasedThisFrame) _possessed.SetForceMove(false);
            }
        }
    }

    private void Steer() {
        var keyboard = Keyboard.current;
        var forward = Quaternion.Euler(0f, _yaw, 0f);
        var direction = Vector3.zero;
        if (keyboard != null && !_possessed.AiControlled) {
            if (keyboard.wKey.isPressed) direction += forward * Vector3.forward;
            if (keyboard.sKey.isPressed) direction += forward * Vector3.back;
            if (keyboard.dKey.isPressed) direction += forward * Vector3.right;
            if (keyboard.aKey.isPressed) direction += forward * Vector3.left;
        }
        _possessed.Steer(direction, CrewControlSettings.Held(Key.LeftShift));
    }

    private void FollowMonster() {
        var focus = _possessed.Enemy.transform.position + Vector3.up * _possessed.CamHeight;
        var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        var desired = focus - rotation * Vector3.forward * _possessed.CamDistance;
        if (Physics.Linecast(focus, desired, out var hit, StartOfRound.Instance.collidersAndRoomMaskAndDefault,
                QueryTriggerInteraction.Ignore)) {
            desired = hit.point + hit.normal * 0.3f;
        }
        _rig.transform.position = desired;
        _rig.transform.rotation = rotation;
    }

    private void BackToGhost() {
        var position = _rig != null ? _rig.transform.position : Vector3.zero;
        if (_possessed != null) {
            if (_possessed.Enemy != null) position = _possessed.Enemy.transform.position + Vector3.up * 2f;
            _possessed.Release();
            _possessed = null;
        }
        if (_rig != null) _rig.transform.position = position;
        _mode = CrewMode.Ghost;
    }

    // ------------------------------------------------------------------ HUD

    private void OnGUI() {
        var local = LocalPlayer;
        if (local == null || StartOfRound.Instance == null || !IsHost || !RoundActive()) return;
        if (local.isPlayerDead && _mode == CrewMode.Player) return;

        if (_rightStyle == null) {
            _rightStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 24 };
            _leftStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 24 };
        }

        var right = new List<string>();
        var left = new List<string>();

        if (_mode == CrewMode.Player) {
            right.Add($"{CrewControlSettings.EnterGhostKey.Value}: Ghost mode");
        } else if (_mode == CrewMode.Ghost) {
            var list = _ghostIndoors ? _indoor : _outdoor;
            var pages = Mathf.Max(1, Mathf.CeilToInt(list.Count / (float)PageSize));
            left.Add($"Ghost mode ({(_ghostIndoors ? "inside" : "outside")})");
            left.Add("WASD / Space / Ctrl: fly   Shift: fast");
            if (_spawning) left.Add("Summoning...");
            if (pages > 1) {
                right.Add($"{CrewControlSettings.PagePrevKey.Value}/{CrewControlSettings.PageNextKey.Value}: Page {_page + 1}/{pages}");
            }
            for (var i = 0; i < PageSize; i++) {
                var index = _page * PageSize + i;
                if (index >= list.Count) break;
                right.Add($"{CrewControlSettings.SelectKeys[i]}: Control {DisplayName(list[index])}");
            }
            right.Add($"{CrewControlSettings.GhostEntranceKey.Value}: Jump through entrance");
            right.Add($"{CrewControlSettings.ExitKey.Value}: Exit ghost mode");
        } else if (_possessed != null) {
            left.Add($"Controlling: {_possessed.DisplayName}");
            left.Add(_possessed.AiControlled ? "AI controlled" : "WASD: move   Shift: run   Mouse: camera");
            if (_possessed.AiControlled) {
                right.Add($"{CrewControlSettings.ToggleAiKey.Value}: Take control");
            } else {
                if (_possessed.SupportsStopSound) right.Add("LMB: Stop sound");
                if (_possessed.SupportsForceMove) right.Add("RMB (hold): Force move");
                right.Add($"{CrewControlSettings.ToggleAiKey.Value}: AI control");
                right.Add($"{CrewControlSettings.UseDoorKey.Value}: Use entrance");
            }
            right.Add($"{CrewControlSettings.ExitKey.Value}: Back to ghost");
        }

        DrawLines(left, _leftStyle, 32f, 750f);
        DrawLines(right, _rightStyle, Screen.width - 782f, 750f);
    }

    private static void DrawLines(List<string> lines, GUIStyle style, float x, float width) {
        var y = 32f;
        foreach (var line in lines) {
            var shadow = new Color(0f, 0f, 0f, 0.85f);
            var previous = GUI.color;
            GUI.color = shadow;
            GUI.Label(new Rect(x + 1.5f, y + 1.5f, width, 32f), line, style);
            GUI.color = previous;
            GUI.Label(new Rect(x, y, width, 32f), line, style);
            y += 32f;
        }
    }
}
