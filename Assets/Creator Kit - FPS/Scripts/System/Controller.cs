using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class AmmoInventoryEntry
{
    [AmmoType]
    public int ammoType;
    public int amount = 0;
}

public class Controller : MonoBehaviour
{
    //Urg that's ugly, maybe find a better way
    public static Controller Instance { get; protected set; }

    public Camera MainCamera;
    public Camera WeaponCamera;

    public Transform CameraPosition;
    public Transform WeaponPosition;

    public Weapon[] startingWeapons;

    //this is only use at start, allow to grant ammo in the inspector. m_AmmoInventory is used during gameplay
    public AmmoInventoryEntry[] startingAmmo;

    [Header("Control Settings")]
    public float MouseSensitivity = 100.0f;
    public float PlayerSpeed = 5.0f;
    public float RunningSpeed = 7.0f;
    public float JumpSpeed = 5.0f;

    [Header("Audio")]
    public RandomPlayer FootstepPlayer;
    public AudioClip JumpingAudioCLip;
    public AudioClip LandingAudioClip;

    float m_VerticalSpeed = 0.0f;
    bool m_IsPaused = false;
    int m_CurrentWeapon;

    float m_VerticalAngle, m_HorizontalAngle;
    public float Speed { get; private set; } = 0.0f;

    public bool LockControl { get; set; }
    public bool CanPause { get; set; } = true;

    public bool Grounded => m_Grounded;
    bool loosedGrounding = false;

    CharacterController m_CharacterController;

    bool m_Grounded;
    float m_GroundedTimer;
    float m_SpeedAtJump = 0.0f;

    float usedSpeed;
    float actualSpeed;

    List<Weapon> m_Weapons = new List<Weapon>();
    Dictionary<int, int> m_AmmoInventory = new Dictionary<int, int>();


    //public InputAction playerControls;
    public MyPlayerInputActions myPlayerInputActions;
    InputAction movePlayer;
    InputAction rotatePlayer;
    InputAction changeWeapon;
    InputAction run;
    InputAction pauseGame;
    InputAction map;
    InputAction load;
    InputAction save;


    void Awake()
    {
        Instance = this;
        myPlayerInputActions = new MyPlayerInputActions();
    }
    public void OnEnable()
    {
        movePlayer = myPlayerInputActions.Player.Movement;
        movePlayer.Enable();


        rotatePlayer = myPlayerInputActions.Player.LookAround;
        rotatePlayer.Enable();

        changeWeapon = myPlayerInputActions.Player.ChangeWeapon;
        changeWeapon.Enable();

        run = myPlayerInputActions.Player.Run;
        run.Enable();

        pauseGame = myPlayerInputActions.Player.Pause;
        pauseGame.Enable();

        map = myPlayerInputActions.Player.Map;
        map.Enable();

        load = myPlayerInputActions.Player.Load;
        load.Enable();

        save = myPlayerInputActions.Player.Save;
        save.Enable();
    }

    private void OnDisable()
    {
        movePlayer.Disable();
        rotatePlayer.Disable();
        changeWeapon.Disable();
        run.Disable();
        pauseGame.Disable();
        map.Disable();
        load.Disable();
        save.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        m_IsPaused = false;
        m_Grounded = true;

        MainCamera.transform.SetParent(CameraPosition, false);
        MainCamera.transform.localPosition = Vector3.zero;
        MainCamera.transform.localRotation = Quaternion.identity;
        m_CharacterController = GetComponent<CharacterController>();

        for (int i = 0; i < startingWeapons.Length; ++i)
        {
            PickupWeapon(startingWeapons[i]);
        }

        for (int i = 0; i < startingAmmo.Length; ++i)
        {
            ChangeAmmo(startingAmmo[i].ammoType, startingAmmo[i].amount);
        }

        m_CurrentWeapon = -1;
        ChangeWeapon(0);

        for (int i = 0; i < startingAmmo.Length; ++i)
        {
            m_AmmoInventory[startingAmmo[i].ammoType] = startingAmmo[i].amount;
        }

        m_VerticalAngle = 0.0f;
        m_HorizontalAngle = transform.localEulerAngles.y;
    }

