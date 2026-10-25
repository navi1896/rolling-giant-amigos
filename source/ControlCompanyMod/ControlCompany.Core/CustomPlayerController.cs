#nullable disable
using System.Collections.Generic;
using System.Linq;
using ControlCompany.Core.Input;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering.HighDefinition;

namespace ControlCompany.Core;

internal class CustomPlayerController : MonoBehaviour
{
	public enum CameraMode
	{
		THIRD_PERSON,
		FIRST_PERSON
	}

	protected bool isInitialized = false;

	protected bool hasControl = false;

	protected bool canMove = false;

	protected bool canRotate = false;

	protected bool canJump = false;

	protected CameraMode camMode;

	protected CharacterController characterController;

	protected GameObject cameraGameObjectParent;

	protected GameObject cameraGameObject;

	protected Camera camera;

	protected Light light;

	protected PlayerActions playerActions;

	protected DoorOpener doorOpener;

	private AudioListener audioListener;

	private float runSpeed = 9f;

	private float walkSpeed = 7f;

	private float camDistance = -3.5f;

	private float camHeight = 2f;

	private float gravity = -20f;

	private float jumpHeight = 4.5f;

	private float currentYVelocity;

	private List<Collider> collidersToIgnore = new List<Collider>();

	public float RunSpeed
	{
		get
		{
			return runSpeed;
		}
		set
		{
			runSpeed = value;
		}
	}

	public float WalkSpeed
	{
		get
		{
			return walkSpeed;
		}
		set
		{
			walkSpeed = value;
		}
	}

	public float Gravity
	{
		get
		{
			return gravity;
		}
		set
		{
			gravity = value;
		}
	}

	public AudioListener AudioListener
	{
		get
		{
			return audioListener;
		}
		set
		{
			audioListener = value;
		}
	}

	public bool IsInitialized
	{
		get
		{
			return isInitialized;
		}
		set
		{
			isInitialized = value;
		}
	}

	public void Initialize(PlayerActions playerActions, CameraMode camMode, List<Collider> collidersToIgnore = null)
	{
		if (!isInitialized)
		{
			this.playerActions = playerActions;
			this.camMode = camMode;
			if (collidersToIgnore != null)
			{
				this.collidersToIgnore = collidersToIgnore;
			}
			characterController = InitializeCharacterController();
			camera = InitializeCamera();
			light = InitializeLight(((Component)camera).gameObject);
			doorOpener = ((Component)this).gameObject.AddComponent<DoorOpener>();
			EnableControl(enable: true);
			isInitialized = true;
		}
	}

	public void EnableLight(bool enable)
	{
		((Behaviour)light).enabled = enable;
	}

	public void EnableCamera(bool enable)
	{
		((Behaviour)camera).enabled = enable;
	}

	public void ToggleLight()
	{
		((Behaviour)light).enabled = !((Behaviour)light).enabled;
	}

	public void EnableControl(bool enable)
	{
		hasControl = enable;
		canMove = enable;
		canRotate = enable;
		canJump = enable;
	}

	public bool IsMoving()
	{
		Vector3 velocity = characterController.velocity;
		return velocity.sqrMagnitude > 0f;
	}

	public void SetCamHeight(float camHeight)
	{
		this.camHeight = camHeight;
		cameraGameObjectParent.transform.localPosition = new Vector3(0f, camHeight, 0f);
	}

	public void SetCamDistance(float camDistance)
	{
		this.camDistance = camDistance;
		cameraGameObject.transform.localPosition = new Vector3(0f, 0f, (camMode == CameraMode.THIRD_PERSON) ? camDistance : 0f);
	}

	protected virtual void LateUpdate()
	{
		if (isInitialized && hasControl)
		{
			if (canMove)
			{
				Move();
			}
			if (canRotate)
			{
				Rotate();
			}
			if (((ButtonControl)InputKeys.CONTROLLER_USE_KEY).wasPressedThisFrame)
			{
				doorOpener.DetectAndOpenDoors(3f);
			}
		}
	}

