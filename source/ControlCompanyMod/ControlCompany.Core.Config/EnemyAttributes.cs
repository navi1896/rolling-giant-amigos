#nullable disable
using UnityEngine;

namespace ControlCompany.Core.Config;

public class EnemyAttributes
{
	public float WalkSpeed { get; set; } = 5f;

	public float RunSpeed { get; set; } = 7f;

	public bool PlayerControlsRotation { get; set; } = true;

	public float CamHeight { get; set; } = 2f;

	public float CamDistance { get; set; } = -3.5f;

	public string EnemyName { get; set; } = "";

	public Vector3 EnemyPositionOffset { get; set; } = Vector3.zero;

	public bool UseLerpRotation { get; set; } = true;
}



