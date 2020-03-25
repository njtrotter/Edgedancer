﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [Header("Camera Configs")]
    public bool m_thirdPersonMode = false;
    public Vector3 m_firstPersonPosition;
    public Vector3 m_ThirdPersonPosition;


    #region Variables
    [Header("Movement Customizations")]
    [Range(1f, 5f)]
    public int m_screenShake = 1;

    [Range(2f, 10f)]
    public int m_WalkMultiplier = 7;

    [Range(10f, 15f)]
    public int m_RunMultiplier = 12;

    [Range(1f, 7f)]
    public int m_DuckMultiplier = 4;

    [Range(1f, 15f)]
    public int m_XLookSensitivity = 10;

    [Range(1f, 15f)]
    public int m_YLookSensitivity = 10;

    [Range(1f, 10f)]
    public int m_jumpForce = 2;

    [Range(1, 4)]
    public float m_FallMultiplier = 2.5f;

    [Range(1, 10)]
    public int m_ClimbHeight = 2;


    [Header("Grapple Variables")]

    [Range(15, 50)]
    public int m_grappleRange = 20;


    [Header("Reticle Color")]
    public Color32 m_EnemyHit;
    public Color32 m_NormalColor;
    public Color32 m_LockedColor;
    public Color32 m_notAvailableColor;
    public Color32 m_InteractingColor;


    [Header("Horizontal-Look Restraints")]
    public float m_minimumXLook = -360f;
    public float m_maximumXLook = 360f;

    [Header("Vertical Look Restraints")]
    public float m_minimumYLook = -60f;
    public float m_maximumYLook = 60f;

    public Animator m_Animator;
    public int frameCounter = 10;

    //Inputs
    float vert_move;
    float hor_move;
    float v_look;
    float h_look;
    float run_intensity;
    bool duck_button;
    bool jump_button;
    bool grapple_button;
    bool pause_button;

    //bool run_toggle;
    public bool duck_toggle;
    bool pause_toggle;

    Rigidbody rb;
    Transform parentTransform;
    [Header("Character States")]
    public CharacterState CharacterState = new CharacterState();

    float rotationX;
    float rotationY;

    // Grapple
    //[SerializeField] private Transform debugHitPointTransform;
    [SerializeField] private Transform hookshotTransform;

    private Vector3 hookshotPosition;
    public bool grapple_hit = false;
    private float hookshotSize;

    private Vector3 characterVelocityMomentum;
    private GrappleState grapple_state;
    private enum GrappleState
    {
        Idle,
        CanHit,
        HookshotThrown,
        HookshotFlyingPlayer,
    }

    //Make private once testing is completed
    public bool isGrounded = true;
    public bool wallHitClimb = false;
    public bool rightWallRun = false;
    public bool leftWallRun = false;

    List<float> lookRotationInputs = new List<float>();
    List<float> movementInputs = new List<float>();

    Vector3 moveDirection;
    Vector3 duckHeight = new Vector3(1, 0.5f, 1);
    Vector3 normalHeight = new Vector3(1, 1, 1);
    Transform feet;

    int layerMask = 1 << 8;

    RaycastHit hit;
    RaycastHit rightHit;
    RaycastHit leftHit;
    RaycastHit grappleHit;
    LineRenderer grappleGraphic;


    //Bool Checks
    bool canUseGrapple = true;
    bool jump_Called = false;
    bool climb_timer_countdown = false;
    float climbDistance = 2;
    bool wallRun_timer_countdown = false;
    public float WallRunDist = 2;
    float initialFieldOfView;
    float velocitydependentFieldOfView;

    #endregion

    private void Awake()
    {
        hookshotTransform.gameObject.SetActive(false);
        if (!m_thirdPersonMode)
        {
            GameObject.Find("Joints").SetActive(false);
            Camera.main.transform.localPosition = m_firstPersonPosition;
        }
        else
        {
            Camera.main.transform.localPosition = m_ThirdPersonPosition;
        }
    }
    private void Start()
    {
        parentTransform = GetComponentInParent<Transform>();
        feet = GameObject.Find("Player Feet").GetComponent<Transform>();
        rb = GetComponentInParent<Rigidbody>();
        PopulateList(lookRotationInputs, 2);
        PopulateList(movementInputs, 2);
        initialFieldOfView = Camera.main.fieldOfView;
    }

    private void Update()
    {
        ClimbUpdateLoop();
        WallRunUpdateLoop();

        if (GameManager.instance.pauseMenuActive)
        {
            m_XLookSensitivity = (int)GameManager.instance.sensitivity;
            m_YLookSensitivity = (int)GameManager.instance.sensitivity - 1;
        }
    }

    void FixedUpdate()
    {
        Inputs();
        SetCharacterLocation();
    }

    void Inputs()
    {

        // Get Inputs from Joystick for Movement
        vert_move = Input.GetAxis("Vertical");
        hor_move = Input.GetAxis("Horizontal");
        v_look = Input.GetAxis("Vertical Look");
        h_look = Input.GetAxis("Horizontal Look");
        run_intensity = Input.GetAxis("Run");
        duck_button = Input.GetButton("Duck");
        jump_button = Input.GetButton("Jump");
        grapple_button = Input.GetButton("Grapple");
        pause_button = Input.GetButton("Pause");

        // Detects the state the player currently is.
        CharacterState = CurrentMovementStage();

        if (!StillLooking(lookRotationInputs))
            RotateHorizontal(h_look);

        PlayerActions();
        SceneActions();

        if (m_thirdPersonMode)
            SetAnimation(CharacterState);

    }

    #region PlayerActions

    void SceneActions()
    {
        if (pause_button)
        {
            Debug.Log("Current State = " + pause_button);
            GameManager.instance.pauseMenuActive = !GameManager.instance.pauseMenuActive;
        }

    }

    void PlayerActions()
    {
        AdjustCameraFOV();
        RayCollisionClimb();
        RayCollisionWallRun();
        if (GameManager.instance.canGrapple)
            GrappleMovement();

        // Methods on the player
        if (GameManager.instance.canMove)
            Movement(vert_move, hor_move);

        if (GameManager.instance.canLookAround)
            RotateVertical(v_look);

        if (jump_button && isGrounded && GameManager.instance.canJump) { Jump(); }

        if (jump_button && wallHitClimb && GameManager.instance.canJump) { Climb(); }

        if (jump_button && (rightWallRun || leftWallRun) && GameManager.instance.canJump) { WallRun(); }

        if (jump_button && !rightWallRun && !leftWallRun && !wallHitClimb)
        {
            rb.useGravity = true;
            Camera.main.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, 0);
        }

        if (!isGrounded) BetterJump();

        if (!jump_button && !leftWallRun && !rightWallRun)
        {
            Camera.main.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, 0);
            rb.useGravity = true;
            wallRun_timer_countdown = false;
        }

        if (rightWallRun || leftWallRun)
        {
            rb.velocity = new Vector3(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -20, 0), rb.velocity.z);
        }

        if (duck_toggle && isGrounded) Slide();

    }

    #endregion

    #region Movement And Rotation    

    /// <summary>
    /// Moves the player in their local axes.
    /// </summary>
    /// <param name="vert"></param>
    /// <param name="hor"></param>
    void Movement(float vert, float hor)
    {

        if (CharacterState.character_activity == ActivityState.climb) { vert = Mathf.Clamp(vert, 0, 1); hor = 0; }

        if (CharacterState.character_activity == ActivityState.wallrun)
        {
            vert = 1;

            if (leftWallRun)
            {
                hor = Mathf.Clamp(hor, 0, 1);
            }
            else if (rightWallRun)
            {
                hor = Mathf.Clamp(hor, -1, 0);
            }
        }

        //While Climbing don't take any inputs
        if (jump_button && wallHitClimb) { vert = Mathf.Clamp(vert, -1, 0); }

        //Get the direction in the local co-ordinates of the player to move.
        moveDirection = transform.TransformDirection(new Vector3(hor, 0, vert));

        //Multiplier added to the given direction vector according to the Input State.

        if ((CharacterState.character_movement == MovementType.walk && CharacterState.character_activity == ActivityState.none) || !GameManager.instance.canRun)
        {
            moveDirection *= m_WalkMultiplier;
        }
        else if (CharacterState.character_movement == MovementType.run && GameManager.instance.canRun)
        {
            moveDirection *= (m_RunMultiplier * run_intensity);
        }
        else if (CharacterState.character_movement == MovementType.walk && CharacterState.character_activity == ActivityState.duck)
        {
            moveDirection *= m_DuckMultiplier;
        }

        //Movement
        rb.MovePosition(rb.position + moveDirection * Time.fixedDeltaTime);
    }

    void BetterJump()
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (m_FallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Horizontal rotation done according to the given input float value; 
    /// 1.0f >= vert >= -1.0f
    /// </summary>
    /// <param name="hor"></param>
    void RotateHorizontal(float hor)
    {
        //Adds the rotation angle according to the input and sensitivity
        rotationX = hor * m_XLookSensitivity;

        // Makes the Vector according to along which axis the rotation should be added and by what magnitude.
        transform.parent.transform.localEulerAngles += new Vector3(0, rotationX, 0);
    }


    /// <summary>
    /// Vertical rotation done according to the given input float value; 
    /// 1.0f >= vert >= -1.0f
    /// </summary>
    /// <param name="vert"></param>
    void RotateVertical(float vert)
    {

        // Adds the rotation angle according to the input and sensitivity
        rotationY = vert * m_YLookSensitivity;

        // Makes the Vector according to along which axis the rotation should be and by what magnitude.
        Vector3 rotationYVector = new Vector3(rotationY, 0, 0);

        //Clamps the rotation between the specified values while rotating just the camera.
        if (Camera.main.transform.eulerAngles.x + rotationYVector.x > 45 &&
            Camera.main.transform.eulerAngles.x + rotationYVector.x < 300)
            return;
        else Camera.main.transform.eulerAngles += rotationYVector;

    }


    void Slide()
    {
        // Debug.Log("Slide Called");
        rb.AddForce(moveDirection * rb.velocity.magnitude, ForceMode.Impulse);
    }

    void Jump()
    {
        Debug.Log("Jump called");

        if (!wallHitClimb && !jump_Called && !leftWallRun && !rightWallRun)
        {
            rb.AddForce(parentTransform.up * m_jumpForce, ForceMode.Impulse);
            CharacterState.character_activity = ActivityState.jump;
            jump_Called = true;
        }
    }




    void Climb()
    {
        CharacterState.character_activity = ActivityState.climb;
        rb.useGravity = false;
        climb_timer_countdown = true;
        rb.MovePosition(new Vector3(rb.transform.position.x, rb.transform.position.y + m_ClimbHeight * Time.deltaTime, rb.transform.position.z));
        jump_Called = true;
    }

    void WallRun()
    {
        Debug.Log("WallRunning");
        if (rightWallRun)
            Camera.main.transform.localEulerAngles = new Vector3(0, 0, 10);
        if (leftWallRun)
            Camera.main.transform.localEulerAngles = new Vector3(0, 0, -10);
        CharacterState.character_activity = ActivityState.wallrun;
        rb.useGravity = false;
        wallRun_timer_countdown = true;

    }

    private void HandleHookShotDetect()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out grappleHit, m_grappleRange))
        {
            if (grappleHit.collider.gameObject.CompareTag("Enemy"))
            {
                GameManager.instance.m_ReticleImage.color = m_EnemyHit;
            }
            else
            {
                // Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * m_grappleRange, Color.red);
                GameManager.instance.grapple_availableImage.color = m_LockedColor;
                GameManager.instance.m_ReticleImage.color = m_NormalColor;

                if (grapple_state == GrappleState.Idle)
                {
                    hookshotTransform.gameObject.SetActive(false);
                    grapple_state = GrappleState.CanHit;
                }
            }
        }

        else
        {
            // Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * m_grappleRange, Color.white);
            grapple_state = GrappleState.Idle;
            GameManager.instance.grapple_availableImage.color = m_notAvailableColor;
            GameManager.instance.m_ReticleImage.color = m_NormalColor;
        }
    }

    private void HandleHookshotStart()
    {
        hookshotPosition = grappleHit.point;
        hookshotSize = 0f;
        hookshotTransform.gameObject.SetActive(true);
        hookshotTransform.localScale = Vector3.zero;
        grapple_state = GrappleState.HookshotThrown;
        this.gameObject.GetComponent<AudioSource>().PlayOneShot(AudioManager.instance.grappleShot);
    }

    private void HandleHookshotThrown()
    {
        hookshotTransform.LookAt(hookshotPosition);
        GameManager.instance.grapple_availableImage.color = m_InteractingColor;
        float hookshotThrowSpeed = 75f;
        hookshotSize += hookshotThrowSpeed * Time.deltaTime;
        hookshotTransform.localScale = new Vector3(1, 1, hookshotSize);

        grappleGraphic = hookshotTransform.gameObject.GetComponentInChildren<LineRenderer>();
        grappleGraphic.SetPosition(0, feet.position);
        grappleGraphic.SetPosition(1, hookshotPosition);

        if (hookshotSize >= Vector3.Distance(transform.position, hookshotPosition))
        {
            Vector3 hookshotDir = (hookshotPosition - rb.transform.position).normalized;



            float hookshotSpeedMin = 10f;
            float hookshotSpeedMax = 40f;
            float hookshotSpeed = Mathf.Clamp(Vector3.Distance(rb.transform.position, hookshotPosition), hookshotSpeedMin, hookshotSpeedMax);
            float hookshotSpeedMultiplier = 1f;

            rb.AddForce(new Vector3(hookshotDir.x * Time.fixedDeltaTime * hookshotSpeed * hookshotSpeedMultiplier,
                                hookshotDir.y * Time.fixedDeltaTime * hookshotSpeed * hookshotSpeedMultiplier,
                                hookshotDir.z * Time.fixedDeltaTime * hookshotSpeed * hookshotSpeedMultiplier), ForceMode.Impulse);
            grapple_state = GrappleState.HookshotFlyingPlayer;
        }
    }

    #region Grapple
    // TODO: Try subtracting the x, y, z from the hookshotposition to the rayhit point
    private void HandleHookshotMovement()
    {
        hookshotTransform.LookAt(hookshotPosition);

        // Hookshot goes down in size based on position of player and hookshotPosition
        //hookshotTransform.localScale = new Vector3(1, 1, Mathf.Abs(hookshotPosition.z - rb.transform.position.z));
        grappleGraphic.SetPosition(0, feet.position);
        Vector3 hookshotDir = (hookshotPosition - rb.transform.position).normalized;

        //if (Camera.main.fieldOfView < initialFieldOfView + 40) Camera.main.fieldOfView += 15f * Time.deltaTime;

        float hookshotSpeedMin = 10f;
        float hookshotSpeedMax = 40f;
        float hookshotSpeed = Mathf.Clamp(Vector3.Distance(rb.transform.position, hookshotPosition), hookshotSpeedMin, hookshotSpeedMax);
        float hookshotSpeedMultiplier = 3f;


        //Move Character Controller

        rb.AddForce(new Vector3(hookshotDir.x * Time.fixedDeltaTime * hookshotSpeed * hookshotSpeedMultiplier,
                                 hookshotDir.y * Time.fixedDeltaTime * hookshotSpeed * hookshotSpeedMultiplier,
                                 hookshotDir.z * Time.fixedDeltaTime * hookshotSpeed * hookshotSpeedMultiplier), ForceMode.VelocityChange);

        float reachedHookshotPositionDistance = 5f;

        if (Vector3.Distance(feet.position, hookshotPosition) < reachedHookshotPositionDistance)
        {
            //Reached hookshot position
            //rb.AddForce(rb.velocity, ForceMode.Impulse);
            StopHookShot();
            grapple_state = GrappleState.Idle;
        }

        if (Input.GetButton("Jump"))
        {
            StopHookShot();
            grapple_state = GrappleState.Idle;
        }
    }

    private void StopHookShot()
    {
        // Debug.Log(rb.velocity);
        grapple_state = GrappleState.Idle;
        hookshotTransform.gameObject.SetActive(false);
        StartCoroutine("GrappleCountdown");
        StartCoroutine("Release");
    }

    //private bool TestInputDownHookshot()
    //{
    //    if (grapple_button == Input.GetButton("Grapple"))
    //    {
    //        return grapple_hit;
    //    }
    //}

    #endregion

    #endregion

    #region Helper Functions

    /// <summary>
    /// Checks if the player is still interacting with the Right Joystick in one movement.
    /// </summary>
    /// <param name="values"></param>
    /// <returns>bool</returns>
    bool StillLooking(List<float> values)
    {
        if (Mathf.Abs(values[0]) > Mathf.Abs(values[1])) return true;
        else if (Mathf.Abs(values[0]) == 1 && Mathf.Abs(values[1]) == 1
                || Mathf.Abs(values[0]) == -1 && Mathf.Abs(values[1]) == -1) return false;
        else if (Mathf.Abs(values[0]) == 0 && Mathf.Abs(values[1]) == 0) return true;
        else return false;
    }

    void PopulateList(List<float> values, int size)
    {
        for (int i = 0; i < size; ++i)
        {
            values.Add(0.0f);
        }
    }

    /// <summary>
    /// Returns the current Movement State of the player according to the game state.
    /// </summary>
    /// <returns>CharahterState</returns>
    CharacterState CurrentMovementStage()
    {

        CharacterState currentCharacterState;

        /*
         * Adds the value of current frame and keeps the value of the previous frame to compare if the 
         * player is still moving around.             
         */
        movementInputs.Add(vert_move);

        if (movementInputs.Count >= 2)
        {
            movementInputs.RemoveAt(0);
        }


        /*
         * Adds the value of current frame and keeps the value of the previous frame to compare if the 
         * player is still moving their Right Joystick to look around.
         */
        lookRotationInputs.Add(h_look);

        if (lookRotationInputs.Count >= 2)
        {
            lookRotationInputs.RemoveAt(0);
        }


        //Checks if there is no input then return idle state.


        //Checks the value of the Run toggle to detect if player has pressed the Run Toggle for a state change.
        if (run_intensity > 0 && !duck_button)
        {
            duck_button = false;
        }

        //if (duck_button) {
        //    duck_toggle = !duck_toggle;
        //}

        if (duck_button)
        {
            Debug.Log("DUCK");
            //run_intensity = 0;
            parentTransform.parent.transform.localScale = duckHeight;
        }
        else
        {
            parentTransform.parent.transform.localScale = normalHeight;
        }

        if (grapple_button)
        {
            currentCharacterState = new CharacterState(CharacterState.character_movement, ActivityState.grappling);
            return currentCharacterState;
        }

        //MovementType and ActivityState Detection Logic.
        if (run_intensity > 0 && Mathf.Abs(movementInputs[0]) > 0.85f && Mathf.Abs(movementInputs[1]) > 0.85f)
        {
            currentCharacterState = new CharacterState(MovementType.run, ActivityState.none);
            // Debug.Log("<color=blue> Run, None</color>");
            return currentCharacterState;
        }
        else if (run_intensity > 0 && Mathf.Abs(movementInputs[0]) <= 0.85f && Mathf.Abs(movementInputs[1]) <= 0.85f)
        {
            run_intensity = 0;
            // Debug.Log("<color=green> Walk, None</color>");
            currentCharacterState = new CharacterState(MovementType.walk, ActivityState.none);
            return currentCharacterState;

        }
        else if (duck_toggle && CharacterState.character_movement == MovementType.walk ||
                 duck_toggle && CharacterState.character_movement == MovementType.idle)
        {
            // Debug.Log("<color=yellow> Walk / Idle, Duck</color>");
            currentCharacterState = new CharacterState(MovementType.walk, ActivityState.duck);
            return currentCharacterState;
        }
        else if (vert_move == 0 && hor_move == 0)
        {
            currentCharacterState = new CharacterState(MovementType.idle, ActivityState.none);
            // Debug.Log("<color=red> Idle</color>");
            return currentCharacterState;
        }
        else
        {
            // Debug.Log("<color=green> Walk, None</color>");
            currentCharacterState = new CharacterState(MovementType.walk, ActivityState.none);
            return currentCharacterState;
        }
    }

    void AdjustCameraFOV() {
        velocitydependentFieldOfView = Mathf.Lerp(0, rb.velocity.y*10, Time.deltaTime * 5);
        Camera.main.fieldOfView = initialFieldOfView + velocitydependentFieldOfView;
    }

    void SetCharacterLocation()
    {
        if (CharacterLocState.instance.currentCharacterLocation == CharacterLocState.CharacterLocation.grounded)
        {
            jump_Called = false;
            grapple_hit = false;
            isGrounded = true;

            if (m_thirdPersonMode)
            {
                m_Animator.SetBool("jump", false);
                m_Animator.SetBool("climbing", false);
            }
        }
        if (CharacterLocState.instance.currentCharacterLocation == CharacterLocState.CharacterLocation.inAir)
        {
            isGrounded = false;
        }
    }


    void RayCollisionClimb()
    {
        if (Physics.Raycast(feet.position, transform.TransformDirection(Vector3.forward), out hit, 1f, layerMask))
        {
            // Debug.DrawRay(feet.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.red);
            wallHitClimb = true;
        }
        else
        {
            wallHitClimb = false;
            //  Debug.DrawRay(feet.position, transform.TransformDirection(Vector3.forward) * 2, Color.white);
        }
    }


    void RayCollisionWallRun()
    {
        if ((Physics.Raycast(rb.transform.position, transform.TransformDirection(Vector3.right), out rightHit, 1f, layerMask)) && !wallHitClimb)
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right), Color.red);
            rightWallRun = true;
        }
        else
        {
            // Debug.DrawRay(feet.position, transform.TransformDirection(Vector3.right), Color.white);           
            rightWallRun = false;
        }

        if (Physics.Raycast(rb.transform.position, transform.TransformDirection(-Vector3.right), out leftHit, 1f, layerMask) && !wallHitClimb)
        {
            // Debug.DrawRay(transform.position, transform.TransformDirection(-Vector3.right), Color.red);
            leftWallRun = true;
        }
        else
        {
            // Debug.DrawRay(feet.position, transform.TransformDirection(-Vector3.right), Color.white);
            leftWallRun = false;
        }
    }


    void GrappleMovement()
    {

        if (GameManager.instance.reloadLevel)
        {
            grapple_state = GrappleState.Idle;
            GameManager.instance.reloadLevel = false;
            return;
        }

        if ((grapple_state != GrappleState.HookshotThrown || grapple_state != GrappleState.HookshotFlyingPlayer)
            && canUseGrapple)
        {
            HandleHookShotDetect();
        }

        switch (grapple_state)
        {
            case GrappleState.CanHit:
                if (CharacterState.character_activity == ActivityState.grappling)
                {
                    HandleHookshotStart();
                }
                break;
            case GrappleState.HookshotThrown:
                HandleHookshotThrown();
                break;
            case GrappleState.HookshotFlyingPlayer:
                HandleHookshotMovement();
                break;
            default:
                hookshotTransform.gameObject.SetActive(false);
                break;
        }
    }

    IEnumerator GrappleCountdown()
    {
        canUseGrapple = false;
        GameManager.instance.grapple_availableImage.enabled = false;
        yield return new WaitForSeconds(2);
        this.gameObject.GetComponent<AudioSource>().PlayOneShot(AudioManager.instance.grappleReloaded);
        GameManager.instance.grapple_availableImage.enabled = true;
        canUseGrapple = true;
    }

    IEnumerator Release()
    {
        //while (Camera.main.fieldOfView > initialFieldOfView)
        //{
        //}
        yield return null;
    }

    void SetAnimation(CharacterState state)
    {
        //Always updating Falling State for fall Animation
        Vector3 vel = Vector3.Normalize(rb.velocity);
        m_Animator.SetFloat("JumpPos", vel.y);

        if (state.character_activity == ActivityState.none)
        {
            switch (state.character_movement)
            {
                case MovementType.idle:
                    m_Animator.SetBool("idle", true);
                    m_Animator.SetBool("running", false);
                    m_Animator.SetBool("walking", false);
                    break;
                case MovementType.walk:
                    m_Animator.SetBool("idle", false);
                    m_Animator.SetBool("walking", true);
                    m_Animator.SetBool("running", false);
                    m_Animator.SetFloat("Y-Axis", vert_move);
                    m_Animator.SetFloat("X-Axis", hor_move);
                    break;
                case MovementType.run:
                    m_Animator.SetBool("idle", false);
                    m_Animator.SetBool("walking", false);
                    m_Animator.SetBool("running", true);
                    break;
                default:
                    Debug.LogError("Invalid Movement State Animation");
                    break;
            }
        }

        else
        {
            switch (state.character_activity)
            {
                case ActivityState.jump:
                    m_Animator.SetBool("jump", true);
                    if (state.character_movement == MovementType.walk)
                    {
                        m_Animator.SetBool("walking", true);
                        m_Animator.SetBool("running", false);
                    }
                    else if (state.character_movement == MovementType.run)
                    {
                        m_Animator.SetBool("walking", false);
                        m_Animator.SetBool("running", true);
                    }
                    else
                    {
                        m_Animator.SetBool("walking", false);
                        m_Animator.SetBool("running", false);
                    }
                    break;
                case ActivityState.climb:
                    m_Animator.SetBool("climbing", true);
                    break;
            }
        }
    }

    void ClimbUpdateLoop()
    {
        if (climb_timer_countdown && climbDistance > 0)
        {
            climbDistance = HelperMethods.CountDown(climbDistance);
            if (m_thirdPersonMode)
                m_Animator.SetFloat("ClimbTimeLeft", climbDistance);
        }
        else if (climbDistance < 0)
        {
            climb_timer_countdown = false;
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (!climb_timer_countdown)
        {
            climbDistance = 2.0f;
            if (m_thirdPersonMode)
                m_Animator.SetFloat("ClimbTimeLeft", climbDistance);
        }
    }

    void WallRunUpdateLoop()
    {
        if (wallRun_timer_countdown && WallRunDist > 0)
        {

            WallRunDist = HelperMethods.CountDown(WallRunDist);
            if (m_thirdPersonMode)
            {
                //Insert here the Third person animation state Var
            }
        }
        else if (WallRunDist < 0)
        {
            wallRun_timer_countdown = false;
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (!wallRun_timer_countdown)
        {
            WallRunDist = 2.5f;
            if (m_thirdPersonMode)
            {
                //Insert here the Third person animation state Var
            }
        }
    }
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        Transform parentTrans = GetComponentInParent<Transform>();
        if (other.gameObject.CompareTag("WallEdge"))
        {
            if (CharacterState.character_activity == ActivityState.climb)
            {
                m_Animator.SetTrigger("Reached Edge");
            }
        }
    }
}