	protected virtual Vector3 MoveInput()
	{
		return (IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move", false).ReadValue<Vector2>());
	}

	protected virtual bool IsSprinting()
	{
		return IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint", false).ReadValue<float>() > 0f;
	}

	protected virtual void Move()
	{
		bool flag = IsSprinting();
		Vector3 val = MoveInput();
		Vector3 val2 = ((Component)this).transform.right * val.x;
		Vector3 val3 = ((Component)this).transform.forward * val.y;
		bool isGrounded = characterController.isGrounded;
		if (isGrounded && currentYVelocity < 0f)
		{
			currentYVelocity = 0f;
		}
		if (canJump && isGrounded && ((ButtonControl)InputKeys.CONTROLLER_JUMP_KEY).wasPressedThisFrame)
		{
			currentYVelocity += Mathf.Sqrt(jumpHeight * (0f - gravity));
		}
		currentYVelocity += gravity * Time.deltaTime;
		characterController.Move(((val2 + val3) * (flag ? runSpeed : walkSpeed) + new Vector3(0f, currentYVelocity, 0f)) * Time.deltaTime);
	}

	protected virtual void Rotate()
	{
		PlayerActions.MovementActions movement = playerActions.Movement;
		Vector2 val = movement.Look.ReadValue<Vector2>() * 0.008f * (float)IngamePlayerSettings.Instance.settings.lookSensitivity;
		if (IngamePlayerSettings.Instance.settings.invertYAxis)
		{
			val.y *= -1f;
		}
		((Component)this).transform.Rotate(new Vector3(0f, val.x, 0f));
		cameraGameObjectParent.transform.Rotate(new Vector3(0f - val.y, 0f, 0f));
	}

	private Light InitializeLight(GameObject lightParent)
	{
		GameObject val = new GameObject("controller_light");
		val.transform.SetParent(lightParent.transform);
		val.transform.localPosition = new Vector3(0f, 0.5f, 0f);
		val.transform.localRotation = Quaternion.identity;
		Light val2 = val.AddComponent<Light>();
		val2.range = 15f;
		val2.intensity = 225f;
		val2.cullingMask = -1;
		return val2;
	}

	private CharacterController InitializeCharacterController()
	{
		CharacterController controller = ((Component)this).gameObject.AddComponent<CharacterController>();
		((Collider)controller).excludeLayers = (int)Constants.PLAYER_LOS_LAYERMASK | (int)Constants.ITERACTABLE_OBJ_LAYERMASK | (int)Constants.ENEMIES_LAYERMASK | (int)Constants.PROPS_LAYERMASK | (int)Constants.PLAYER_RAGDOLL_LAYERMASK;
		controller.center = new Vector3(0f, 0.55f, 0f);
		controller.height = 0.4f;
		ControlCenter.Instance.GetAllPlayers().ForEach(delegate(PlayerControllerB p)
		{
			if ((Object)(object)p != (Object)null)
			{
				((Component)p).GetComponentsInChildren<Collider>().ToList().ForEach(delegate(Collider c)
				{
					Physics.IgnoreCollision(c, (Collider)(object)controller);
				});
			}
		});
		if (collidersToIgnore.Count() > 0)
		{
			foreach (Collider item in collidersToIgnore)
			{
				Physics.IgnoreCollision(item, (Collider)(object)controller);
			}
		}
		controller.slopeLimit = 60f;
		controller.stepOffset = 0.4f;
		return controller;
	}

	private Camera InitializeCamera()
	{
		GameObject val = new GameObject("controller_cam_parent");
		val.transform.SetParent(((Component)this).transform);
		val.transform.localRotation = Quaternion.identity;
		val.transform.localPosition = new Vector3(0f, camHeight, 0f);
		GameObject val2 = new GameObject("controller_cam");
		val2.transform.SetParent(val.transform);
		val2.transform.localRotation = Quaternion.identity;
		val2.transform.localPosition = new Vector3(0f, 0f, (camMode == CameraMode.THIRD_PERSON) ? camDistance : 0f);
		Camera val3 = val2.AddComponent<Camera>();
		audioListener = val2.AddComponent<AudioListener>();
		cameraGameObject = val2;
		cameraGameObjectParent = val;
		Camera gameplayCamera = ControlCenter.Instance.GetHostPlayer().gameplayCamera;
		val3.cullingMask = gameplayCamera.cullingMask;
		val3.farClipPlane = gameplayCamera.farClipPlane;
		HDAdditionalCameraData component = ((Component)gameplayCamera).gameObject.GetComponent<HDAdditionalCameraData>();
		CopyHDCameraData(val3, component);
		return val3;
	}

	private void CopyHDCameraData(Camera cam, HDAdditionalCameraData dataToCopy)
	{
		if ((Object)(object)dataToCopy != (Object)null)
		{
			HDAdditionalCameraData val = ((Component)cam).gameObject.GetComponent<HDAdditionalCameraData>();
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)cam).gameObject.AddComponent<HDAdditionalCameraData>();
			}
			if ((Object)(object)val != (Object)null)
			{
				val.customRenderingSettings = true;
				val.renderingPathCustomFrameSettingsOverrideMask.mask = dataToCopy.renderingPathCustomFrameSettingsOverrideMask.mask;
				SetCustomPass(val, dataToCopy);
				SetVolumetricFog(val, dataToCopy);
				SetLOD(val, dataToCopy);
				SetAliasing(val, dataToCopy);
				SetShadowQuality(val, dataToCopy);
			}
		}
	}

	private void SetCustomPass(HDAdditionalCameraData camData, HDAdditionalCameraData dataToCopy)
	{
		camData.renderingPathCustomFrameSettings.SetEnabled((FrameSettingsField)6, dataToCopy.renderingPathCustomFrameSettings.IsEnabled((FrameSettingsField)6));
	}

	private void SetVolumetricFog(HDAdditionalCameraData camData, HDAdditionalCameraData dataToCopy)
	{
		camData.renderingPathCustomFrameSettings.SetEnabled((FrameSettingsField)28, dataToCopy.renderingPathCustomFrameSettings.IsEnabled((FrameSettingsField)28));
	}

	private void SetLOD(HDAdditionalCameraData camData, HDAdditionalCameraData dataToCopy)
	{
		camData.renderingPathCustomFrameSettings.lodBiasMode = dataToCopy.renderingPathCustomFrameSettings.lodBiasMode;
		camData.renderingPathCustomFrameSettings.lodBias = dataToCopy.renderingPathCustomFrameSettings.lodBias;
	}

	private void SetAliasing(HDAdditionalCameraData camData, HDAdditionalCameraData dataToCopy)
	{
		camData.antialiasing = dataToCopy.antialiasing;
	}

	private void SetShadowQuality(HDAdditionalCameraData camData, HDAdditionalCameraData dataToCopy)
	{
		camData.renderingPathCustomFrameSettings.SetEnabled((FrameSettingsField)20, dataToCopy.renderingPathCustomFrameSettings.IsEnabled((FrameSettingsField)20));
	}
}



