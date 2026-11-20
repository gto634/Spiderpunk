using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Interactions;

[DefaultExecutionOrder(-10)]
public class BrainPlayer : MonoBehaviour
{
    const float GamepadReferenceFrameRate = 60f;

    [Header("Systems")]
    [SerializeField] MovementPlayer movementPlayer;
    [SerializeField] DashPlayer dashPlayer;
    [SerializeField] OrbitCameraPlayer orbitCameraPlayer;
    [SerializeField] ShoulderCameraPlayer shoulderCameraPlayer;
    [SerializeField] RotateSystem rotateSystem;
    [SerializeField] NormalAttackPlayer normalAttackPlayer;
    [SerializeField] ShootAttackPlayer shootAttackPlayer;
    [SerializeField] CompetenceOnePlayer competenceOnePlayer;
    [SerializeField] InteractionPlayer interactionPlayer;
    [SerializeField] EnableBackground guitarBackground;
    [SerializeField] GameObject shoulderCamera;
    [SerializeField, Tooltip("Time in shoulder view after firing a web on mouse release.")]
    Clock postShotAimClock = new Clock { Duration = 0.45 };
    [SerializeField, ReadOnly] bool isAiming;

    public bool IsAiming => isAiming;
    public bool IsDashing => isActiveAndEnabled && dashPlayer != null && dashPlayer.IsDashing;
    public float DashDuration => dashPlayer != null ? dashPlayer.Duration : 0f;
    public bool IsAttacking => (normalAttackPlayer != null && normalAttackPlayer.IsAttacking) ||
        (competenceOnePlayer != null && competenceOnePlayer.IsAttacking);
    public event Action OnShoot;
    public event Action<bool> OnAimingChanged;

    InputAction moveInput;
    InputAction sprintInput;
    InputAction dashInput;
    InputAction lookAxes;
    InputAction attackInput;
    InputAction aimInput;
    InputAction shootInput;
    InputAction competenceOneInput;
    InputAction interactInput;
    InputAction toggleGuitarBackgroundInput;
    bool sprintFromDash;
    bool attackRequested;
    bool mouseAiming;
    bool mouseShotRequested;
    bool shootRequested;
    bool competenceOneRequested;

    bool HasAimInput => mouseAiming || aimInput?.IsPressed() == true;
    
    #region Setup

    private void Reset()
    {
        movementPlayer = GetComponentInChildren<MovementPlayer>();
        dashPlayer = GetComponentInChildren<DashPlayer>();
        orbitCameraPlayer = GetComponentInChildren<OrbitCameraPlayer>();
        shoulderCameraPlayer = GetComponentInChildren<ShoulderCameraPlayer>();
        rotateSystem = GetComponentInChildren<RotateSystem>();
        normalAttackPlayer = GetComponentInChildren<NormalAttackPlayer>();
        interactionPlayer = GetComponentInChildren<InteractionPlayer>();
        guitarBackground = GetComponentInChildren<EnableBackground>();
    }

    void OnEnable()
    {
        InputActionAsset actions = InputSystem.actions;
        moveInput = actions?.FindAction("Player/MoveInput");
        sprintInput = actions?.FindAction("Player/Sprint");
        dashInput = actions?.FindAction("Player/Dash");
        lookAxes = actions?.FindAction("Player/OrbitAxes");
        attackInput = actions?.FindAction("Player/Attack");
        aimInput = actions?.FindAction("Player/Aim");
        shootInput = actions?.FindAction("Player/Shoot");
        competenceOneInput = actions?.FindAction("Player/CompetenceOne");
        interactInput = actions?.FindAction("Player/Interact");
        toggleGuitarBackgroundInput = actions?.FindAction("Player/ToggleGuitarBackground");

        ValidateConfiguration();

        if (normalAttackPlayer != null)
            normalAttackPlayer.OnAttackChanged += OnNormalAttackChanged;

        if (attackInput != null)
        {
            attackInput.performed += OnAttackPerformed;
            attackInput.canceled += OnAttackCanceled;
        }
        if (shootInput != null)
            shootInput.performed += OnShootPerformed;
        if (competenceOneInput != null)
            competenceOneInput.performed += OnCompetenceOnePerformed;
        if (toggleGuitarBackgroundInput != null)
            toggleGuitarBackgroundInput.performed += OnToggleGuitarBackground;

        if (aimInput != null)
        {
            aimInput.performed += OnAimChanged;
            aimInput.canceled += OnAimChanged;
        }
        SetAiming(aimInput?.IsPressed() == true);

        if (moveInput != null)
            moveInput.canceled += OnMoveCanceled;
    }

