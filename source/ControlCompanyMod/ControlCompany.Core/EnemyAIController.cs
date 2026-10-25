#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using ControlCompany.Core.Config;
using ControlCompany.Core.Extentions;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.AI;

namespace ControlCompany.Core;

internal class EnemyAIController : EnemyController
{
	private EnemyAI enemyAI;

	private NavMeshAgent enemyAIAgent;

	private float originalAgentAngularSpeed;

	private float originalAgentSpeed;

	private Vector3 enemyPositionOffset = Vector3.zero;

	private bool useLerpRotation = false;

	public EnemyAI EnemyAI => enemyAI;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		if (!isInitialized)
		{
			List<Collider> list = enemyGameObject.GetComponentsInChildren<Collider>().ToList();
			Initialize(playerActions, CameraMode.THIRD_PERSON, list);
			CharacterController obj = characterController;
			((Collider)obj).excludeLayers = (int)((Collider)obj).excludeLayers & ~(int)Constants.ITERACTABLE_OBJ_LAYERMASK;
			enemyAI = GetEnemyAIFromEnemyGameObject(enemyGameObject);
			if ((Object)(object)enemyAI == (Object)null)
			{
				throw new Exception("Enemy gameobject does not have EnemyAI script attached.");
			}
			((Component)enemyAI).gameObject.LogLayer();
			enemyGameObject.transform.position = ((Component)this).transform.position;
			enemyGameObject.transform.rotation = ((Component)this).transform.rotation;
			base.enemyGameObject = enemyGameObject;
			enemyAIAgent = enemyGameObject.GetComponentInChildren<NavMeshAgent>();
			if ((Object)(object)enemyAIAgent != (Object)null)
			{
				originalAgentAngularSpeed = enemyAIAgent.angularSpeed;
				originalAgentSpeed = enemyAIAgent.speed;
			}
			ConfigureEnemyAttributes(enemyAI);
			EnableAIControl(enabled: false);
		}
	}

	protected override void LateUpdate()
	{
		if (!isInitialized)
		{
			return;
		}
		base.LateUpdate();
		if (!isAIControlled)
		{
			if (!CanControlEnemy())
			{
				return;
			}
			if (controlEnemyMovement)
			{
				enemyGameObject.transform.position = ((Component)this).transform.position + enemyPositionOffset;
			}
			if (!controlEnemyRotation || lockEnemyRotationInput)
			{
				return;
			}
			Vector3 velocity = characterController.velocity;
			if (velocity.sqrMagnitude > 0.1f)
			{
				if (useLerpRotation)
				{
					enemyGameObject.transform.rotation = Quaternion.Lerp(enemyGameObject.transform.rotation, ((Component)this).transform.rotation, 8f * Time.deltaTime);
				}
				else
				{
					enemyGameObject.transform.rotation = ((Component)this).transform.rotation;
				}
			}
		}
		else
		{
			((Component)this).transform.position = enemyGameObject.transform.position;
		}
	}

	protected EnemyAI GetEnemyAIFromEnemyGameObject(GameObject enemyObj)
	{
		return enemyObj.GetComponentInChildren<EnemyAI>();
	}

	private void ConfigureEnemyAttributes(EnemyAI enemyAI)
	{
		EnemyAttributes enemyAttributes = enemyAI.GetEnemyAttributes();
		base.WalkSpeed = enemyAttributes.WalkSpeed;
		base.RunSpeed = enemyAttributes.RunSpeed;
		SetCamDistance(enemyAttributes.CamDistance);
		SetCamHeight(enemyAttributes.CamHeight);
		controlEnemyRotation = enemyAttributes.PlayerControlsRotation;
		controlEnemyMovement = true;
		enemyPositionOffset = enemyAttributes.EnemyPositionOffset;
		useLerpRotation = enemyAttributes.UseLerpRotation;
	}

	protected virtual void ConfigureEnemyHandicap(EnemyAI enemyAI)
	{
	}

	public override bool CanControlEnemy()
	{
		return !enemyAI.isEnemyDead;
	}

	protected override void Move()
	{
		if (!enemyAI.isEnemyDead)
		{
			base.Move();
		}
	}

	public void SwitchAIState(int stateIndex)
	{
		CheckInitialized();
		if (!isAIControlled)
		{
			enemyAI.SwitchToBehaviourState(stateIndex);
		}
	}

	protected override void ToggleAIControlTrue()
	{
		if ((Object)(object)enemyAIAgent != (Object)null)
		{
			enemyAIAgent.angularSpeed = originalAgentAngularSpeed;
			enemyAIAgent.speed = originalAgentSpeed;
			enemyAIAgent.isStopped = false;
			enemyAIAgent.Warp(enemyGameObject.transform.position);
			enemyAIAgent.updatePosition = true;
		}
	}

	protected override void ToggleAIControlFalse()
	{
		if ((Object)(object)enemyAI != (Object)null)
		{
			try
			{
				enemyAI.ChangeOwnershipOfEnemy(ControlCenter.Instance.GetHostPlayer().actualClientId);
			}
			catch (Exception ex)
			{
				ControlCompanyPlugin.Instance.logger.LogDebug((object)("Cannot give host control: " + ex.Message));
			}
		}
		if ((Object)(object)enemyAIAgent != (Object)null)
		{
			enemyAIAgent.angularSpeed = 0f;
			enemyAIAgent.speed = 0f;
			enemyAIAgent.isStopped = true;
			enemyAIAgent.updatePosition = false;
		}
		((Component)this).transform.SetParent((Transform)null);
	}

	protected override bool ShouldDestroyEnemyGameObjectOnCleanUp()
	{
		return !enemyAI.isEnemyDead;
	}

	public override string GetSecondarySkillName()
	{
		return "Secondary Skill";
	}

	protected override void UseSecondarySkillAction()
	{
		enemyAI.SwitchToBehaviourState(1);
	}

	protected override void EnableAIControl(bool enabled)
	{
		base.EnableAIControl(enabled);
		canMove = !isAIControlled;
	}
}



