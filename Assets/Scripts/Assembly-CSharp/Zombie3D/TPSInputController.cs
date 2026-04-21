using System.Collections.Generic;
using UnityEngine;

namespace Zombie3D
{
    public class TPSInputController : InputController
    {
        private VitaInputManager vitaInput = null;

      /*  void Start()
        {
#if UNITY_PSP2 && !UNITY_EDITOR
            vitaInput = VitaInputManager.Instance;   // Use the singleton (safer than new)
            if (vitaInput != null)
            {
                // Subscribe to the events that exist in your VitaInputManager
                vitaInput.OnLeftStick += HandleLeftStick;
                vitaInput.OnRTrigDown += HandleShootBegin;
                vitaInput.OnRTrigUp += HandleShootEnd;
             // vitaInput.OnRightStick += HandleRightStick; // for camera rotation
            }
#endif
        }

        // New handlers for Vita inputs
        private void HandleLeftStick(float horizontal, float vertical)
        {
            if (!base.EnableMoveInput) return;

            float distance = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
            if (distance < 0.1f)
            {
                // Stick is centered → stop moving
                base.InputInfo.IsMoving = false;
                base.InputInfo.moveDirection = Vector3.zero;
                player.SetMoveDirection();
                return;
            }

            // Convert to angle in radians (0 = right, increases counter-clockwise)
            float angle = Mathf.Atan2(vertical, horizontal);

            // Call your existing move logic (eventType 2 = Moved)
            ProcessMoveInput(2, distance, angle);
        }

        private void HandleShootBegin()
        {
            if (base.EnableShootingInput)
            {
                GameApp.GetInstance().GetGameScene().GetPlayer().OnFireBegin();
                base.InputInfo.fire = true;
            }
        }

        private void HandleShootEnd()
        {
            if (base.EnableShootingInput)
            {
                base.InputInfo.fire = false;
            }
        }
        */
        public override void ProcessFireInput(int inputEventType, float distance, float angle, TUIInput data)
        {
            if (base.EnableShootingInput)
            {
                switch (inputEventType)
                {
                    case 1:  // Pressed / Began
                        GameApp.GetInstance().GetGameScene().GetPlayer().OnFireBegin();
                        base.InputInfo.fire = true;
                        break;
                    case 3:  // Released / Ended
                        base.InputInfo.fire = false;
                        break;
                }
            }
        }

        public override void ProcessMoveInput(int inputEventType, float distance, float angle)
        {
            if (base.EnableMoveInput)
            {
                base.InputInfo.moveDirection = new Vector3(distance * Mathf.Cos(angle), 0f, distance * Mathf.Sin(angle));
                base.InputInfo.moveDirection = player.GetTransform().TransformDirection(base.InputInfo.moveDirection);
                base.InputInfo.moveDirection += Physics.gravity * Time.deltaTime * 20f;
                player.SetMoveDirection();

                if (inputEventType == 1 || inputEventType == 2)
                {
                    base.InputInfo.IsMoving = true;
                }
                else
                {
                    base.InputInfo.IsMoving = false;
                }
            }
        }

        public override void ProcessRotateInput(int inputEventType, TUIInput data)
        {
            if (base.EnableTurningAround)
            {
                switch (inputEventType)
                {
                    case 1:
                        lastTouchPosition = data.position;
                        Debug.Log("case rotate 1");
                        break;
                    case 2:
                        cameraRotation.x = (data.position.x - lastTouchPosition.x) * 0.24f;
                        cameraRotation.y = (data.position.y - lastTouchPosition.y) * 0.128f;
                        lastTouchPosition = data.position;
                        Debug.Log("case rotate 2");
                        break;
                    case 3:
                        cameraRotation = Vector2.zero;
                        Debug.Log("case rotate 3");
                        break;
                }
            }
        }