    void OnDisable()
    {
        sprintFromDash = false;
        attackRequested = false;
        mouseAiming = false;
        mouseShotRequested = false;
        postShotAimClock.ResetTimer();
        shootRequested = false;
        competenceOneRequested = false;
        if (normalAttackPlayer != null)
            normalAttackPlayer.OnAttackChanged -= OnNormalAttackChanged;

        if (attackInput != null)
        {
            attackInput.performed -= OnAttackPerformed;
            attackInput.canceled -= OnAttackCanceled;
        }
        if (shootInput != null)
            shootInput.performed -= OnShootPerformed;
        if (competenceOneInput != null)
            competenceOneInput.performed -= OnCompetenceOnePerformed;
        if (toggleGuitarBackgroundInput != null)
            toggleGuitarBackgroundInput.performed -= OnToggleGuitarBackground;
        if (shootAttackPlayer != null)
            shootAttackPlayer.CancelAttack();

        if (aimInput != null)
        {
            aimInput.performed -= OnAimChanged;
            aimInput.canceled -= OnAimChanged;
        }
        SetAiming(false);

        if (moveInput != null)
            moveInput.canceled -= OnMoveCanceled;

        ClearMovement();
    }

    void ValidateConfiguration()
    {
        if (movementPlayer == null)
        {
            Debug.LogError("BrainPlayer: movement is disabled. Assign Move System in the Inspector.", this);
            moveInput = null;
        }
        else if (moveInput == null)
        {
            Debug.LogError("BrainPlayer: movement is disabled. Add Player/MoveInput to the project-wide Input Actions.", this);
        }
        else if (rotateSystem == null)
        {
            Debug.LogError("BrainPlayer: character rotation is disabled. Assign Rotate System in the Inspector.", this);
        }

        if (moveInput == null)
            sprintInput = null;
        else if (sprintInput == null)
            Debug.LogError("BrainPlayer: sprint is disabled. Add Player/Sprint to the project-wide Input Actions.", this);

        if (orbitCameraPlayer == null)
            Debug.LogError("BrainPlayer: camera orbit is disabled. Assign Orbit Camera Player in the Inspector.", this);
        if (lookAxes == null)
            Debug.LogError("BrainPlayer: camera look is disabled. Add Player/OrbitAxes to the project-wide Input Actions.", this);
    }

    #endregion

    #region Feedback

    void OnToggleGuitarBackground(InputAction.CallbackContext _)
    {
        if (guitarBackground != null && guitarBackground.isActiveAndEnabled)
            guitarBackground.Toggle();
    }

    #endregion
    
    #region Camera

    void OnAimChanged(InputAction.CallbackContext context)
    {
        postShotAimClock.ResetTimer();
        SetAiming(context.performed || mouseAiming || mouseShotRequested);
    }

