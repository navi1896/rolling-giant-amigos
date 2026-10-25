#nullable disable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ControlCompany.Core;

internal static class Constants
{
	public static Dictionary<int, string> ALL_LAYERS_DICT = (from index in Enumerable.Range(0, 32)
		where !string.IsNullOrEmpty(LayerMask.LayerToName(index))
		select index).ToDictionary((int index) => index, (int index) => LayerMask.LayerToName(index));

	public static string PLAYER_LOS_LAYERNAME = "LineOfSight";

	public static int PLAYER_LOS_LAYERINDEX = LayerMask.NameToLayer(PLAYER_LOS_LAYERNAME);

	public static LayerMask PLAYER_LOS_LAYERMASK = LayerMask.GetMask(new string[1] { PLAYER_LOS_LAYERNAME });

	public static string PLAYER_LAYERNAME = "Player";

	public static int PLAYER_LAYERINDEX = LayerMask.NameToLayer(PLAYER_LAYERNAME);

	public static LayerMask PLAYER_LAYERMASK = LayerMask.GetMask(new string[1] { PLAYER_LAYERNAME });

	public static string PLAYER_VISOR_LAYERNAME = "HelmetVisor";

	public static int PLAYER_VISOR_LAYERINDEX = LayerMask.NameToLayer(PLAYER_VISOR_LAYERNAME);

	public static LayerMask PLAYER_VISOR_LAYERMASK = LayerMask.GetMask(new string[1] { PLAYER_VISOR_LAYERNAME });

	public static string ITERACTABLE_OBJ_LAYERNAME = "InteractableObject";

	public static int ITERACTABLE_OBJ_LAYERINDEX = LayerMask.NameToLayer(ITERACTABLE_OBJ_LAYERNAME);

	public static LayerMask ITERACTABLE_OBJ_LAYERMASK = LayerMask.GetMask(new string[1] { ITERACTABLE_OBJ_LAYERNAME });

	public static string PROPS_OBJ_LAYERNAME = "Props";

	public static int PROPS_OBJ_LAYERINDEX = LayerMask.NameToLayer(PROPS_OBJ_LAYERNAME);

	public static LayerMask PROPS_LAYERMASK = LayerMask.GetMask(new string[1] { PROPS_OBJ_LAYERNAME });

	public static string ENEMIES_OBJ_LAYERNAME = "Enemies";

	public static int ENEMIES_OBJ_LAYERINDEX = LayerMask.NameToLayer(ENEMIES_OBJ_LAYERNAME);

	public static LayerMask ENEMIES_LAYERMASK = LayerMask.GetMask(new string[1] { ENEMIES_OBJ_LAYERNAME });

	public static string PLAYER_RAGDOLL_LAYERNAME = "PlayerRagdoll";

	public static int PLAYER_RAGDOLL_LAYERINDEX = LayerMask.NameToLayer(PLAYER_RAGDOLL_LAYERNAME);

	public static LayerMask PLAYER_RAGDOLL_LAYERMASK = LayerMask.GetMask(new string[1] { PLAYER_RAGDOLL_LAYERNAME });

	public static string PHYSICS_OBJECT_LAYERNAME = "PhysicsObject";

	public static int PHYSICS_OBJECT_LAYERINDEX = LayerMask.NameToLayer(PHYSICS_OBJECT_LAYERNAME);

	public static LayerMask PHYSICS_OBJECT_LAYERMASK = LayerMask.GetMask(new string[1] { PHYSICS_OBJECT_LAYERNAME });
}