    void Update()
    {
        //if (CanPause && Input.GetButtonDown("Menu")) //OLD_ Pausar juego
        //{
        //    PauseMenu.Instance.Display();
        //}

        if (CanPause && pauseGame.IsPressed()) //NEW_ Pausar juego
        {
            PauseMenu.Instance.Display();
            //myPlayerInputActions.Player.Disable();
            //myPlayerInputActions.UI.Enable();           
        }

        
        //if (!CanPause && pauseGame.IsPressed()) //NEW_ Pausar juego
        //{
        //    PauseMenu.Instance.Display();
        //}

        //FullscreenMap.Instance.gameObject.SetActive(Input.GetButton("Map")); //Abrir el mapa
        FullscreenMap.Instance.gameObject.SetActive(map.IsPressed()); //Abrir el mapa


        bool wasGrounded = m_Grounded;
        loosedGrounding = false;

        //we define our own grounded and not use the Character controller one as the character controller can flicker
        //between grounded/not grounded on small step and the like. So we actually make the controller "not grounded" only
        //if the character controller reported not being grounded for at least .5 second;
        if (!m_CharacterController.isGrounded)
        {
            if (m_Grounded)
            {
                m_GroundedTimer += Time.deltaTime;
                if (m_GroundedTimer >= 0.5f)
                {
                    loosedGrounding = true;
                    m_Grounded = false;
                }
            }
        }
        else
        {
            m_GroundedTimer = 0.0f;
            m_Grounded = true;
        }

        Speed = 0;
        Vector3 move = Vector3.zero;
        if (!m_IsPaused && !LockControl)
        {
            // Jump (we do it first as 
            /*
            if (m_Grounded && Input.GetButtonDown("Jump"))
            {
                m_VerticalSpeed = JumpSpeed;
                m_Grounded = false;
                loosedGrounding = true;
                FootstepPlayer.PlayClip(JumpingAudioCLip, 0.8f,1.1f);
            }
            */
            //if not grounded && input ... try unreal logic
            //Jump has separated method

            if (m_Grounded)
            {
                loosedGrounding = true;
            }

            //bool running = m_Weapons[m_CurrentWeapon].CurrentState == Weapon.WeaponState.Idle && Input.GetButton("Run"); //correr
            bool running = m_Weapons[m_CurrentWeapon].CurrentState == Weapon.WeaponState.Idle && run.IsPressed();

            actualSpeed = running ? RunningSpeed : PlayerSpeed;

            if (loosedGrounding)
            {
                m_SpeedAtJump = actualSpeed;
            }

            // Move around with WASD---------------------------------------------------------

            //move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxisRaw("Vertical")); // OLD
            move = new Vector3(movePlayer.ReadValue<Vector2>().x, 0, movePlayer.ReadValue<Vector2>().y); //Nuevo sistema


            if (move.sqrMagnitude > 1.0f)
            {
                move.Normalize();
            }

            usedSpeed = m_Grounded ? actualSpeed : m_SpeedAtJump;
            move = move * usedSpeed * Time.deltaTime;
            move = transform.TransformDirection(move);
            m_CharacterController.Move(move);

            ////---------------------------------------------------------------------------------

            // Turn player
            //float turnPlayer = Input.GetAxis("Mouse X") * MouseSensitivity; //Old turn
            float turnPlayer = rotatePlayer.ReadValue<Vector2>().x * MouseSensitivity;
            m_HorizontalAngle = m_HorizontalAngle + turnPlayer;

            if (m_HorizontalAngle > 360) m_HorizontalAngle -= 360.0f;
            if (m_HorizontalAngle < 0) m_HorizontalAngle += 360.0f;

            Vector3 currentAngles = transform.localEulerAngles;
            currentAngles.y = m_HorizontalAngle;
            transform.localEulerAngles = currentAngles;

            // Camera look up/down -----------------------------------------------------------------
            //var turnCam = -Input.GetAxis("Mouse Y");
            var turnCam = rotatePlayer.ReadValue<Vector2>().y;

            turnCam = turnCam * MouseSensitivity;
            m_VerticalAngle = Mathf.Clamp(turnCam + m_VerticalAngle, -89.0f, 89.0f);
            currentAngles = CameraPosition.transform.localEulerAngles;
            currentAngles.x = m_VerticalAngle;
            CameraPosition.transform.localEulerAngles = currentAngles;

            //m_Weapons[m_CurrentWeapon].triggerDown = Input.GetMouseButton(0); // Fire?

            Speed = move.magnitude / (PlayerSpeed * Time.deltaTime);

            //if (Input.GetButton("Reload")) // Old Reload
                //m_Weapons[m_CurrentWeapon].Reload();


            // Old Change Weapon -----------------------------------------------------
            /*
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                ChangeWeapon(m_CurrentWeapon - 1);
            }
            else if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                ChangeWeapon(m_CurrentWeapon + 1);
            }
            */
            //////////////////////////////////////////////////

            //New Change Weapon ---Discard no control
            //Debug.Log("Scroll values: " + Input.GetAxis("Mouse ScrollWheel"));
            //if (changeWeapon.ReadValue<Vector2>().y < 0)
            //{
            //    //ChangeWeapon(m_CurrentWeapon - 1);
            //}
            //else if (changeWeapon.ReadValue<Vector2>().y > 0)
            //{
            //    //ChangeWeapon(m_CurrentWeapon + 1);
            //}
            //-------------------------------------

            //Key input to change weapon

            for (int i = 0; i < 10; ++i)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i)) //Se deja igual, no es relevante para el control
                {
                    int num = 0;
                    if (i == 0)
                        num = 10;
                    else
                        num = i - 1;

                    if (num < m_Weapons.Count)
                    {
                        ChangeWeapon(num);
                    }
                }
            }
        }

        // Fall down / gravity
        m_VerticalSpeed = m_VerticalSpeed - 10.0f * Time.deltaTime;
        if (m_VerticalSpeed < -10.0f)
            m_VerticalSpeed = -10.0f; // max fall speed
        var verticalMove = new Vector3(0, m_VerticalSpeed * Time.deltaTime, 0);
        var flag = m_CharacterController.Move(verticalMove);
        if ((flag & CollisionFlags.Below) != 0)
            m_VerticalSpeed = 0;

        if (!wasGrounded && m_Grounded)
        {
            FootstepPlayer.PlayClip(LandingAudioClip, 0.8f, 1.1f);
        }
    }

    public void DisplayCursor(bool display)
    {
        m_IsPaused = display;
        Cursor.lockState = display ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = display;
    }

    void PickupWeapon(Weapon prefab)
    {
        //TODO : maybe find a better way than comparing name...
        if (m_Weapons.Exists(weapon => weapon.name == prefab.name))
        {//if we already have that weapon, grant a clip size of the ammo type instead
            ChangeAmmo(prefab.ammoType, prefab.clipSize);
        }
        else
        {
            var w = Instantiate(prefab, WeaponPosition, false);
            w.name = prefab.name;
            w.transform.localPosition = Vector3.zero;
            w.transform.localRotation = Quaternion.identity;
            w.gameObject.SetActive(false);

            w.PickedUp(this);

            m_Weapons.Add(w);
        }
    }

    void ChangeWeapon(int number)
    {
        if (m_CurrentWeapon != -1)
        {
            m_Weapons[m_CurrentWeapon].PutAway();
            m_Weapons[m_CurrentWeapon].gameObject.SetActive(false);
        }

        m_CurrentWeapon = number;

        if (m_CurrentWeapon < 0)
            m_CurrentWeapon = m_Weapons.Count - 1;
        else if (m_CurrentWeapon >= m_Weapons.Count)
            m_CurrentWeapon = 0;

        m_Weapons[m_CurrentWeapon].gameObject.SetActive(true);
        m_Weapons[m_CurrentWeapon].Selected();
    }

    public int GetAmmo(int ammoType)
    {
        int value = 0;
        m_AmmoInventory.TryGetValue(ammoType, out value);

        return value;
    }

    public void ChangeAmmo(int ammoType, int amount)
    {
        if (!m_AmmoInventory.ContainsKey(ammoType))
            m_AmmoInventory[ammoType] = 0;

        var previous = m_AmmoInventory[ammoType];
        m_AmmoInventory[ammoType] = Mathf.Clamp(m_AmmoInventory[ammoType] + amount, 0, 999);

        if (m_Weapons[m_CurrentWeapon].ammoType == ammoType)
        {
            if (previous == 0 && amount > 0)
            {//we just grabbed ammo for a weapon that add non left, so it's disabled right now. Reselect it.
                m_Weapons[m_CurrentWeapon].Selected();
            }

            WeaponInfoUI.Instance.UpdateAmmoAmount(GetAmmo(ammoType));
        }
    }

    public void PlayFootstep()
    {
        FootstepPlayer.PlayRandom();
    }

    //----------------------------------------------------------MODIFICACIONES--------------------------------------------------------------

    public void NewJump(InputAction.CallbackContext context) //Salto
    {
        if (!m_IsPaused && !LockControl)
        {
            if (m_Grounded && context.started)
            {
                m_VerticalSpeed = JumpSpeed;
                m_Grounded = false;
                //loosedGrounding = true;
                FootstepPlayer.PlayClip(JumpingAudioCLip, 0.8f, 1.1f);
                //Debug.Log("Im jumpin");
            }
        }


    }

    public void FireWeapon(InputAction.CallbackContext context)
    {
        if (!m_IsPaused && !LockControl)
        {

            if (!context.canceled)
            {
                m_Weapons[m_CurrentWeapon].triggerDown = true;
            }
            else
            {
                m_Weapons[m_CurrentWeapon].triggerDown = false;
            }
        }

    }

    public void ReloadWeapon(InputAction.CallbackContext context)
    {
        if (!m_IsPaused && !LockControl)
        {
            if (context.started)
            {
                m_Weapons[m_CurrentWeapon].Reload();
            }
        }

    }

    public void Input_ChangeWeapon(InputAction.CallbackContext context)
    {
        if (!m_IsPaused && !LockControl)
        {
            if (changeWeapon.ReadValue<Vector2>().y < 0  || changeWeapon.ReadValue<Vector2>().x < 0)
            {
                ChangeWeapon(m_CurrentWeapon - 1);
            }
            else if (changeWeapon.ReadValue<Vector2>().y > 0 || changeWeapon.ReadValue<Vector2>().x > 0)
            {
                ChangeWeapon(m_CurrentWeapon + 1);
            }
        }

    }


    public void SavePlayer(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            SaveSystem.SavePlayer(this);
            Debug.Log("Saved Position: " + transform.position);

        }
    }

    public void LoadPlayer(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            SaveData data = SaveSystem.LoadPlayer();

            //Se cargan los datos guardados y se asignan
            Vector3 position;
            position.x = data.playerPosition[0];
            position.y = data.playerPosition[1];
            position.z = data.playerPosition[2];

            //this.gameObject.transform.position = position;

            Debug.Log("Loaded data: " + data.playerPosition[0].ToString("f1") + ", " + data.playerPosition[1].ToString("f1") + ", " + data.playerPosition[2].ToString("f1"));
        }
        

    }

    /*public void NewMovement(InputAction.CallbackContext context)
    {
        //Debug.Log("Input:" + context);
        Vector3 move = Vector3.zero;
        //move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        move = new Vector3(context.ReadValue<Vector2>().x, 0, context.ReadValue<Vector2>().y);
        if (move.sqrMagnitude > 1.0f)
            move.Normalize();

        //usedSpeed = m_Grounded ? actualSpeed : m_SpeedAtJump;

        move = move * usedSpeed * Time.deltaTime;

        move = transform.TransformDirection(move);
        m_CharacterController.Move(move);

    }
    */
}