    void SetAiming(bool aiming)
    {
        bool changed = aiming != isAiming;
        if (changed)
        {
            if (aiming && rotateSystem != null)
                rotateSystem.Direction = Vector3.zero;

            if (orbitCameraPlayer != null && shoulderCameraPlayer != null)
            {
                if (aiming)
                    shoulderCameraPlayer.SetLookOrientation(orbitCameraPlayer.Orientation);
                else
                    orbitCameraPlayer.SetLookOrientation(shoulderCameraPlayer.LookOrientation);
            }
        }

        isAiming = aiming;
        if (!isAiming && shootAttackPlayer != null)
            shootAttackPlayer.CancelAttack();
        if (shoulderCameraPlayer != null)
            shoulderCameraPlayer.SetAiming(isAiming);
        if (orbitCameraPlayer != null)
            orbitCameraPlayer.Direction = Vector2.zero;
        if (shoulderCamera != null)
            shoulderCamera.SetActive(isAiming);
        if (changed)
            OnAimingChanged?.Invoke(isAiming);
    }

    void UpdateCameraLook()
    {
        if (lookAxes == null)
            return;

        InputDevice lookDevice = lookAxes.activeControl?.device;
        bool fromGamepad = lookDevice is Gamepad;
        Vector2 input = lookAxes.ReadValue<Vector2>();
        if (fromGamepad)
            input *= GamepadReferenceFrameRate * Time.deltaTime;

        if (isAiming)
        {
            if (shoulderCameraPlayer != null)
                shoulderCameraPlayer.SetLook(input, fromGamepad);
        }
        else if (orbitCameraPlayer != null)
        {
            orbitCameraPlayer.UseGamepad = fromGamepad;
            orbitCameraPlayer.Direction = input;
        }
    }

    #endregion

    #region Movement

    void OnMoveCanceled(InputAction.CallbackContext context)
    {
        if (!IsDashing)
            ClearMovement();
    }

    void Update()
    {
        if (interactionPlayer != null && interactInput?.WasPressedThisFrame() == true)
            interactionPlayer.Interact();

        bool dashHeld = dashInput?.IsPressed() == true;
        if (!dashHeld || dashInput.WasReleasedThisFrame())
            sprintFromDash = false;

        if (IsDashing)
        {
            UpdateCameraLook();
            UpdateMovement(dashPlayer.Direction);
            return;
        }

        UpdateCompetenceOne();
        UpdateNormalAttack();
        UpdateShoot();
        UpdateCameraLook();
        if (IsAttacking)
        {
            LockMovement();
            return;
        }

        if (moveInput == null)
            return;

        Vector3 direction = GetMovementDirection(moveInput.ReadValue<Vector2>());
        if (!isAiming && dashPlayer != null && dashInput?.WasPressedThisFrame() == true)
        {
            Transform facing = rotateSystem != null && rotateSystem.Target != null ? rotateSystem.Target : transform;
            Vector3 dashDirection = direction != Vector3.zero ? direction : Vector3.ProjectOnPlane(facing.forward, Vector3.up);
            if (dashPlayer.TryStartDash(dashDirection))
                sprintFromDash = dashHeld;
        }

        UpdateMovement(IsDashing ? dashPlayer.Direction : direction);
    }

    void LockMovement()
    {
        if (IsDashing)
            return;

        ClearMovement();
        if (rotateSystem != null)
            rotateSystem.Direction = Vector3.zero;
    }

    void ClearMovement()
    {
        if (movementPlayer == null) return;
        
        movementPlayer.Direction = Vector3.zero;
        movementPlayer.SetSpeed(movementPlayer.WalkSpeed, false);
    }

