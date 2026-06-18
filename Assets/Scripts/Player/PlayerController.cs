using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStateManager))]
[RequireComponent(typeof(PlayerAnimator))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset inputActions;

    [Header("Movement")]
    private float walkSpeed = 5.5f;
    private float runSpeed = 12.5f;
    private float jumpForce = 4.5f;
    private float dashForce = 7.5f;
    private float dashDuration = 0.2f;
    private float rotationSpeed = 7.5f;
    private float movementSmoothTime = 0.25f;
    private float doubleTapTime = 0.3f;

    [Header("Snapping offsets")]
    private Vector3 hangOffset = new Vector3(0f, -2.15f, -0.1f);

    [Header("Références")]
    private PlayerStateManager _stateManager;
    private PlayerAnimator _playerAnimator;
    private Rigidbody _rb;
    private Transform _mainCamera;
    private Collider _currentLedgeCollider;
    private bool _wasLedgeAvailable;

    [Header("États locaux")]
    private bool _isJumping;
    private bool _isRunning;
    private bool _isDashing;
    private bool _canDash = true;

    [Header("Données de mouvement")]
    private float _jumpCooldown;
    private float _lastTapTimeX;
    private float _lastTapDirectionX;
    private Vector3 _moveDirVelocity;
    private Vector2 _inputValue;
    
    [Header("Actions")]
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _interactAction; 
    private InputAction _dropAction;     

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _stateManager = GetComponent<PlayerStateManager>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        if (Camera.main != null) _mainCamera = Camera.main.transform;
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnEnable()
    {
        var map = inputActions.FindActionMap("Player", true);
        _moveAction = map.FindAction("Move", true);
        _jumpAction = map.FindAction("Jump", true);
        _sprintAction = map.FindAction("Sprint", true);
        _interactAction = map.FindAction("Interact", true);
        _dropAction = map.FindAction("Drop", true);

        _moveAction.performed += OnMove;
        _moveAction.canceled += OnMoveCanceled;
        _jumpAction.performed += OnJump;
        map.Enable();
    }

    void OnDisable()
    {
        _moveAction.performed -= OnMove;
        _moveAction.canceled -= OnMoveCanceled;
        _jumpAction.performed -= OnJump;
        inputActions.FindActionMap("Player").Disable();
    }

    void Update()
    {
        _isRunning = _sprintAction.IsPressed();

        if (_stateManager.CurrentState == PlayerState.Mantling) return;

        if (_stateManager.CurrentState == PlayerState.Hanging)
        {
            HandleHangingState();
            return; 
        }

        if (!_isDashing)
        {
            MoveAndRotate();
            UpdateAnimator();
            CheckLanding();
            CheckLedgeAvailability();
        }
    }

    #region Movement Logic

    private void MoveAndRotate()
    {
        Vector3 camForward = Vector3.ProjectOnPlane(_mainCamera.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(_mainCamera.right, Vector3.up).normalized;
        Vector3 targetMoveDir = (camForward * _inputValue.y + camRight * _inputValue.x).normalized;

        float targetSpeed = _inputValue.sqrMagnitude > 0.01f ? (_isRunning ? runSpeed : walkSpeed) : 0f;
        Vector3 targetVelocity = targetMoveDir * targetSpeed;

        Vector3 currentHorizVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        Vector3 smoothedVelocity = Vector3.SmoothDamp(currentHorizVel, targetVelocity, ref _moveDirVelocity, movementSmoothTime);
        _rb.linearVelocity = new Vector3(smoothedVelocity.x, _rb.linearVelocity.y, smoothedVelocity.z);

        if (targetMoveDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetMoveDir), rotationSpeed * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        if (_isDashing) return;
        float targetZ = _inputValue.sqrMagnitude > 0.01f ? (_isRunning ? 1f : 0.75f) : 0f;
        _playerAnimator.UpdateMoveAnimation(targetZ);
    }

    #endregion

    #region Ledge Grab & Mantle

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ledge"))
        {
            _currentLedgeCollider = other;
        }

        if (other.CompareTag("SpawnPoint") && SceneManager.GetActiveScene().name == "Init")
    {
        _stateManager.ChangeState(PlayerState.Tutorial);
    }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ledge") && _currentLedgeCollider == other)
        {
            _currentLedgeCollider = null;
            if (_wasLedgeAvailable)
            {
                _stateManager.SetLedgeAvailable(false);
                _wasLedgeAvailable = false;
            }
        }

        if (other.CompareTag("SpawnPoint"))
        {
            _stateManager.ChangeState(PlayerState.Default);
        }
    }

    private void StartHanging(Transform ledgeTransform)
    {
        _stateManager.ChangeState(PlayerState.Hanging);
        _isJumping = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true; 

        _playerAnimator.SetJumping(false); 
        _playerAnimator.SetHanging(true);

        transform.rotation = ledgeTransform.rotation;

        Vector3 snapPosition = ledgeTransform.position 
                             + (ledgeTransform.up * hangOffset.y) 
                             + (ledgeTransform.forward * hangOffset.z) 
                             + (ledgeTransform.right * hangOffset.x);
                             
        transform.position = snapPosition;
    }

    private void HandleHangingState()
    {
        if (_jumpAction.triggered)
        {
            StartCoroutine(MantleRoutine());
            return;
        }

        bool isPressingBackwards = _inputValue.y < -0.5f;
        bool isPressingDropButton = _dropAction != null && _dropAction.triggered;

        if (isPressingBackwards || isPressingDropButton)
        {
            StopHanging();
        }
    }

    private IEnumerator MantleRoutine()
    {
        _stateManager.ChangeState(PlayerState.Mantling);
        
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider != null) playerCollider.enabled = false;

        transform.position -= transform.forward * 0.03f; 

        _playerAnimator.SetRootMotion(true);
        _playerAnimator.TriggerMantle();
        
        float duration = 3.4f; 
        yield return new WaitForSeconds(duration);
        
        _playerAnimator.SetRootMotion(false);

        transform.position += transform.forward * 0.25f;
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1f))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y + 0.01f, transform.position.z);
        }

        if (playerCollider != null) playerCollider.enabled = true;
        
        StopHanging();
        _rb.linearVelocity = Vector3.zero;
    }

    public void StopHanging()
    {
        _stateManager.ChangeState(PlayerState.Default);
        _stateManager.SetLedgeAvailable(false);
        _rb.isKinematic = false;
        _playerAnimator.SetHanging(false);
    }

    #endregion

    #region Input & Landing / Dash

    private void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 newValue = ctx.ReadValue<Vector2>();
        if (newValue.x != 0 && _inputValue.x == 0)
        {
            float currentDir = Mathf.Sign(newValue.x);
            if (Time.time - _lastTapTimeX < doubleTapTime && _lastTapDirectionX == currentDir)
            {
                if (_canDash && !_isJumping && !_isDashing && _stateManager.CurrentState == PlayerState.Default)
                    StartCoroutine(DashRoutine(currentDir));
            }
            _lastTapTimeX = Time.time;
            _lastTapDirectionX = currentDir;
        }
        _inputValue = newValue;
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx) => _inputValue = Vector2.zero;

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (_stateManager.CurrentState != PlayerState.Default || _isJumping || _isDashing) return;

        _isJumping = true;
        _jumpCooldown = 0.2f;
        _playerAnimator.SetJumping(true);
        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void CheckLanding()
    {
        if (!_isJumping) return;
        if (_jumpCooldown > 0f) { _jumpCooldown -= Time.deltaTime; return; }
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.5f))        
        {
            _isJumping = false;
            _playerAnimator.SetJumping(false);
        }
    }

    private void CheckLedgeAvailability()
    {
        bool canHang = (_currentLedgeCollider != null) 
                    && (_stateManager.CurrentState == PlayerState.Default) 
                    && _isJumping;

        if (canHang != _wasLedgeAvailable)
        {
            _stateManager.SetLedgeAvailable(canHang);
            _wasLedgeAvailable = canHang;
        }

        if (canHang && _interactAction != null && _interactAction.IsPressed())
        {
            StartHanging(_currentLedgeCollider.transform);
        }
    }

    private IEnumerator DashRoutine(float direction)
    {
        _canDash = false;
        _isDashing = true;
        _playerAnimator.TriggerDash(direction);
        
        Vector3 dashDir = (direction > 0 ? _mainCamera.right : -_mainCamera.right);
        dashDir.y = 0;
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        _rb.AddForce(dashDir * dashForce, ForceMode.Impulse);
        
        yield return new WaitForSeconds(dashDuration);
        _isDashing = false;
        yield return new WaitForSeconds(0.5f);
        _canDash = true;
    }
    #endregion
}