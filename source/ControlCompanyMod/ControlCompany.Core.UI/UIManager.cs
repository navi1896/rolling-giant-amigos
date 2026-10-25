#nullable disable
using System.Collections.Generic;
using BepInEx;
using ControlCompany.Core.Enemy;
using ControlCompany.Core.Extentions;
using GameNetcodeStuff;
using UnityEngine;

namespace ControlCompany.Core.UI;

internal class UIManager : MonoBehaviour
{
	private const float GUI_LABEL_HEIGHT = 36f;

	private const float GUI_LABEL_WIDTH = 750f;

	private const float GUI_PADDING = 32f;

	private const int GUI_FONT_SIZE = 28;

	private const FontStyle GUI_FONT_WEIGHT = (FontStyle)0;

	public static UIManager Instance;

	private void Awake()
	{
		if ((Object)(object)Instance == (Object)null)
		{
			Instance = this;
		}
	}

	public void RenderNotHostLabel(string toggleUIKey)
	{
		GUIStyle val = new GUIStyle(GUI.skin.label)
		{
			alignment = (TextAnchor)3,
			fontSize = 24,
			fontStyle = (FontStyle)0
		};
		val.normal.textColor = Color.red;
		RenderTextTopLeftCorner("Not hosting. ControlCompany mod is disabled. Press " + toggleUIKey + " to hide.", 0f, val, 1000f);
	}

	public void RenderEnemyControls(string primaryKey, string secondaryKey, string aiControlKey, string exitKey, string listenFromHostKey, EnemyController enemyController, PlayerControllerB hostPlayer)
	{
		string primarySkillName = enemyController.GetPrimarySkillName();
		string secondarySkillName = enemyController.GetSecondarySkillName();
		List<UIInstruction> list = new List<UIInstruction>();
		if (enemyController.IsAIControlled)
		{
			list.Add(new UIInstruction
			{
				InputKey = aiControlKey,
				ActionText = "Take control"
			});
		}
		else
		{
			if (!Utility.IsNullOrWhiteSpace(primarySkillName))
			{
				list.Add(new UIInstruction
				{
					InputKey = primaryKey,
					ActionText = primarySkillName
				});
			}
			if (!Utility.IsNullOrWhiteSpace(secondarySkillName))
			{
				list.Add(new UIInstruction
				{
					InputKey = secondaryKey,
					ActionText = secondarySkillName
				});
			}
			list.Add(new UIInstruction
			{
				InputKey = aiControlKey,
				ActionText = "AI control"
			});
		}
		list.Add(new UIInstruction
		{
			InputKey = exitKey,
			ActionText = "Exit"
		});
		RenderPlayerStatus(hostPlayer, listenFromHostKey);
		float height = 0f;
		height = RenderEnemyStatus(enemyController, height);
		RenderEnemySkills(list, height);
	}

	public void RenderHostControls(string ghostModeKey, string cycleLeftKey, string cycleRightKey, PlayerControllerB currentTarget, bool isExtendedModeEnabled, string creditsKey, string entranceTeleportKey, string toggleUIKey)
	{
		List<UIInstruction> list = new List<UIInstruction>();
		list.Add(new UIInstruction
		{
			InputKey = ghostModeKey,
			ActionText = "Ghost mode"
		});
		list.Add(new UIInstruction
		{
			InputKey = $"{cycleLeftKey}/{cycleRightKey}",
			ActionText = "Change target"
		});
		list.Add(new UIInstruction
		{
			InputKey = toggleUIKey,
			ActionText = "Hide UI"
		});
		if (isExtendedModeEnabled)
		{
			list.Add(new UIInstruction
			{
				InputKey = creditsKey,
				ActionText = "Give credits"
			});
			list.Add(new UIInstruction
			{
				InputKey = entranceTeleportKey,
				ActionText = "Teleport to entrance"
			});
		}
		RenderHostControlList(list, currentTarget);
	}

