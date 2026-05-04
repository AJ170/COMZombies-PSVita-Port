using System.Collections.Generic;
using UnityEngine;
using Zombie3D;

[AddComponentMenu("TPS/TPSSimpleCamera")]
public class TPSSimpleCameraScript : BaseCameraScript
{
	public Texture reticle;

	public Texture leftTopReticle;

	public Texture rightTopReticle;

	public Texture leftBottomReticle;

	public Texture rightBottomReticle;
	
	private float stickHeldTime = 0f;
	private const float sensitivityRampDelay = 0.6f;
	private const float sensitivityRampMultiplier = 2.5f;

	protected Shader transparentShader;

	protected Shader solidShader;

	protected Shader solidShader_eff;

	protected float drx;

	protected float dry;

	protected AlphaEffScript effCom;

	protected float winTime = -1f;

	private bool lockCamera;
	
	private  bool pauseMenu = false;

	// Track previous fire state so we only call OnFireBegin/StopFire on edges
	private bool wasFirePressed = false;

	private void Awake()
	{
		cameraTransform = Camera.main.transform;
	}

	public override CameraType GetCameraType()
	{
		return CameraType.TPSCamera;
	}

	private void Start()
	{
		solidShader = Shader.Find("iPhone/LightMap");
		transparentShader = Shader.Find("iPhone/AlphaBlend_Color");
		solidShader_eff = Shader.Find("iPhone/LightMap_Effect");
		Object.Destroy(GameObject.Find("Music"));
		GameObject gameObject = ((Random.Range(1, 100) <= 50) ? (Object.Instantiate(Resources.Load("Prefabs/BettleMusic2")) as GameObject) : (Object.Instantiate(Resources.Load("Prefabs/BettleMusic1")) as GameObject));
		base.GetComponent<Camera>().GetComponent<AudioSource>().clip = gameObject.GetComponent<BettleMusicScript>().BettleAudio;
		base.GetComponent<Camera>().GetComponent<AudioSource>().mute = !GameApp.GetInstance().GetGameState().MusicOn;
		base.GetComponent<Camera>().GetComponent<AudioSource>().Play();
	}

	public override void Init()
	{
		base.Init();
		cameraSwingSpeed *= 0.6f;
	}

	public override void CreateScreenBlood(float damage)
	{
		if (bs != null)
		{
			bs.NewBlood(damage);
		}
		else
		{
			Debug.Log("bs null");
		}
	}

	private void Update()
	{
		if (!base.GetComponent<Camera>().GetComponent<AudioSource>().isPlaying)
		{
			base.GetComponent<Camera>().GetComponent<AudioSource>().Play();
		}

#if UNITY_PSP2 && !UNITY_EDITOR
		HandleVitaInput();
#endif
	}

