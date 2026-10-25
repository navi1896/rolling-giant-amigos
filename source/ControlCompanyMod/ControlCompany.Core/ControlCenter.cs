#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using ControlCompany.Core.Config;
using ControlCompany.Core.Enemy;
using ControlCompany.Core.Enemy.NonStandardEnemy;
using ControlCompany.Core.Extentions;
using ControlCompany.Core.Input;
using ControlCompany.Core.UI;
using Dissonance;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ControlCompany.Core;

internal class ControlCenter : MonoBehaviour
{
	public enum Mode
	{
		HOST,
		GHOST,
		ENEMY
	}

	public static ControlCenter Instance;

	private CustomPlayerController ghostController;

	private GameObject ghost;

	private EnemyController currentControlledEnemy;

	private int currentTargetIndex = 0;

	private PlayerControllerB currentTarget;

	private Mode currentMode = Mode.HOST;

	private bool allowAllEnemies = false;

	private bool initialMicPushToTalkSetting = false;

	private bool isExtendedModeEnabled = false;

	private bool scrapSpawnMode = false;

	private int currentSpawnScrapPage = 0;

	private const int scrapSpawnPageSize = 10;

	private int currentSpawnEnemiesPage = 0;

	private const int spawnEnemiesPageSize = 12;

	private List<ControllableEnemy> currentSpawnableEnemyList = new List<ControllableEnemy>();

	private bool loadedConfig = false;

	private bool showUI = true;

	private float gameStartedTime = 0f;

	private bool inRoundEndPhase = false;

	private bool isGhostIndoors = false;

	private List<ControllableEnemy> allIndoorEnemies = new List<ControllableEnemy>();

	private List<ControllableEnemy> allOutdoorEnemies = new List<ControllableEnemy>();

	private List<ControllableEnemy> indoorEnemiesOfLevel = new List<ControllableEnemy>();

	private List<ControllableEnemy> outdoorEnemiesOfLevel = new List<ControllableEnemy>();

	public Mode CurrentMode => currentMode;

	public EnemyController CurrentControlledEnemy => currentControlledEnemy;

	public bool IsGhostIndoors
	{
		get
		{
			return isGhostIndoors;
		}
		set
		{
			isGhostIndoors = value;
		}
	}

	private void Awake()
	{
		if ((Object)(object)Instance == (Object)null)
		{
			Instance = this;
		}
	}

	private void OnEnable()
	{
		if (!loadedConfig)
		{
			loadedConfig = true;
			ConfigManager.Instance.LoadPlayerConfig(reload: true);
		}
	}

	private void Update()
	{
		if (!IsGameStarted())
		{
			return;
		}
		if (((ButtonControl)InputKeys.TOGGLE_UI_KEY).wasPressedThisFrame)
		{
			showUI = !showUI;
		}
		gameStartedTime += Time.deltaTime;
		if (!IsHost() || inRoundEndPhase)
		{
			return;
		}
		if (((Object)(object)currentTarget == (Object)null || !currentTarget.isPlayerControlled || currentTarget.isPlayerDead) && GetAlivePlayers().Count > 0)
		{
			SetNextTargetPlayer(1);
		}
		if ((Object)(object)ghost == (Object)null)
		{
			GameObject val = new GameObject("ghost");
			ghostController = val.AddComponent<CustomPlayerController>();
			ghostController.Initialize(GetHostPlayer().playerActions, CustomPlayerController.CameraMode.FIRST_PERSON);
			ghost = val;
			ghost.SetActive(false);
			ControlCompanyPlugin.Instance.logger.LogInfo((object)"Creating ghost");
		}
		if (currentMode != Mode.HOST)
		{
			GetHostPlayer().isTypingChat = true;
		}
		if (IsListeningFromHost() && !IsHostDead() && currentMode != Mode.HOST)
		{
			if ((Object)(object)ghostController != (Object)null)
			{
				((Behaviour)ghostController.AudioListener).enabled = false;
			}
			if ((Object)(object)currentControlledEnemy != (Object)null)
			{
				((Behaviour)currentControlledEnemy.AudioListener).enabled = false;
			}
			((Behaviour)GetHostPlayer().activeAudioListener).enabled = true;
			ChangeAudioListener(GetHostPlayer().activeAudioListener);
		}
		else if (currentMode == Mode.GHOST)
		{
			if ((Object)(object)ghostController != (Object)null && ghostController.IsInitialized)
			{
				((Behaviour)GetHostPlayer().activeAudioListener).enabled = false;
				((Behaviour)ghostController.AudioListener).enabled = true;
				ChangeAudioListener(ghostController.AudioListener);
			}
		}
		else if (currentMode == Mode.ENEMY)
		{
			if (((Object)(object)currentControlledEnemy != (Object)null) & currentControlledEnemy.IsInitialized)
			{
				((Behaviour)GetHostPlayer().activeAudioListener).enabled = false;
				((Behaviour)currentControlledEnemy.AudioListener).enabled = true;
				ChangeAudioListener(currentControlledEnemy.AudioListener);
			}
		}
		else if (currentMode == Mode.HOST)
		{
			ChangeAudioListener(GetHostPlayer().activeAudioListener);
		}
		InputUpdate();
	}