	public void RenderGhostControls(List<string> enemySpawnKeys, string exitKey, string cycleLeftKey, string cycleRightKey, string listenFromHostKey, bool showEnemyPaging, List<ControllableEnemy> enemies, List<Item> scraps, PlayerControllerB hostPlayer, bool scrapSpawnMode)
	{
		List<UIInstruction> list = new List<UIInstruction>();
		if (!scrapSpawnMode)
		{
			if (enemies != null && enemies.Count > 0)
			{
				if (showEnemyPaging)
				{
					list.Add(new UIInstruction
					{
						InputKey = $"{cycleLeftKey}/{cycleRightKey}",
						ActionText = "Change page"
					});
				}
				for (int i = 0; i < enemies.Count && i < enemySpawnKeys.Count; i++)
				{
					GameObject enemyPrefab = enemies[i].EnemyPrefab;
					list.Add(new UIInstruction
					{
						InputKey = enemySpawnKeys[i],
						ActionText = $"Control {enemies[i].EnemyName}"
					});
				}
			}
		}
		else
		{
			for (int j = 0; j < scraps.Count && j < enemySpawnKeys.Count; j++)
			{
				list.Add(new UIInstruction
				{
					InputKey = enemySpawnKeys[j],
					ActionText = $"Spawn {scraps[j].itemName}"
				});
			}
		}
		list.Add(new UIInstruction
		{
			InputKey = exitKey,
			ActionText = "Exit"
		});
		try { RenderPlayerStatus(hostPlayer, listenFromHostKey); } catch { }
		RenderGhostControlList(list);
	}

	private void RenderGhostControlList(List<UIInstruction> ghostControls)
	{
		if (ghostControls.Count <= 0)
		{
			return;
		}
		GUIStyle style = new GUIStyle(GUI.skin.label)
		{
			alignment = (TextAnchor)5,
			fontSize = 28,
			fontStyle = (FontStyle)0
		};
		float num = 0f;
		foreach (UIInstruction ghostControl in ghostControls)
		{
			RenderTextTopRightCorner($"{ghostControl.InputKey}: {ghostControl.ActionText}", num, style);
			num += 36f;
		}
	}

	private float RenderMicStatus(float height, PlayerControllerB hostPlayer)
	{
		GUIStyle val = new GUIStyle(GUI.skin.label)
		{
			alignment = (TextAnchor)3,
			fontSize = 28,
			fontStyle = (FontStyle)0
		};
		float num = height;
		if (IngamePlayerSettings.Instance.settings.micEnabled && (Object)(object)StartOfRound.Instance.voiceChatModule != (Object)null)
		{
			if (!StartOfRound.Instance.voiceChatModule.IsMuted)
			{
				val.normal.textColor = Color.green;
				RenderTextTopLeftCorner(string.Format("{0} mic enabled", hostPlayer.isPlayerDead ? "Dead" : "Player"), num, val);
			}
			else
			{
				val.normal.textColor = Color.red;
				RenderTextTopLeftCorner(string.Format("{0} mic disabled (push to talk)", hostPlayer.isPlayerDead ? "Dead" : "Player"), num, val);
			}
			num += 36f;
		}
		return num;
	}

	private float RenderPlayerStatus(PlayerControllerB hostPlayer, string listenFromHostKey)
	{
		GUIStyle val = new GUIStyle(GUI.skin.label)
		{
			alignment = (TextAnchor)3,
			fontSize = 28,
			fontStyle = (FontStyle)0
		};
		if (hostPlayer.isPlayerDead)
		{
			val.normal.textColor = Color.red;
		}
		else
		{
			val.normal.textColor = Color.green;
		}
		float num = 0f;
		RenderTextTopLeftCorner(string.Format("Player status: {0}", hostPlayer.isPlayerDead ? "Dead" : "Alive"), num, val);
		num += 36f;
		if (!hostPlayer.isPlayerDead)
		{
			GUIStyle val2 = new GUIStyle(GUI.skin.label)
			{
				alignment = (TextAnchor)3,
				fontSize = 28,
				fontStyle = (FontStyle)0
			};
			bool flag = !HostIsNearOtherPlayers(hostPlayer);
			if (flag)
			{
				val2.normal.textColor = Color.green;
			}
			else
			{
				val2.normal.textColor = Color.red;
			}
			RenderTextTopLeftCorner(string.Format("Player proximity: {0}", flag ? "Alone" : "Near others"), num, val2);
			num += 36f;
			GUIStyle val3 = new GUIStyle(GUI.skin.label)
			{
				alignment = (TextAnchor)3,
				fontSize = 28,
				fontStyle = (FontStyle)0
			};
			bool flag2 = ControlCenter.Instance.IsListeningFromHost();
			if (flag2)
			{
				val3.normal.textColor = Color.green;
			}
			else
			{
				val3.normal.textColor = Color.red;
			}
			RenderTextTopLeftCorner(string.Format("Listening from host: {0}", flag2 ? "Yes" : ("No (hold " + listenFromHostKey + ")")), num, val3);
			num += 36f;
		}
		return num + RenderMicStatus(num, hostPlayer);
	}