    Vector3 GetMovementDirection(Vector2 input)
    {
        if (input == Vector2.zero)
            return Vector3.zero;

        Quaternion orientation = orbitCameraPlayer != null ? orbitCameraPlayer.Orientation : Quaternion.identity;
        if (isAiming && shoulderCameraPlayer != null)
            orientation = shoulderCameraPlayer.Orientation;
        Vector3 forward = Vector3.ProjectOnPlane(orientation * Vector3.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.Cross(orientation * Vector3.right, Vector3.up);

        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        return right * input.x + forward * input.y;
    }

    void UpdateMovement(Vector3 worldDirection)
    {
        if (worldDirection == Vector3.zero)
        {
            ClearMovement();
            return;
        }

        bool sprinting = !isAiming && (sprintFromDash || sprintInput?.IsPressed() == true);
        float locomotionSpeed = sprinting ? movementPlayer.RunSpeed : movementPlayer.WalkSpeed;
        float dashSpeed = IsDashing ? dashPlayer.CurrentSpeed : 0f;
        movementPlayer.SetSpeed(Mathf.Max(locomotionSpeed, dashSpeed), sprinting);
        movementPlayer.Direction = worldDirection;

        if (rotateSystem != null && (!isAiming || IsDashing))
            rotateSystem.Direction = worldDirection;
    }

    #endregion
    
    #region Attack

    void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (IsDashing)
            return;

        if (context.interaction is HoldInteraction)
        {
            if (!IsAttacking)
            {
                postShotAimClock.ResetTimer();
                mouseAiming = true;
                SetAiming(true);
            }
            return;
        }

        attackRequested = true;
    }

    void OnAttackCanceled(InputAction.CallbackContext context)
    {
        if (!mouseAiming || !(context.interaction is HoldInteraction))
            return;

        if (context.control is ButtonControl button && button.device.added && !button.isPressed)
        {
            mouseAiming = false;
            mouseShotRequested = true;
        }
        else
            CancelMouseAim();
    }

    void CancelMouseAim()
    {
        if (!mouseAiming && !mouseShotRequested && !postShotAimClock.IsRunning)
            return;

        mouseAiming = false;
        mouseShotRequested = false;
        postShotAimClock.ResetTimer();
        SetAiming(aimInput?.IsPressed() == true);
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            sprintFromDash = false;
            attackRequested = false;
            CancelMouseAim();
        }
    }

    void UpdateNormalAttack()
    {
        if (!attackRequested)
            return;

        attackRequested = false;
        if (postShotAimClock.IsRunning)
            CancelMouseAim();
        if (!isAiming && aimInput?.IsPressed() != true && normalAttackPlayer != null &&
            (competenceOnePlayer == null || !competenceOnePlayer.IsAttacking))
            normalAttackPlayer.TryAttack();
    }

    void OnNormalAttackChanged(int _) => LockMovement();

    void OnShootPerformed(InputAction.CallbackContext _)
    {
        if (!IsDashing)
            shootRequested = true;
    }

    void OnCompetenceOnePerformed(InputAction.CallbackContext _)
    {
        if (!IsDashing)
            competenceOneRequested = true;
    }

    void UpdateCompetenceOne()
    {
        if (!competenceOneRequested)
            return;

        competenceOneRequested = false;
        if (IsAttacking || competenceOnePlayer == null || !competenceOnePlayer.CanAttack)
            return;

        if (shootAttackPlayer != null)
            shootAttackPlayer.CancelAttack();
        if (competenceOnePlayer.TryAttack())
        {
            CancelMouseAim();
            LockMovement();
        }
    }

    void UpdateShoot()
    {
        if (mouseShotRequested)
        {
            mouseShotRequested = false;
            bool fired = attackInput.enabled && TryShoot(true);
            if (fired && !HasAimInput && postShotAimClock.Duration > 0d)
                postShotAimClock.StartTimer();
            SetAiming(HasAimInput || postShotAimClock.IsRunning);
        }
        else if (postShotAimClock.IsRunning)
        {
            if (!attackInput.enabled)
                CancelMouseAim();
            else if (postShotAimClock.UpdateTimer(out _))
                SetAiming(HasAimInput);
        }
        else if (shootRequested || shootInput?.IsPressed() == true)
            TryShoot(false);

        shootRequested = false;
    }

    bool TryShoot(bool fireImmediately)
    {
        if (!isAiming || IsAttacking || shootAttackPlayer == null || !shootAttackPlayer.StartAttack(fireImmediately))
            return false;

        OnShoot?.Invoke();
        return true;
    }

    #endregion
}