	private void HandleVitaInput()
	{
	
		/*Debug.Log("IsMoving: " + player.InputController.InputInfo.IsMoving + 
		          " | State: " + player.GetPlayerState().GetStateType() + 
		          " | MoveSpeed: " + player.MoveSpeed);*/
		if (player == null || player.GetTransform() == null)
		{
			Debug.Log("[Vita] player = null");
			return;
		}

		float h = Input.GetAxis("Left Joystick Horizontal");
		float v = Input.GetAxis("Left Joystick Vertical");

		bool isMoving = (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f);

		if (isMoving)
		{
			Vector3 moveDir = new Vector3(h, 0f, v);
			moveDir = player.GetTransform().TransformDirection(moveDir);
			moveDir += Physics.gravity * Time.deltaTime * 20f;

			CharacterController cc = player.GetTransform().GetComponent<CharacterController>();
			if (cc != null)
			{
				cc.Move(moveDir * Time.deltaTime * 2f);
			}

			if (player.InputController != null)
			{
				player.InputController.InputInfo.moveDirection = moveDir;
				player.InputController.InputInfo.IsMoving = true;
			}
			player.SetMoveDirection();
		}
		else
		{
			if (player.InputController != null)
			{
				player.InputController.InputInfo.IsMoving = false;
			}
		}

		// === SHOOTING - edge triggered so the state machine handles animations ===
		bool firePressed = Input.GetButton("Fire1");

		if (firePressed && !wasFirePressed)
		{
			// Button just pressed this frame
			player.OnFireBegin();
			if (player.InputController != null)
			{
				player.InputController.InputInfo.fire = true;
				//Debug.Log("Fire = true");
			}
		}
		else if (!firePressed && wasFirePressed)
		{
			// Button just released this frame
			player.StopFire();
			if (player.InputController != null)
			{
				player.InputController.InputInfo.fire = false;
				//Debug.Log("Fire = false");
			}
		}

		wasFirePressed = firePressed; 
		
		if (Input.GetButtonDown("Start Button"))
{
    if (!pauseMenu)
    {
        pauseMenu = true;
        Time.timeScale = 0f;
        GameUIScriptNew.GetGameUIScript().ShowPausePanel();
        OpenClikPlugin.Show(true);
    }
    else
    {
        pauseMenu = false;
        Time.timeScale = 1f;
        GameUIScriptNew.GetGameUIScript().HidePausePanel();
        OpenClikPlugin.Hide();
    }
}
		
		GameUIScriptNew gui = GameUIScriptNew.GetGameUIScript();
		if (gui != null && gui.uiInited)
		{
			if (Input.GetButtonDown("DPad Left"))
			{
				UseCarryItem(gui, 1);
			}
			else if (Input.GetButtonDown("DPad Right"))
			{
				UseCarryItem(gui, 0);
			}
		}

		
		if (Input.GetButtonDown("Triangle Button"))
		{
			if (player.PlayerBonusState == null || player.PlayerBonusState.StateType != PlayerBonusStateType.Suicidegun)
			{
				player.NextWeapon();
			}
		}
		//Debug.Log("HandleVitaInput running, isMoving: " + (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f));
		
	}

	private void UseCarryItem(GameUIScriptNew gui, int slot)
	{
		if (gui.itemInfo == null) return;
		if (gui.itemInfo.itemLogo == null || gui.itemInfo.itemLogo.Length <= slot) return;

		string frameName = gui.itemInfo.itemLogo[slot].frameName_Accessor;
		if (string.IsNullOrEmpty(frameName) || !frameName.StartsWith("item_")) return;

		ItemType itemType = Item.GetItemTypeByName(frameName.Substring("item_".Length));
		GameState gameState = GameApp.GetInstance().GetGameState();

		if (gui.itemInfo.isBuyItem[slot])
		{
			gameState.BuyItem(gameState.GetItemByType(itemType));
			player.carryItemsPacket[itemType] = gameState.GetItemByType(itemType).OwnedCount;
			gui.itemInfo.UpdateCarryItemPacket(itemType, player.carryItemsPacket[itemType]);
		}

		player.OnUseCarryItem(itemType);
	}
	