        /*public override void ProcessInput(float deltaTime, InputInfo inputInfo)
        {
            // This is the PC/Editor + keyboard path (WASD + weapon hotkeys)
            if (base.EnableMoveInput)
            {
                inputInfo.moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
                inputInfo.moveDirection = player.GetTransform().TransformDirection(inputInfo.moveDirection);
                inputInfo.IsMoving = true;
                Debug.Log("set move dire to get axis");
            }
            inputInfo.moveDirection += Physics.gravity * deltaTime * 20f;
            player.SetMoveDirection();

            if (inputInfo.moveDirection.x != 0f || inputInfo.moveDirection.z != 0f)
            {
                inputInfo.IsMoving = true;
                Debug.Log("moving x");
            }
            else
            {
                inputInfo.IsMoving = false;
            }

            // Weapon switching + debug keys (PC only)
            List<Weapon> battleWeapons = GameApp.GetInstance().GetGameState().GetBattleWeapons();
            for (int i = 1; i <= battleWeapons.Count; i++)
            {
                if (Input.GetButton("Weapon" + i) && player.GetWeapon().Name != battleWeapons[i - 1].Name)
                {
                    player.ChangeWeaponAndSendMsg(i - 1);
                }
            }
            if (Input.GetButton("H"))
            {
                player.GetHealed((int)player.MaxHp);
            }
            if (Input.GetButtonDown("K"))
            {
                player.enableHit = !player.enableHit;
            }
            if (Input.GetButtonDown("N"))
            {
                GameObject.Find("ArenaTrigger").GetComponent<ArenaTriggerFromConfigScript>().enabled = false;
                GameApp.GetInstance().GetGameScene().GamePlayingState = PlayingState.GameWin;
                GameApp.GetInstance().GetGameState().DayUp();
                GameApp.GetInstance().Save();
                SceneName.LoadLevel("MainMapTUI");
            }
        }*/

       public override void ProcessInput(float deltaTime, InputInfo inputInfo)
        {
#if UNITY_PSP2 && !UNITY_EDITOR
        inputInfo.moveDirection = new Vector3(Input.GetAxis("Left Joystick Horizontal"), 0f, Input.GetAxis("Left Joystick Vertical"));
        inputInfo.moveDirection = player.GetTransform().TransformDirection(inputInfo.moveDirection);
        inputInfo.IsMoving = true;

#else
            // ====================== PC/EDITOR KEYBOARD (unchanged) ======================
            if (base.EnableMoveInput)
    {
        inputInfo.moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        inputInfo.moveDirection = player.GetTransform().TransformDirection(inputInfo.moveDirection);
        inputInfo.IsMoving = true;
    }
#endif

            // Shared code (runs on both PC and Vita)
            inputInfo.moveDirection += Physics.gravity * deltaTime * 20f;
            player.SetMoveDirection();

            if (inputInfo.moveDirection.x != 0f || inputInfo.moveDirection.z != 0f)
            {
                inputInfo.IsMoving = true;
            }
            else
            {
                inputInfo.IsMoving = false;
            }

            // PC-only weapon switching + debug keys (won't affect Vita)
            List<Weapon> battleWeapons = GameApp.GetInstance().GetGameState().GetBattleWeapons();
            for (int i = 1; i <= battleWeapons.Count; i++)
            {
                if (Input.GetButton("Weapon" + i) && player.GetWeapon().Name != battleWeapons[i - 1].Name)
                {
                    player.ChangeWeaponAndSendMsg(i - 1);
                }
            }
            if (Input.GetButton("H"))
            {
                player.GetHealed((int)player.MaxHp);
            }
            if (Input.GetButtonDown("K"))
            {
                player.enableHit = !player.enableHit;
            }
            if (Input.GetButtonDown("N"))
            {
                GameObject.Find("ArenaTrigger").GetComponent<ArenaTriggerFromConfigScript>().enabled = false;
                GameApp.GetInstance().GetGameScene().GamePlayingState = PlayingState.GameWin;
                GameApp.GetInstance().GetGameState().DayUp();
                GameApp.GetInstance().Save();
                SceneName.LoadLevel("MainMapTUI");
            }
        }
    }
}
