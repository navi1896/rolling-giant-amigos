#nullable disable
using System.Collections;
using ControlCompany.Core.Extentions;
using Unity.Netcode;
using UnityEngine;

namespace ControlCompany.Core.Enemy;

internal class RadMechEnemyController : EnemyAIController
{
	private RadMechAI radMechAI;

	private float originalRunSpeed = 0f;

	private float originalWalkSpeed = 0f;

	private float scanTargetRange = 200f;

	private float fireRate;

	private float shootTimer = 0f;

	private float locateNewTargetTime = 2f;

	private float locateNewTargetTimer = 0f;

	private bool isFiring = false;

	private int visibleThreatsMask;

	public override void InitializeEnemyController(PlayerActions playerActions, GameObject enemyGameObject)
	{
		radMechAI = (RadMechAI)GetEnemyAIFromEnemyGameObject(enemyGameObject);
		base.InitializeEnemyController(playerActions, enemyGameObject);
		originalRunSpeed = base.RunSpeed;
		originalWalkSpeed = base.WalkSpeed;
		visibleThreatsMask = radMechAI.GetFieldValue<int>("visibleThreatsMask");
		fireRate = radMechAI.fireRate;
		AdjustAudio();
	}

	protected override void LateUpdate()
	{
		if (!isInitialized)
		{
			return;
		}
		base.LateUpdate();
		if (isAIControlled || !hasControl)
		{
			return;
		}
		radMechAI.SetFieldValue("shootTimer", 0f);
		Vector3 val = MoveInput();
		if (val.sqrMagnitude <= 0.1f)
		{
			radMechAI.SetFieldValue("walkStepTimer", 0.1f);
			radMechAI.SetFieldValue("takingStep", value: false);
		}
		else
		{
			if (radMechAI.aimingGun && !isFiring)
			{
				radMechAI.SetAimingGun(false);
			}
			bool fieldValue = radMechAI.GetFieldValue<bool>("takingStep");
			float fieldValue2 = radMechAI.GetFieldValue<float>("walkStepTimer");
			radMechAI.SetFieldValue("walkStepTimer", Mathf.Min(fieldValue2, 0.7f));
			if (!fieldValue)
			{
				base.RunSpeed = 0f;
				base.WalkSpeed = 0f;
				if (IsSprinting())
				{
					radMechAI.SetFieldValue("walkStepTimer", Mathf.Min(fieldValue2, 0.2f));
				}
			}
			else
			{
				base.RunSpeed = originalRunSpeed;
				base.WalkSpeed = originalWalkSpeed;
			}
		}
		if (isFiring)
		{
			shootTimer += Time.deltaTime;
			locateNewTargetTimer += Time.deltaTime;
			if (locateNewTargetTimer >= locateNewTargetTime)
			{
				locateNewTargetTimer = 0f;
				ScanAndSetTarget();
			}
			if (!radMechAI.aimingGun)
			{
				radMechAI.SetAimingGun(true);
			}
			if (shootTimer >= fireRate)
			{
				shootTimer = 0f;
				radMechAI.StartShootGun();
			}
		}
	}

	protected override void UsePrimarySkillAction()
	{
		if ((Object)(object)radMechAI.focusedThreatTransform == (Object)null)
		{
			ScanAndSetTarget();
		}
		if ((Object)(object)radMechAI.focusedThreatTransform != (Object)null)
		{
			((EnemyAI)radMechAI).SwitchToBehaviourState(1);
			isFiring = true;
		}
	}

	protected override void ReleasePrimarySkillAction()
	{
		isFiring = false;
		shootTimer = 0f;
		locateNewTargetTimer = 0f;
		radMechAI.SetAimingGun(false);
		((EnemyAI)radMechAI).SwitchToBehaviourState(0);
	}

	protected override void UseSecondarySkillAction()
	{
	}

	protected override void ReleaseSecondarySkillAction()
	{
	}