	private void LateUpdate()
	{
		if (!started)
		{
			return;
		}
		deltaTime = Time.deltaTime;
		if (player == null || player.GetTransform() == null || lockCamera || gameScene.GamePlayingState == PlayingState.GameQuit || gameScene.GamePlayingState == PlayingState.GameVsKiller)
		{
			return;
		}
		if (gameScene.GamePlayingState == PlayingState.GameLose)
		{
			cameraTransform.position = player.GetTransform().TransformPoint(3f * Mathf.Sin(Time.time * 0.3f), 4f, 3f * Mathf.Cos(Time.time * 0.3f));
			cameraTransform.LookAt(player.GetTransform());
		}
		else if (gameScene.GamePlayingState == PlayingState.GameWin)
		{
			if (winTime == -1f)
			{
				winTime = Time.time;
			}
			float num = Time.time - winTime;
			cameraTransform.position = player.GetTransform().TransformPoint(3f * Mathf.Sin((num - 1.7f) * 0.3f), 2f, 3f * Mathf.Cos((num - 1.7f) * 0.3f));
			cameraTransform.LookAt(player.GetTransform().position + Vector3.up * 1f);
		}
		else
		{
//right joystick camera sensitivity stuff
			float rawX = Input.GetAxis("Mouse X");
			float rawY = Input.GetAxis("Mouse Y");

// Track how long the stick has been held
			if (Mathf.Abs(rawX) > 0.1f || Mathf.Abs(rawY) > 0.1f)
			{
				stickHeldTime += Time.deltaTime;
			}
			else
			{
				stickHeldTime = 0f; // reset when stick is released
			}

// After 1 second, ramp up sensitivity
			float rampMultiplier = (stickHeldTime >= sensitivityRampDelay) ? sensitivityRampMultiplier : 1f;

			float x = rawX * 50f * Time.deltaTime * rampMultiplier;
			float y = rawY * 50f * Time.deltaTime * rampMultiplier;

			if (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.Android && player.InputController.EnableTurningAround)
			{
				if (Screen.lockCursor)
				{
					x = rawX * 50f * Time.deltaTime * rampMultiplier;
					y = rawY * 50f * Time.deltaTime * rampMultiplier;
				}
			}
			if (Time.timeScale != 0f)
			{
				angelH += x * 0.03f * cameraSwingSpeed;
				angelV += y * 0.03f * cameraSwingSpeed;
			}
			angelV = Mathf.Clamp(angelV, minAngelV, maxAngelV);
			if (player.GetWeapon().Deflection.x == 0f && player.GetWeapon().Deflection.y == 0f)
			{
				drx = Mathf.Lerp(drx, player.GetWeapon().Deflection.x, deltaTime * 5f);
				dry = Mathf.Lerp(dry, player.GetWeapon().Deflection.y, deltaTime * 5f);
			}
			else
			{
				drx = player.GetWeapon().Deflection.x;
				dry = player.GetWeapon().Deflection.y;
			}
			Quaternion q = Quaternion.Euler(0f - (angelV + drx), angelH + dry, 0f);
			NormalizeQuaternion(ref q);
			cameraTransform.rotation = q;
			Quaternion q2 = Quaternion.Euler(0f, angelH, 0f);
			NormalizeQuaternion(ref q2);
			player.GetTransform().rotation = q2;
			moveTo = player.GetTransform().TransformPoint(cameraDistanceFromPlayer);
			Vector3 direction = moveTo - player.GetTransform().position;
			Ray ray = new Ray(player.GetTransform().position, direction);
			float magnitude = direction.magnitude;
			RaycastHit hitInfo;
			if (Physics.Raycast(ray, out hitInfo, magnitude, 67584))
			{
				GameObject gameObject = hitInfo.collider.gameObject;
				if (gameObject.GetComponent<Renderer>() == null)
				{
					gameObject = gameObject.transform.parent.gameObject;
				}
				if (gameObject.GetComponent<Renderer>() != null)
				{
					gameObject.layer = 16;
					Material[] materials = gameObject.GetComponent<Renderer>().materials;
					foreach (Material material in materials)
					{
						Texture texture = material.GetTexture("_texBase");
						material.shader = transparentShader;
						Color gray = Color.gray;
						gray.a = 0.1f;
						material.SetColor("_TintColor", gray);
						material.SetTexture("_MainTex", texture);
					}
					for (int j = 0; j < 5 && !(lastTransparentObjList[j] == gameObject); j++)
					{
						if (lastTransparentObjList[j] == null)
						{
							lastTransparentObjList[j] = gameObject;
							break;
						}
					}
				}
			}
			else
			{
				for (int k = 0; k < 5; k++)
				{
					if (!(lastTransparentObjList[k] != null))
					{
						continue;
					}
					int num2 = 0;
					Material[] materials2 = lastTransparentObjList[k].GetComponent<Renderer>().materials;
					foreach (Material material2 in materials2)
					{
						SceneObjOldShaders component = lastTransparentObjList[k].GetComponent<SceneObjOldShaders>();
						if (component != null)
						{
							material2.shader = component.OldShaders[num2];
							if (material2.shader == solidShader_eff)
							{
								effCom = lastTransparentObjList[k].GetComponent<AlphaEffScript>();
								if (effCom == null)
								{
									effCom = lastTransparentObjList[k].AddComponent<AlphaEffScript>();
									effCom.colorPropertyName = "_Color";
									effCom.enableAlphaAnimation = true;
									effCom.minAlpha = 0f;
									effCom.animationSpeed = Random.Range(0.1f, 0.4f);
								}
							}
							num2++;
						}
						else
						{
							material2.shader = solidShader;
						}
					}
					lastTransparentObjList[k] = null;
				}
			}
			cameraTransform.position = Vector3.Lerp(cameraTransform.position, moveTo, 100f * Time.deltaTime);
		}
		if (player.InputController != null)
		{
			player.InputController.CameraRotation = Vector2.zero;
		}
	}