	private void InputUpdate()
	{
		if (!IsGameStarted() || !IsHost() || inRoundEndPhase || (ConfigManager.Instance.DisableWhileUsingTerminal && GetHostPlayer().inTerminalMenu))
		{
			return;
		}
		if (((ButtonControl)InputKeys.EXTENDED_MODE_KEY).wasPressedThisFrame)
		{
			isExtendedModeEnabled = !isExtendedModeEnabled;
			allowAllEnemies = isExtendedModeEnabled;
			SetNextSpawnEnemiesPage(0);
		}
		switch (currentMode)
		{
		case Mode.ENEMY:
			if (!currentControlledEnemy.IsAIControlled && currentControlledEnemy.CanControlEnemy())
			{
				if (InputKeys.ENEMY_PRIMARY_SKILL_BUTTON.wasPressedThisFrame)
				{
					currentControlledEnemy.UsePrimarySkill();
				}
				else if (InputKeys.ENEMY_PRIMARY_SKILL_BUTTON.wasReleasedThisFrame)
				{
					currentControlledEnemy.ReleasePrimarySkill();
				}
				else if (InputKeys.ENEMY_SECONDARY_SKILL_BUTTON.wasPressedThisFrame)
				{
					currentControlledEnemy.UseSecondarySkill();
				}
				else if (InputKeys.ENEMY_SECONDARY_SKILL_BUTTON.wasReleasedThisFrame)
				{
					currentControlledEnemy.ReleaseSecondarySkill();
				}
				currentControlledEnemy.SetLockEnemyRotation(((ButtonControl)InputKeys.LOCK_ENEMY_ROTATION_KEY).isPressed);
			}
			if (((ButtonControl)InputKeys.EXIT_KEY).wasPressedThisFrame)
			{
				EnableGhost(enable: true, ((Component)currentControlledEnemy).transform.position);
				ghostController.EnableLight(isGhostIndoors);
				currentControlledEnemy.DestroyAndCleanUp();
				currentMode = Mode.GHOST;
			}
			else if (((ButtonControl)InputKeys.ENEMY_TOGGLE_AI_KEY).wasPressedThisFrame)
			{
				currentControlledEnemy.ToggleAIControl();
			}
			else if (((ButtonControl)InputKeys.ENEMY_TOGGLE_LIGHT_KEY).wasPressedThisFrame)
			{
				currentControlledEnemy.ToggleLight();
			}
			break;
		case Mode.HOST:
			if (((ButtonControl)InputKeys.HOST_GHOST_MODE_KEY).wasPressedThisFrame)
			{
				isGhostIndoors = IsTargetInside();
				EnableGhost(enable: true, ((Component)currentTarget).gameObject.transform.position);
				ghostController.EnableLight(isGhostIndoors);
				EnableHostPlayer(enable: false);
				currentMode = Mode.GHOST;
			}
			else if (((ButtonControl)InputKeys.CYCLE_KEY_LEFT).wasPressedThisFrame)
			{
				SetNextTargetPlayer(-1);
			}
			else if (((ButtonControl)InputKeys.CYCLE_KEY_RIGHT).wasPressedThisFrame)
			{
				SetNextTargetPlayer(1);
			}
			if (!isExtendedModeEnabled)
			{
				break;
			}
			if (((ButtonControl)InputKeys.HOST_TELEPORT_TO_ENTRANCE_KEY).wasPressedThisFrame)
			{
				if (!GetHostPlayer().isPlayerDead)
				{
					EntranceTeleport mainEntrance = GetMainEntrance(getMainEntranceThatIsOutside: true);
					if ((Object)(object)mainEntrance != (Object)null)
					{
						mainEntrance.TeleportPlayer();
					}
				}
			}
			else if (((ButtonControl)InputKeys.HOST_GIVE_CREDITS_KEY).wasPressedThisFrame)
			{
				Terminal obj = Object.FindObjectOfType<Terminal>();
				if (obj != null)
				{
					obj.groupCredits += 100;
					obj.SyncGroupCreditsServerRpc(obj.groupCredits, obj.numberOfItemsInDropship);
				}
			}
			break;
		case Mode.GHOST:
			if (!scrapSpawnMode)
			{
				List<ControllableEnemy> pagedSpawnableEnemies = GetPagedSpawnableEnemies();
				for (int i = 0; i < InputKeys.GHOST_SPAWN_ENEMY_KEYS.Count && i < pagedSpawnableEnemies.Count; i++)
				{
					if (!((ButtonControl)InputKeys.GHOST_SPAWN_ENEMY_KEYS[i]).wasPressedThisFrame)
					{
						continue;
					}
					if (ConfigManager.Instance.UseEnemySpawningBehaviour)
					{
						System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 888);
						if (isGhostIndoors)
						{
							RoundManager.Instance.RefreshEnemyVents();
							EnemyVent[] allEnemyVents = RoundManager.Instance.allEnemyVents;
							if (allEnemyVents.Count() > 0)
							{
								EnemyVent val = allEnemyVents[random.Next(allEnemyVents.Count())];
								currentControlledEnemy = SpawnControllableEnemy(pagedSpawnableEnemies[i], val.floorNode.position, val.floorNode.rotation);
							}
							else
							{
								currentControlledEnemy = SpawnControllableEnemy(pagedSpawnableEnemies[i], ghost.transform.position, ghost.transform.rotation);
							}
						}
						else
						{
							GameObject[] array = GameObject.FindGameObjectsWithTag("OutsideAINode");
							if (array.Count() > 0)
							{
								currentControlledEnemy = SpawnControllableEnemy(pagedSpawnableEnemies[i], array[random.Next(array.Count())].transform.position, ghost.transform.rotation);
							}
							else
							{
								currentControlledEnemy = SpawnControllableEnemy(pagedSpawnableEnemies[i], ghost.transform.position, ghost.transform.rotation);
							}
						}
					}
					else
					{
						currentControlledEnemy = SpawnControllableEnemy(pagedSpawnableEnemies[i], ghost.transform.position, ghost.transform.rotation);
					}
					EnableGhost(enable: false);
					currentMode = Mode.ENEMY;
					break;
				}
			}
			else
			{
				List<Item> list = GetSpawnableScraps().Chunk(10).ToList()[currentSpawnScrapPage].ToList();
				for (int j = 0; j < InputKeys.GHOST_SPAWN_ENEMY_KEYS.Count && j < list.Count; j++)
				{
					if (((ButtonControl)InputKeys.GHOST_SPAWN_ENEMY_KEYS[j]).wasPressedThisFrame)
					{
						SpawnScrap(list[j], ghost.transform.position);
						break;
					}
				}
			}
			if (((ButtonControl)InputKeys.EXIT_KEY).wasPressedThisFrame)
			{
				EnableGhost(enable: false);
				EnableHostPlayer(enable: true);
				currentMode = Mode.HOST;
				GetHostPlayer().isTypingChat = false;
			}
			else if (((ButtonControl)InputKeys.CYCLE_KEY_LEFT).wasPressedThisFrame)
			{
				SetNextSpawnEnemiesPage(-1);
			}
			else if (((ButtonControl)InputKeys.CYCLE_KEY_RIGHT).wasPressedThisFrame)
			{
				SetNextSpawnEnemiesPage(1);
			}
			if (((ButtonControl)Keyboard.current.upArrowKey).wasPressedThisFrame)
			{
			}
			if (scrapSpawnMode && ((ButtonControl)Keyboard.current.downArrowKey).wasPressedThisFrame)
			{
				int num = Mathf.CeilToInt((float)GetSpawnableScraps().Count / 10f);
				ControlCompanyPlugin.Instance.logger.LogMessage((object)$"Total scraps: {GetSpawnableScraps().Count()}, final page of scrap: {num}");
				currentSpawnScrapPage++;
				if (currentSpawnScrapPage >= num)
				{
					currentSpawnScrapPage = 0;
				}
			}
			break;
		}
	}

	private void ChangeAudioListener(AudioListener audioListener)
	{
		StartOfRound.Instance.audioListener = audioListener;
	}

	private EntranceTeleport GetMainEntrance(bool getMainEntranceThatIsOutside)
	{
		EntranceTeleport[] array = Object.FindObjectsOfType<EntranceTeleport>(false);
		foreach (EntranceTeleport val in array)
		{
			if (val.entranceId != 0)
			{
				continue;
			}
			if (getMainEntranceThatIsOutside)
			{
				if (val.isEntranceToBuilding)
				{
					return val;
				}
			}
			else if (!val.isEntranceToBuilding)
			{
				return val;
			}
		}
		return null;
	}

	private bool IsTargetInside()
	{
		return (Object)(object)currentTarget != (Object)null && currentTarget.isInsideFactory;
	}

	private void OnGUI()
	{
		if (!IsGameStarted() || !showUI)
		{
			return;
		}
		if (!IsHost())
		{
			if (gameStartedTime < 30f)
			{
				UIManager.Instance.RenderNotHostLabel(((InputControl)InputKeys.TOGGLE_UI_KEY).displayName);
			}
		}
		else
		{
			if (inRoundEndPhase || (ConfigManager.Instance.DisableWhileUsingTerminal && GetHostPlayer().inTerminalMenu))
			{
				return;
			}
			switch (currentMode)
			{
			case Mode.ENEMY:
				UIManager.Instance.RenderEnemyControls(((InputControl)InputKeys.ENEMY_PRIMARY_SKILL_BUTTON).shortDisplayName, ((InputControl)InputKeys.ENEMY_SECONDARY_SKILL_BUTTON).shortDisplayName, ((InputControl)InputKeys.ENEMY_TOGGLE_AI_KEY).displayName, ((InputControl)InputKeys.EXIT_KEY).displayName, ((InputControl)InputKeys.HOST_LISTEN_KEY).displayName, currentControlledEnemy, GetHostPlayer());
				break;
			case Mode.HOST:
				UIManager.Instance.RenderHostControls(((InputControl)InputKeys.HOST_GHOST_MODE_KEY).displayName, ((InputControl)InputKeys.CYCLE_KEY_LEFT).displayName, ((InputControl)InputKeys.CYCLE_KEY_RIGHT).displayName, currentTarget, isExtendedModeEnabled, ((InputControl)InputKeys.HOST_GIVE_CREDITS_KEY).displayName, ((InputControl)InputKeys.HOST_TELEPORT_TO_ENTRANCE_KEY).displayName, ((InputControl)InputKeys.TOGGLE_UI_KEY).displayName);
				break;
			case Mode.GHOST:
			{
				bool showEnemyPaging = GetSpawnableEnemies(isGhostIndoors).Count > 12;
				List<IEnumerable<Item>> list = GetSpawnableScraps().Chunk(10).ToList();
				List<Item> scraps = ((list.Count > 0) ? list[currentSpawnScrapPage].ToList() : new List<Item>());
				UIManager.Instance.RenderGhostControls(InputKeys.GHOST_SPAWN_ENEMY_KEYS.Select((KeyControl key) => ((InputControl)key).displayName).ToList(), ((InputControl)InputKeys.EXIT_KEY).displayName, ((InputControl)InputKeys.CYCLE_KEY_LEFT).displayName, ((InputControl)InputKeys.CYCLE_KEY_RIGHT).displayName, ((InputControl)InputKeys.HOST_LISTEN_KEY).displayName, showEnemyPaging, GetPagedSpawnableEnemies(), scraps, GetHostPlayer(), scrapSpawnMode);
				break;
			}
			}
		}
	}

	private bool IsHostDead()
	{
		return GetHostPlayer().isPlayerDead;
	}

	private void EnableGhost(bool enable)
	{
		EnableGhost(enable, Vector3.zero);
	}

	private void EnableGhost(bool enable, Vector3 position)
	{
		if (enable)
		{
			ghost.transform.position = position + new Vector3(0f, 0.5f, 0f);
			ghost.transform.rotation = Quaternion.identity;
			ghost.SetActive(true);
			ChangeAudioListener(ghostController.AudioListener);
			SetNextSpawnEnemiesPage(0);
		}
		else
		{
			ghost.SetActive(false);
		}
	}

	private void EnableHostPlayer(bool enable)
	{
		PlayerControllerB hostPlayer = GetHostPlayer();
		PlayerActions.MovementActions movement;
		if (enable)
		{
			if (ConfigManager.Instance.ForcePushToTalkWhileInMonsterMode)
			{
				ResetMicPushToTalk();
			}
			((Behaviour)GetHostPlayer().activeAudioListener).enabled = true;
			movement = hostPlayer.playerActions.Movement;
			movement.PingScan.Enable();
			ChangeAudioListener(hostPlayer.activeAudioListener);
			((Behaviour)StartOfRound.Instance.activeCamera).enabled = true;
		}
		else
		{
			if (ConfigManager.Instance.ForcePushToTalkWhileInMonsterMode)
			{
				ForceMicPushToTalk();
			}
			((Behaviour)GetHostPlayer().activeAudioListener).enabled = false;
			movement = hostPlayer.playerActions.Movement;
			movement.PingScan.Disable();
			((Behaviour)StartOfRound.Instance.activeCamera).enabled = false;
		}
	}

	private void EnablePlayer2DMics(bool enable, List<PlayerControllerB> players)
	{
		foreach (PlayerControllerB player in players)
		{
			if (player.isHostPlayerObject)
			{
				continue;
			}
			AudioSource currentVoiceChatAudioSource = player.currentVoiceChatAudioSource;
			if (enable)
			{
				if ((Object)(object)currentVoiceChatAudioSource != (Object)null)
				{
					currentVoiceChatAudioSource.spatialBlend = 0f;
				}
				player.currentVoiceChatIngameSettings.set2D = true;
				player.voicePlayerState.Volume = 1f;
			}
			else
			{
				if ((Object)(object)currentVoiceChatAudioSource != (Object)null)
				{
					currentVoiceChatAudioSource.spatialBlend = 1f;
				}
				player.currentVoiceChatIngameSettings.set2D = false;
				player.voicePlayerState.Volume = 0f;
			}
		}
	}

	private void SetNextTargetPlayer(int direction)
	{
		List<PlayerControllerB> alivePlayers = GetAlivePlayers();
		if (alivePlayers.Count == 0)
		{
			SetCurrentTargetToHost();
			ControlCompanyPlugin.Instance.logger.LogMessage((object)"Alive player count is zero");
		}
		else
		{
			currentTargetIndex += direction;
			if (currentTargetIndex >= alivePlayers.Count)
			{
				currentTargetIndex = 0;
			}
			else if (currentTargetIndex < 0)
			{
				currentTargetIndex = alivePlayers.Count - 1;
			}
			currentTargetIndex = Mathf.Clamp(currentTargetIndex, 0, alivePlayers.Count - 1);
			currentTarget = alivePlayers[currentTargetIndex];
		}
		ControlCompanyPlugin.Instance.logger.LogMessage((object)$"Current target index: {currentTargetIndex}");
		ControlCompanyPlugin.Instance.logger.LogMessage((object)("Setting next target to: " + currentTarget?.playerUsername));
	}

	public void SetCurrentTargetToHost()
	{
		currentTargetIndex = 0;
		currentTarget = GetHostPlayer();
		ControlCompanyPlugin.Instance.logger.LogMessage((object)"Setting next target to host");
	}

	private void SetNextSpawnEnemiesPage(int direction)
	{
		List<ControllableEnemy> spawnableEnemies = GetSpawnableEnemies(isGhostIndoors);
		if (spawnableEnemies == null || spawnableEnemies.Count == 0)
		{
			currentSpawnableEnemyList = new List<ControllableEnemy>();
			return;
		}
		if (spawnableEnemies.Count > 12)
		{
			List<IEnumerable<ControllableEnemy>> list = spawnableEnemies.Chunk(12).ToList();
			currentSpawnEnemiesPage += direction;
			if (currentSpawnEnemiesPage >= list.Count)
			{
				currentSpawnEnemiesPage = 0;
			}
			else if (currentSpawnEnemiesPage < 0)
			{
				currentSpawnEnemiesPage = list.Count - 1;
			}
			currentSpawnEnemiesPage = Mathf.Clamp(currentSpawnEnemiesPage, 0, list.Count - 1);
		}
		else
		{
			currentSpawnEnemiesPage = 0;
		}
		currentSpawnableEnemyList = GetSpawnableEnemies(isGhostIndoors).Chunk(12).ToList()[currentSpawnEnemiesPage].ToList();
	}

	public List<SpawnableEnemyWithRarity> GetDaytimeEnemies()
	{
		return new List<SpawnableEnemyWithRarity>(GetCurrentLevel().DaytimeEnemies);
	}

	public void StartOfRoundInitialization()
	{
		initialMicPushToTalkSetting = IngamePlayerSettings.Instance.settings.pushToTalk;
		allIndoorEnemies = GetAllIndoorEnemiesAcrossAllMapsList();
		allOutdoorEnemies = GetAllOutdoorEnemiesAcrossAllMapsList();
		indoorEnemiesOfLevel = new List<ControllableEnemy>(from enemy in GetCurrentLevel().Enemies
			select ControllableEnemy.FromSpawnableEnemy(enemy) into x
			where x != null
			select x);
		outdoorEnemiesOfLevel = new List<ControllableEnemy>(from enemy in GetCurrentLevel().OutsideEnemies.Concat(GetCurrentLevel().DaytimeEnemies)
			select ControllableEnemy.FromSpawnableEnemy(enemy) into x
			where x != null
			select x);
		ControllableEnemy maskedEnemy = GetMaskedEnemy();
		indoorEnemiesOfLevel.Add(maskedEnemy);
		outdoorEnemiesOfLevel.Add(maskedEnemy);
		allIndoorEnemies.Add(maskedEnemy);
		allOutdoorEnemies.Add(maskedEnemy);
		ControllableEnemy companyMonsterEnemy = GetCompanyMonsterEnemy();
		if (companyMonsterEnemy != null)
		{
			indoorEnemiesOfLevel.Add(companyMonsterEnemy);
			outdoorEnemiesOfLevel.Add(companyMonsterEnemy);
			allIndoorEnemies.Add(companyMonsterEnemy);
			allOutdoorEnemies.Add(companyMonsterEnemy);
		}
		ControllableEnemy secretEnemy = GetSecretEnemy();
		if (secretEnemy != null)
		{
			indoorEnemiesOfLevel.Insert(0, secretEnemy);
			allIndoorEnemies.Insert(0, secretEnemy);
		}
		if (RollingGiant.Plugin.EnemyTypeInside != null && RollingGiant.Plugin.EnemyTypeInside.enemyPrefab != null)
		{
			EnemyAI rgInsideAI = RollingGiant.Plugin.EnemyTypeInside.enemyPrefab.GetComponentInChildren<EnemyAI>();
			if (rgInsideAI != null)
			{
				ControllableEnemy rgInside = ControllableEnemy.FromEnemyAI(rgInsideAI);
				if (rgInside != null)
				{
					indoorEnemiesOfLevel.Insert(0, rgInside);
					allIndoorEnemies.Insert(0, rgInside);
				}
			}
		}
		if (RollingGiant.Plugin.EnemyTypeOutside != null && RollingGiant.Plugin.EnemyTypeOutside.enemyPrefab != null)
		{
			EnemyAI rgOutsideAI = RollingGiant.Plugin.EnemyTypeOutside.enemyPrefab.GetComponentInChildren<EnemyAI>();
			if (rgOutsideAI != null)
			{
				ControllableEnemy rgOutside = ControllableEnemy.FromEnemyAI(rgOutsideAI);
				if (rgOutside != null)
				{
					outdoorEnemiesOfLevel.Insert(0, rgOutside);
					allOutdoorEnemies.Insert(0, rgOutside);
				}
			}
		}
		indoorEnemiesOfLevel = indoorEnemiesOfLevel.DedupeBy((ControllableEnemy enemy) => enemy.EnemyName).ToList();
		outdoorEnemiesOfLevel = outdoorEnemiesOfLevel.DedupeBy((ControllableEnemy enemy) => enemy.EnemyName).ToList();
		allIndoorEnemies = allIndoorEnemies.DedupeBy((ControllableEnemy enemy) => enemy.EnemyName).ToList();
		allOutdoorEnemies = allOutdoorEnemies.DedupeBy((ControllableEnemy enemy) => enemy.EnemyName).ToList();
		gameStartedTime = 0f;
		SetCurrentTargetToHost();
		ResetToDefault();
		inRoundEndPhase = false;
		ControlCompanyPlugin.Instance.logger.LogMessage((object)"Start of round initialization completed.");
	}

	public void EndOfRoundCleanUp()
	{
		ResetToDefault();
		inRoundEndPhase = true;
		ControlCompanyPlugin.Instance.logger.LogMessage((object)"End of round clean up completed.");
	}

	private void ResetToDefault()
	{
		StartOfRound.Instance.audioListener = GetHostPlayer().activeAudioListener;
		GetHostPlayer().isTypingChat = false;
		currentMode = Mode.HOST;
		if ((Object)(object)ghost != (Object)null)
		{
			Object.Destroy((Object)(object)ghost);
			ghost = null;
		}
		if ((Object)(object)currentControlledEnemy != (Object)null)
		{
			currentControlledEnemy.DestroyAndCleanUp();
		}
		currentSpawnableEnemyList = new List<ControllableEnemy>();
		currentSpawnEnemiesPage = 0;
		EnableHostPlayer(enable: true);
		ControlCompanyPlugin.Instance.logger.LogMessage((object)"Reset to default.");
	}

	public bool IsListeningFromHost()
	{
		return ((ButtonControl)InputKeys.HOST_LISTEN_KEY).isPressed;
	}

	public PlayerControllerB GetHostPlayer()
	{
		return RoundManager.Instance.playersManager.allPlayerObjects[0].GetComponentInChildren<PlayerControllerB>();
	}

	public List<PlayerControllerB> GetDeadPlayers()
	{
		return (from player in GetAllPlayers()
			where player.isPlayerDead
			select player).ToList();
	}

	public List<PlayerControllerB> GetAlivePlayers()
	{
		return (from player in GetAllPlayers()
			where !player.isPlayerDead
			select player).ToList();
	}

	public List<PlayerControllerB> GetAllPlayers()
	{
		List<PlayerControllerB> list = new List<PlayerControllerB>();
		GameObject[] allPlayerObjects = RoundManager.Instance.playersManager.allPlayerObjects;
		for (int i = 0; i < allPlayerObjects.Length; i++)
		{
			PlayerControllerB componentInChildren = allPlayerObjects[i].GetComponentInChildren<PlayerControllerB>();
			if ((Object)(object)componentInChildren != (Object)null && componentInChildren.isPlayerControlled)
			{
				list.Add(componentInChildren);
			}
		}
		return list;
	}

	public bool DoesCurrentLevelExist()
	{
		return (Object)(object)GetCurrentLevel() != (Object)null;
	}

	public bool IsGameStarted()
	{
		return (Object)(object)RoundManager.Instance != (Object)null && (Object)(object)RoundManager.Instance.currentLevel != (Object)null && (Object)(object)GameNetworkManager.Instance != (Object)null && GameNetworkManager.Instance.gameHasStarted && (Object)(object)StartOfRound.Instance != (Object)null && !StartOfRound.Instance.inShipPhase;
	}

	public SelectableLevel GetCurrentLevel()
	{
		return RoundManager.Instance.currentLevel;
	}

	public StartOfRound GetPlayersManager()
	{
		return RoundManager.Instance.playersManager;
	}

	private bool IsHost()
	{
		return ((NetworkBehaviour)RoundManager.Instance).IsHost;
	}

	public List<Item> GetSpawnableScraps()
	{
		return StartOfRound.Instance.allItemsList.itemsList;
	}

	public List<ControllableEnemy> GetPagedSpawnableEnemies()
	{
		return currentSpawnableEnemyList;
	}

	public List<ControllableEnemy> GetSpawnableEnemies(bool isIndoor)
	{
		List<ControllableEnemy> list = ((!isIndoor) ? GetSpawnableOutdoorEnemies() : GetSpawnableIndoorEnemies());
		if (list == null || list.Count == 0)
		{
			list = new List<ControllableEnemy>();
		}
		return list;
	}

	public List<ControllableEnemy> GetSpawnableIndoorEnemies()
	{
		return allowAllEnemies ? GetIndoorEnemyList() : GetAllowListedIndoorEnemies();
	}

	public List<ControllableEnemy> GetAllowListedIndoorEnemies()
	{
		return (from enemy in GetIndoorEnemyList()
			where enemy.Allowlisted
			select enemy).ToList();
	}

	public List<ControllableEnemy> GetIndoorEnemyList()
	{
		return allowAllEnemies ? allIndoorEnemies : indoorEnemiesOfLevel;
	}

	public List<ControllableEnemy> GetAllIndoorEnemiesAcrossAllMapsList()
	{
		HashSet<string> hashSet = new HashSet<string>();
		List<ControllableEnemy> list = new List<ControllableEnemy>();
		SelectableLevel[] levels = StartOfRound.Instance.levels;
		foreach (SelectableLevel val in levels)
		{
			if (!((Object)(object)val != (Object)null))
			{
				continue;
			}
			foreach (SpawnableEnemyWithRarity enemy in val.Enemies)
			{
				if ((Object)(object)enemy.enemyType != (Object)null && !hashSet.Contains(enemy.enemyType.enemyName))
				{
					hashSet.Add(enemy.enemyType.enemyName);
					ControllableEnemy controllableEnemy = ControllableEnemy.FromSpawnableEnemy(enemy);
					if (controllableEnemy != null)
					{
						list.Add(controllableEnemy);
					}
				}
			}
		}
		return list;
	}

	public List<ControllableEnemy> GetSpawnableOutdoorEnemies()
	{
		return allowAllEnemies ? GetOutdoorEnemyList() : GetAllowListedOutdoorEnemies();
	}

	public List<ControllableEnemy> GetAllowListedOutdoorEnemies()
	{
		return (from enemy in GetOutdoorEnemyList()
			where enemy.Allowlisted
			select enemy).ToList();
	}

	public List<ControllableEnemy> GetOutdoorEnemyList()
	{
		return allowAllEnemies ? allOutdoorEnemies : outdoorEnemiesOfLevel;
	}

	public List<ControllableEnemy> GetAllOutdoorEnemiesAcrossAllMapsList()
	{
		if (GetCurrentLevel().OutsideEnemies.Count == 0)
		{
			return new List<ControllableEnemy>();
		}
		HashSet<string> hashSet = new HashSet<string>();
		List<ControllableEnemy> list = new List<ControllableEnemy>();
		SelectableLevel[] levels = StartOfRound.Instance.levels;
		foreach (SelectableLevel val in levels)
		{
			if (!((Object)(object)val != (Object)null))
			{
				continue;
			}
			IEnumerable<SpawnableEnemyWithRarity> enumerable = val.OutsideEnemies.Concat(val.DaytimeEnemies);
			foreach (SpawnableEnemyWithRarity item in enumerable)
			{
				if ((Object)(object)item.enemyType != (Object)null && !hashSet.Contains(item.enemyType.enemyName))
				{
					hashSet.Add(item.enemyType.enemyName);
					ControllableEnemy controllableEnemy = ControllableEnemy.FromSpawnableEnemy(item);
					if (controllableEnemy != null)
					{
						list.Add(controllableEnemy);
					}
				}
			}
		}
		return list;
	}

	public ControllableEnemy GetMaskedEnemy()
	{
		SelectableLevel[] levels = StartOfRound.Instance.levels;
		foreach (SelectableLevel val in levels)
		{
			foreach (SpawnableEnemyWithRarity enemy in val.Enemies)
			{
				EnemyType enemyType = enemy.enemyType;
				object obj;
				if (enemyType == null)
				{
					obj = null;
				}
				else
				{
					GameObject enemyPrefab = enemyType.enemyPrefab;
					obj = ((enemyPrefab != null) ? enemyPrefab.GetComponent<EnemyAI>() : null);
				}
				EnemyAI val2 = (EnemyAI)obj;
				if ((Object)(object)val2 != (Object)null && ((object)val2).GetType() == typeof(MaskedPlayerEnemy))
				{
					return ControllableEnemy.FromSpawnableEnemy(enemy);
				}
			}
		}
		return null;
	}

	private ControllableEnemy GetSecretEnemy()
	{
		Object[] array = Resources.FindObjectsOfTypeAll(typeof(EnemyAI));
		Object[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			EnemyAI val = (EnemyAI)array2[i];
			if (((object)val).GetType() == typeof(TestEnemy))
			{
				return ControllableEnemy.FromEnemyAI(val);
			}
		}
		return null;
	}

	private GameObject SpawnScrap(Item scrap, Vector3 position)
	{
		GameObject val = Object.Instantiate<GameObject>(scrap.spawnPrefab, position, Quaternion.identity);
		GrabbableObject componentInChildren = val.GetComponentInChildren<GrabbableObject>();
		((Component)componentInChildren).transform.rotation = Quaternion.Euler(componentInChildren.itemProperties.restingRotation);
		val.GetComponentInChildren<NetworkObject>().Spawn(true);
		return val;
	}

	private EnemyController SpawnControllableEnemy(ControllableEnemy controllableEnemy, Vector3 position, Quaternion rotation)
	{
		Vector3 val = (controllableEnemy.OverrideEnemySpawnLocation ? controllableEnemy.EnemySpawnLocationOverride : (position + new Vector3(0f, 0.5f, 0f)));
		GameObject val2 = (controllableEnemy.OverrideEnemyPrefab ? controllableEnemy.EnemyGameObject : Object.Instantiate<GameObject>(controllableEnemy.EnemyPrefab, val, Quaternion.identity));
		if (!controllableEnemy.OverrideEnemyPrefab)
		{
			val2.GetComponentInChildren<NetworkObject>().Spawn(true);
		}
		GameObject val3 = new GameObject("enemy_controller");
		val3.transform.position = val;
		val3.transform.rotation = rotation;
		if (controllableEnemy.CreateAndAttachEnemyControllerFunc == null)
		{
			throw new Exception("Controllable enemy does not have a function defined to create and attach an enemy controller!");
		}
		EnemyController enemyController = controllableEnemy.CreateAndAttachEnemyControllerFunc(val2, val3);
		enemyController.InitializeEnemyController(GetHostPlayer().playerActions, val2);
		enemyController.EnableLight(isGhostIndoors);
		ChangeAudioListener(enemyController.AudioListener);
		return enemyController;
	}

	private GameObject GetTurretPrefab()
	{
		SelectableLevel[] levels = StartOfRound.Instance.levels;
		foreach (SelectableLevel val in levels)
		{
			SpawnableMapObject[] spawnableMapObjects = val.spawnableMapObjects;
			foreach (SpawnableMapObject val2 in spawnableMapObjects)
			{
				object obj;
				if (val2 == null)
				{
					obj = null;
				}
				else
				{
					GameObject prefabToSpawn = val2.prefabToSpawn;
					obj = ((prefabToSpawn != null) ? prefabToSpawn.GetComponentInChildren<Turret>() : null);
				}
				if ((Object)obj != (Object)null)
				{
					return val2.prefabToSpawn;
				}
			}
		}
		return null;
	}

	private EnemyController SpawnControllableTurret(Vector3 position, Quaternion rotation)
	{
		GameObject turretPrefab = GetTurretPrefab();
		if ((Object)(object)turretPrefab == (Object)null)
		{
			ControlCompanyPlugin.Instance.logger.LogDebug((object)"Cannot find turret prefab in current level");
			return null;
		}
		GameObject val = Object.Instantiate<GameObject>(turretPrefab, position, rotation);
		val.GetComponentInChildren<NetworkObject>().Spawn(true);
		GameObject val2 = new GameObject("enemy_controller");
		val2.transform.position = position + new Vector3(0f, 0.5f, 0f);
		val2.transform.rotation = rotation;
		TurretEnemyController turretEnemyController = val2.AddComponent<TurretEnemyController>();
		turretEnemyController.InitializeEnemyController(GetHostPlayer().playerActions, val);
		turretEnemyController.EnableLight(enable: true);
		return turretEnemyController;
	}

	private ControllableEnemy GetCompanyMonsterEnemy()
	{
		DepositItemsDesk val = Object.FindObjectOfType<DepositItemsDesk>();
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		GameObject val2 = GameObject.Find("BellDinger");
		return new ControllableEnemy
		{
			EnemyName = "Company Monster",
			OverrideEnemyPrefab = true,
			EnemyGameObject = ((Component)val).gameObject,
			Allowlisted = true,
			OverrideEnemySpawnLocation = true,
			EnemySpawnLocationOverride = (((Object)(object)val2 != (Object)null) ? val2.transform.position : ((Component)val).transform.position),
			CreateAndAttachEnemyControllerFunc = (GameObject enemyGameObject, GameObject enemyController) => enemyController.AddComponent<CompanyMonsterController>()
		};
	}

	private DissonanceComms GetMic()
	{
		return StartOfRound.Instance.voiceChatModule;
	}

	private void ForceMicPushToTalk()
	{
		IngamePlayerSettings.Instance.settings.pushToTalk = true;
	}

	private void ResetMicPushToTalk()
	{
		IngamePlayerSettings.Instance.settings.pushToTalk = initialMicPushToTalkSetting;
	}
}



