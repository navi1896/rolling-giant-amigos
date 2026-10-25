#nullable disable
using ControlCompany.Core.Extentions;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class JesterEnemyController : EnemyAIController
{
	private JesterAI jesterEnemyAI;

	private const int JESTER_DEFAULT_MODE = 0;

	private const int JESTER_CRANK_MODE_INDEX = 1;

	private const int JESTER_POP_OUT_INDEX = 2;

	private float initialWalkSpeed;

	private float initialRunSpeed;

	private bool initialControlRotation;

	private float poppedOutSpeedAddition = 0f;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		jesterEnemyAI = (JesterAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
		initialWalkSpeed = base.WalkSpeed;
		initialRunSpeed = base.RunSpeed;
		initialControlRotation = controlEnemyRotation;
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		if (IsCranking())
		{
			controlEnemyMovement = false;
			controlEnemyRotation = false;
		}
		else if (IsPoppedOut() || IsInBox())
		{
			controlEnemyMovement = initialControlRotation;
			controlEnemyRotation = initialControlRotation;
		}
		if (IsPoppedOut())
		{
			Vector3 velocity = characterController.velocity;
			if (velocity.sqrMagnitude > 0.1f)
			{
				poppedOutSpeedAddition = Mathf.Clamp(poppedOutSpeedAddition + Time.deltaTime * 1f, 0f, 15f);
			}
			else if (poppedOutSpeedAddition > 3f)
			{
				poppedOutSpeedAddition = Mathf.Clamp(poppedOutSpeedAddition - Time.deltaTime * 2.5f, 3f, 15f);
			}
			base.WalkSpeed = Mathf.Clamp(0.5f + poppedOutSpeedAddition, 0f, 15f);
			base.RunSpeed = Mathf.Clamp(0.5f + poppedOutSpeedAddition, 0f, 15f) * 1.15f;
		}
		else if (IsInBox())
		{
			poppedOutSpeedAddition = 0f;
			base.WalkSpeed = initialWalkSpeed;
			base.RunSpeed = initialRunSpeed;
		}
		if (jesterEnemyAI.GetFieldValue<bool>("inKillAnimation"))
		{
			poppedOutSpeedAddition = 0f;
			controlEnemyMovement = false;
			controlEnemyRotation = false;
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if (!IsInBox())
		{
			ResetState();
		}
	}

	protected override void UseSecondarySkillAction()
	{
		if (IsInBox())
		{
			Cranking();
		}
	}

	protected override void ReleaseSecondarySkillAction()
	{
		if (IsCranking())
		{
			PopOut();
		}
	}

	protected override void Move()
	{
		bool fieldValue = jesterEnemyAI.GetFieldValue<bool>("inKillAnimation");
		if (!(IsCranking() || fieldValue))
		{
			base.Move();
		}
	}

	public override string GetPrimarySkillName()
	{
		return "Close box";
	}

	public override string GetSecondarySkillName()
	{
		return "(HOLD) Play music";
	}

	private void Cranking()
	{
		SwitchAIState(1);
	}

	private void PopOut()
	{
		SwitchAIState(2);
	}

	private void ResetState()
	{
		SwitchAIState(0);
	}

	private bool IsInBox()
	{
		return ((EnemyAI)jesterEnemyAI).currentBehaviourStateIndex == 0;
	}

	private bool IsCranking()
	{
		return ((EnemyAI)jesterEnemyAI).currentBehaviourStateIndex == 1;
	}

	private bool IsPoppedOut()
	{
		return ((EnemyAI)jesterEnemyAI).currentBehaviourStateIndex == 2;
	}
}