	public void LockCameraVSKiller(Transform targetPlayer)
	{
		if (cameraTransform != null && targetPlayer != null)
		{
			cameraTransform.position = targetPlayer.TransformPoint(0f, 2f, 3f);
			cameraTransform.LookAt(targetPlayer.position + Vector3.up * 1f);
			player.InputController.CameraRotation = Vector2.zero;
			gameScene.GamePlayingState = PlayingState.GameVsKiller;
		}
	}

	public void LockCameraCoopKiller(bool isMyself, Transform targetPlayer)
	{
		if (isMyself)
		{
			gameScene.GamePlayingState = PlayingState.GameWin;
		}
		else if (targetPlayer != null)
		{
			cameraTransform.position = targetPlayer.TransformPoint(0f, 2f, 3f);
			cameraTransform.LookAt(targetPlayer.position + Vector3.up * 1f);
		}
	}

	private void OnGUI()
	{
		if (Time.time == 0f || Time.timeScale == 0f || player == null || GameApp.GetInstance().GetGameScene().GamePlayingState != 0 || !player.InputController.EnableShootingInput)
		{
			return;
		}
		Weapon weapon = player.GetWeapon();
		if (weapon == null)
		{
			return;
		}
		if (weapon.GetWeaponType() == WeaponType.Sniper)
		{
			GUI.DrawTexture(new Rect(Sniper.lockAreaRect.xMin - AutoRect.AutoValue(leftTopReticle.width / 2), Sniper.lockAreaRect.yMin - AutoRect.AutoValue(leftTopReticle.height / 2), AutoRect.AutoValue(leftTopReticle.width), AutoRect.AutoValue(leftTopReticle.height)), leftTopReticle);
			GUI.DrawTexture(new Rect(Sniper.lockAreaRect.xMax - AutoRect.AutoValue(rightTopReticle.width / 2), Sniper.lockAreaRect.yMin - AutoRect.AutoValue(rightTopReticle.height / 2), AutoRect.AutoValue(rightTopReticle.width), AutoRect.AutoValue(rightTopReticle.height)), rightTopReticle);
			GUI.DrawTexture(new Rect(Sniper.lockAreaRect.xMin - AutoRect.AutoValue(leftBottomReticle.width / 2), Sniper.lockAreaRect.yMax - AutoRect.AutoValue(leftBottomReticle.height / 2), AutoRect.AutoValue(leftBottomReticle.width), AutoRect.AutoValue(leftBottomReticle.height)), leftBottomReticle);
			GUI.DrawTexture(new Rect(Sniper.lockAreaRect.xMax - AutoRect.AutoValue(rightBottomReticle.width / 2), Sniper.lockAreaRect.yMax - AutoRect.AutoValue(rightBottomReticle.height / 2), AutoRect.AutoValue(rightBottomReticle.width), AutoRect.AutoValue(rightBottomReticle.height)), rightBottomReticle);
			Sniper sniper = (Sniper)weapon;
			List<NearestEnemyInfo> nearestEnemyInfoList = sniper.GetNearestEnemyInfoList();
			{
				foreach (NearestEnemyInfo item in nearestEnemyInfoList)
				{
					GUI.DrawTexture(new Rect(item.currentScreenPos.x - AutoRect.AutoValue((float)reticle.width * 0.5f), item.currentScreenPos.y - AutoRect.AutoValue((float)reticle.height * 0.5f), AutoRect.AutoValue(reticle.width), AutoRect.AutoValue(reticle.height)), reticle);
				}
				return;
			}
		}
		if (weapon.GetWeaponType() == WeaponType.AssaultRifle)
		{
			AssaultRifle assaultRifle = (AssaultRifle)weapon;
			if (assaultRifle.curEnemyInfo != null)
			{
				Rect rect = new Rect(assaultRifle.curEnemyInfo.currentScreenPos.x - AutoRect.AutoValue((float)reticle.width * 0.5f), assaultRifle.curEnemyInfo.currentScreenPos.y - AutoRect.AutoValue((float)reticle.height * 0.5f), AutoRect.AutoValue(reticle.width), AutoRect.AutoValue(reticle.height));
				GUI.DrawTexture(new Rect(assaultRifle.curEnemyInfo.currentScreenPos.x - AutoRect.AutoValue((float)reticle.width * 0.5f), assaultRifle.curEnemyInfo.currentScreenPos.y - AutoRect.AutoValue((float)reticle.height * 0.5f), AutoRect.AutoValue(reticle.width), AutoRect.AutoValue(reticle.height)), reticle);
				reticlePosition = new Vector3(rect.x + rect.width / 2f, rect.y + rect.height / 2f, 0f);
			}
			else
			{
				GUI.DrawTexture(new Rect(reticlePosition.x - AutoRect.AutoValue((float)reticle.width * 0.5f * mutipleSizeReticle), reticlePosition.y - AutoRect.AutoValue((float)reticle.height * 0.5f * mutipleSizeReticle), AutoRect.AutoValue((float)reticle.width * mutipleSizeReticle), AutoRect.AutoValue((float)reticle.height * mutipleSizeReticle)), reticle);
				reticlePosition = new Vector3(Screen.width / 2, Screen.height / 2, 0f);
			}
		}
		else if (weapon.GetWeaponType() == WeaponType.MachineGun)
		{
			MachineGun machineGun = (MachineGun)weapon;
			if (machineGun.curEnemyInfo != null)
			{
				Rect rect2 = new Rect(machineGun.curEnemyInfo.currentScreenPos.x - AutoRect.AutoValue((float)reticle.width * 0.5f), machineGun.curEnemyInfo.currentScreenPos.y - AutoRect.AutoValue((float)reticle.height * 0.5f), AutoRect.AutoValue(reticle.width), AutoRect.AutoValue(reticle.height));
				GUI.DrawTexture(new Rect(machineGun.curEnemyInfo.currentScreenPos.x - AutoRect.AutoValue((float)reticle.width * 0.5f), machineGun.curEnemyInfo.currentScreenPos.y - AutoRect.AutoValue((float)reticle.height * 0.5f), AutoRect.AutoValue(reticle.width), AutoRect.AutoValue(reticle.height)), reticle);
				reticlePosition = new Vector3(rect2.x + rect2.width / 2f, rect2.y + rect2.height / 2f, 0f);
			}
			else
			{
				GUI.DrawTexture(new Rect(reticlePosition.x - AutoRect.AutoValue((float)reticle.width * 0.5f * mutipleSizeReticle), reticlePosition.y - AutoRect.AutoValue((float)reticle.height * 0.5f * mutipleSizeReticle), AutoRect.AutoValue((float)reticle.width * mutipleSizeReticle), AutoRect.AutoValue((float)reticle.height * mutipleSizeReticle)), reticle);
				reticlePosition = new Vector3(Screen.width / 2, Screen.height / 2, 0f);
			}
		}
		else
		{
			GUI.DrawTexture(new Rect(reticlePosition.x - AutoRect.AutoValue((float)reticle.width * 0.5f * mutipleSizeReticle), reticlePosition.y - AutoRect.AutoValue((float)reticle.height * 0.5f * mutipleSizeReticle), AutoRect.AutoValue((float)reticle.width * mutipleSizeReticle), AutoRect.AutoValue((float)reticle.height * mutipleSizeReticle)), reticle);
			reticlePosition = new Vector3(Screen.width / 2, Screen.height / 2, 0f);
		}
	}

	private void NormalizeQuaternion(ref Quaternion q)
	{
		float num = 0f;
		for (int i = 0; i < 4; i++)
		{
			num += q[i] * q[i];
		}
		float num2 = 1f / Mathf.Sqrt(num);
		for (int j = 0; j < 4; j++)
		{
			int index;
			int index2 = (index = j);
			float num3 = q[index];
			q[index2] = num3 * num2;
		}
	}
}