	private void RenderHostControlList(List<UIInstruction> hostControls, PlayerControllerB currentTarget)
	{
		if (hostControls.Count <= 0)
		{
			return;
		}
		GUIStyle style = new GUIStyle(GUI.skin.label)
		{
			alignment = (TextAnchor)5,
			fontSize = 28,
			fontStyle = (FontStyle)0
		};
		float num = 0f;
		RenderTextTopRightCorner("ControlCompany (Crew17 Edition)", num, style);
		num += 36f;
		if ((Object)(object)currentTarget != (Object)null)
		{
			RenderTextTopRightCorner($"Spawn target: {currentTarget.playerUsername}", num, style);
			num += 36f;
		}
		foreach (UIInstruction hostControl in hostControls)
		{
			RenderTextTopRightCorner($"{hostControl.InputKey}: {hostControl.ActionText}", num, style);
			num += 36f;
		}
	}

	private float RenderEnemyStatus(EnemyController enemyController, float height)
	{
		float num = height;
		if (enemyController.IsAIControlled)
		{
			GUIStyle val = new GUIStyle(GUI.skin.label)
			{
				alignment = (TextAnchor)5,
				fontSize = 28,
				fontStyle = (FontStyle)0
			};
			val.normal.textColor = Color.red;
			RenderTextTopRightCorner("AI controlled", num, val);
			num += 36f;
		}
		return num;
	}

	private float RenderEnemySkills(List<UIInstruction> playerEnemyControls, float height)
	{
		float num = height;
		if (playerEnemyControls.Count > 0)
		{
			GUIStyle style = new GUIStyle(GUI.skin.label)
			{
				alignment = (TextAnchor)5,
				fontSize = 28,
				fontStyle = (FontStyle)0
			};
			foreach (UIInstruction playerEnemyControl in playerEnemyControls)
			{
				RenderTextTopRightCorner($"{playerEnemyControl.InputKey}: {playerEnemyControl.ActionText}", num, style);
				num += 36f;
			}
		}
		return num;
	}

	private void RenderTextMiddle(string text, float yPos, GUIStyle style)
	{
		GUI.Label(new Rect((float)Screen.width / 2f - 375f, yPos + 32f, 750f, 36f), text, style);
	}

	private void RenderTextTopRightCorner(string text, float yPos, GUIStyle style)
	{
		GUI.Label(new Rect((float)Screen.width - 750f - 32f, yPos + 32f, 750f, 36f), text, style);
	}

	private void RenderTextTopLeftCorner(string text, float yPos, GUIStyle style, float widthOverride = -1f)
	{
		GUI.Label(new Rect(32f, yPos + 32f, (widthOverride > 0f) ? widthOverride : 750f, 36f), text, style);
	}

	private bool HostIsNearOtherPlayers(PlayerControllerB hostPlayer)
	{
		if ((Object)(object)hostPlayer == (Object)null || ControlCenter.Instance == null) return false;
		foreach (PlayerControllerB p in ControlCenter.Instance.GetAlivePlayers())
		{
			if ((Object)(object)p != (Object)null && (Object)(object)p != (Object)(object)hostPlayer && Vector3.Distance(((Component)hostPlayer).transform.position, ((Component)p).transform.position) < 17f)
			{
				return true;
			}
		}
		return false;
	}
}



