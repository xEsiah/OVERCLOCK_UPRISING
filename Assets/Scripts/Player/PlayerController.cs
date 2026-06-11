using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
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
    private float movementSmoothTime = 0.5f;
    private float doubleTapTime = 0.3f;

    [Header("Animator")]
    public float animationDampTime = 0.1f;
    
    [Header("Snapping offsets")]
    private Vector3 hangOffset = new Vector3(0f, -2.25f, -0.05f);

    // États
    private bool _isHanging;
    private bool _isMantling;
    private bool _isJumping;
    private bool _isRunning;
    private bool _isDashing;
    private bool _canDash = true;

    // Données de mouvement
    private float _jumpCooldown;
    private float _lastTapTimeX;
    private float _lastTapDirectionX;
    private float _hangStartTime;
    private Vector3 _moveDirVelocity;
    private Vector2 _inputValue;
    private Animator _animator;
    private Rigidbody _rb;
    private Transform _mainCamera;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;

    // Hashes
    private static readonly int HashMoveZ = Animator.StringToHash("MoveZ");
    private static readonly int HashIsJumping = Animator.StringToHash("IsJumping");
    private static readonly int HashDashLeft = Animator.StringToHash("DashLeft");
    private static readonly int HashDashRight = Animator.StringToHash("DashRight");
    private static readonly int HashIsHanging = Animator.StringToHash("IsHanging");
    private static readonly int HashMantle = Animator.StringToHash("Mantle");

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
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

        if (_isMantling) return;

        if (_isHanging)
        {
            HandleHangingState();
            return; 
        }

        if (!_isDashing)
        {
            MoveAndRotate();
            UpdateAnimator();
            CheckLanding();
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

    #endregion

    #region Ledge Grab (Trigger Based)

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ledge") && !_isHanging && _isJumping)
        {
            StartHanging(other.transform);
        }
    }

    private void StartHanging(Transform ledgeTransform)
    {
        _isHanging = true;
        _isJumping = false;
        
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true; 

        _animator.SetBool(HashIsJumping, false); 
        _animator.SetBool(HashIsHanging, true);

        transform.rotation = ledgeTransform.rotation;

        Vector3 snapPosition = ledgeTransform.position 
                             + (ledgeTransform.up * hangOffset.y) 
                             + (ledgeTransform.forward * hangOffset.z) 
                             + (ledgeTransform.right * hangOffset.x);
                             
        transform.position = snapPosition;
    }

    private void HandleHangingState()
    {
        if (Time.time - _hangStartTime < 0.25f) return;
        if (_jumpAction.triggered)
        {
            StartCoroutine(MantleRoutine());
        }
    }

    private IEnumerator MantleRoutine()
    {
        _isMantling = true; 
        
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider != null) playerCollider.enabled = false;

        _animator.applyRootMotion = true;
        _animator.SetTrigger(HashMantle);
        
        float duration = 3.4f; 
        yield return new WaitForSeconds(duration);
        
        _animator.applyRootMotion = false;

        // 1. On avance légèrement sur la plateforme
        transform.position += transform.forward * 0.25f;
        
        // 2. LE "FLOOR SNAP" : On cherche le sol pour s'y poser parfaitement
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1f))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y + 0.01f, transform.position.z);
        }

        if (playerCollider != null) playerCollider.enabled = true;
        
        // 3. On appelle StopHanging EN PREMIER (qui va remettre isKinematic à false)
        StopHanging();

        // 4. MAINTENANT on peut remettre la vitesse à zéro sans fâcher Unity !
        _rb.linearVelocity = Vector3.zero;
        
        _isMantling = false; 
    }

public void StopHanging()
    {
        _isHanging = false;
        _rb.isKinematic = false;
        _animator.SetBool(HashIsHanging, false);
    }

    #endregion

    #region Input & Landing

    private void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 newValue = ctx.ReadValue<Vector2>();
        if (newValue.x != 0 && _inputValue.x == 0)
        {
            float currentDir = Mathf.Sign(newValue.x);
            if (Time.time - _lastTapTimeX < doubleTapTime && _lastTapDirectionX == currentDir)
            {
                if (_canDash && !_isJumping && !_isDashing && !_isHanging && !_isMantling)
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
        if (_isHanging || _isJumping || _isDashing || _isMantling) return;

        _isJumping = true;
        _jumpCooldown = 0.2f;
        _animator.SetBool(HashIsJumping, true);
        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void CheckLanding()
    {
        if (!_isJumping) return;
        if (_jumpCooldown > 0f) { _jumpCooldown -= Time.deltaTime; return; }
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.5f))        
        {
            _isJumping = false;
            _animator.SetBool(HashIsJumping, false);
        }
    }

    private void UpdateAnimator()
    {
        if (_isDashing) return;
        float targetZ = _inputValue.sqrMagnitude > 0.01f ? (_isRunning ? 1f : 0.75f) : 0f;
        _animator.SetFloat(HashMoveZ, targetZ, animationDampTime, Time.deltaTime);
    }

    private IEnumerator DashRoutine(float direction)
    {
        _canDash = false;
        _isDashing = true;
        _animator.SetTrigger(direction > 0 ? HashDashRight : HashDashLeft);
        
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