	public override void DestroyAndCleanUp()
	{
		((MonoBehaviour)this).StartCoroutine(DelayedDestroyAndCleanUp());
	}

	private IEnumerator DelayedDestroyAndCleanUp()
	{
		EnableControl(enable: false);
		EnableCamera(enable: false);
		yield return (object)new WaitForSeconds(3f);
		base.DestroyAndCleanUp();
	}

	public override string GetPrimarySkillName()
	{
		return "Fire";
	}

	public override string GetSecondarySkillName()
	{
		return "";
	}

	private void AdjustAudio()
	{
		AudioSource creatureSFX = ((EnemyAI)radMechAI).creatureSFX;
		creatureSFX.volume *= 0.25f;
		AudioSource localLRADAudio = radMechAI.LocalLRADAudio;
		localLRADAudio.volume *= 0.25f;
		AudioSource localLRADAudio2 = radMechAI.LocalLRADAudio2;
		localLRADAudio2.volume *= 0.25f;
		AudioSource explosionAudio = radMechAI.explosionAudio;
		explosionAudio.volume *= 0.25f;
		AudioSource blowtorchAudio = radMechAI.blowtorchAudio;
		blowtorchAudio.volume *= 0.25f;
		AudioSource flyingDistantAudio = radMechAI.flyingDistantAudio;
		flyingDistantAudio.volume *= 0.25f;
		AudioSource spotlightOnAudio = radMechAI.spotlightOnAudio;
		spotlightOnAudio.volume *= 0.25f;
		AudioSource chargeForwardAudio = radMechAI.chargeForwardAudio;
		chargeForwardAudio.volume *= 0.25f;
		AudioSource engineSFX = radMechAI.engineSFX;
		engineSFX.volume *= 0.25f;
		AudioSource chargeForwardAudio2 = radMechAI.chargeForwardAudio;
		chargeForwardAudio2.volume *= 0.25f;
		AudioSource chargeForwardAudio3 = radMechAI.chargeForwardAudio;
		chargeForwardAudio3.volume *= 0.25f;
	}

	private void FireGun()
	{
	}

	private void ReloadGun()
	{
	}

	private void ScanAndSetTarget()
	{
		float num = float.MaxValue;
		Transform eye = ((EnemyAI)radMechAI).eye;
		IVisibleThreat val = null;
		Transform val2 = null;
		Collider[] array = Physics.OverlapSphere(eye.position + eye.forward * 58f + -eye.up * 10f, scanTargetRange, visibleThreatsMask, (QueryTriggerInteraction)2);
		Collider[] array2 = array;
		foreach (Collider val3 in array2)
		{
			if ((Object)(object)val3 == (Object)(object)radMechAI.ownCollider)
			{
				continue;
			}
			IVisibleThreat componentInChildren = ((Component)val3).GetComponentInChildren<IVisibleThreat>();
			if (componentInChildren != null && ((Component)this).transform.IsObjectWithinAngle(((Component)val3).transform, ((Component)this).transform.forward, 180f))
			{
				Vector3 val4 = ((Component)val3).transform.position - eye.position;
				float magnitude = val4.magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					val = componentInChildren;
					val2 = ((Component)val3).transform;
				}
			}
		}
		if (val != null)
		{
			float num2 = Vector3.Distance(eye.position, val2.position);
			radMechAI.SetTargetedThreat(val, val2.position + Vector3.up * 0.5f, num2);
			radMechAI.focusedThreatTransform = val.GetThreatTransform();
			NetworkObject component = ((Component)radMechAI.focusedThreatTransform).gameObject.GetComponent<NetworkObject>();
			((EnemyAI)radMechAI).SwitchToBehaviourStateOnLocalClient(1);
			radMechAI.SetTargetToThreatClientRpc(((NetworkObjectReference)component), radMechAI.targetedThreat.lastSeenPosition);
		}
		else
		{
			radMechAI.EnableSpotlight();
		}
	}
}



