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
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpForce = 5f;
    public float dashForce = 12.5f;
    public float dashDuration = 0.2f;
    public float rotationSpeed = 15f;
    public float movementSmoothTime = 0.5f;
    public float doubleTapTime = 0.3f;

    [Header("Ledge Grab")]
    public LayerMask ledgeLayer; 
    public float ledgeForwardCheck = 0.5f; 
    public float ledgeDownCheck = 0.5f;

    [Header("Animator")]
    public float animationDampTime = 0.1f;

    // États
    private bool _isHanging;
    private bool _isJumping;
    private bool _isRunning;
    private bool _isDashing;
    private bool _canDash = true;

    // Données de mouvement
    private float _jumpCooldown;
    private float _lastTapTimeX;
    private float _lastTapDirectionX;
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

        if (_isHanging)
        {
            if (_jumpAction.triggered) 
            {
                StartCoroutine(MantleRoutine());
            }
            else if (_inputValue.y < -0.5f) 
            {
                StopHanging();
            }
            return; 
        }

        if (!_isDashing)
        {
            MoveAndRotate();
            UpdateAnimator();
            CheckLanding();
            if (_isJumping) CheckForLedge();
        }
    }

    #region Movement & Ledge Logic

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

    private void CheckForLedge()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Debug.DrawRay(origin, transform.forward * ledgeForwardCheck, Color.red);
        
        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, ledgeForwardCheck, ledgeLayer))
        {
            Vector3 topOrigin = hit.point + (transform.forward * 0.1f) + (Vector3.up * 0.5f);
            Debug.DrawRay(topOrigin, Vector3.down * 0.5f, Color.green);
            
            if (!Physics.Raycast(topOrigin, Vector3.down, 0.5f, ledgeLayer))
            {
                StartHanging(hit);
            }
        }
    }

    private void StartHanging(RaycastHit hit)
    {
        _isHanging = true;
        _isJumping = false;
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;
        
        transform.forward = -hit.normal; 
        transform.position = hit.point + (hit.normal * 0.4f) - (Vector3.up * 1.5f);
        
        _animator.SetBool(HashIsHanging, true);
        _animator.Play("Mantle", 0, 0.15f); 
        _animator.speed = 0f; 
        
        _rb.isKinematic = true;
    }

    private void StopHanging()
    {
        _isHanging = false;
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _animator.SetBool(HashIsHanging, false);
        _animator.CrossFade("Locomotion", 0.1f);
        _animator.speed = 1f;
    }

    private IEnumerator MantleRoutine()
    {
        _animator.speed = 1f;
        
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + (transform.up * 1.5f) + (transform.forward * 0.5f);
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = endPos;

       _isHanging = false;
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _animator.SetBool(HashIsHanging, false);
        _animator.CrossFade("Locomotion", 0.1f);
        _animator.speed = 1f;
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
                if (_canDash && !_isJumping && !_isDashing && !_isHanging)
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
        if (_isHanging) return; // Empêche le saut si on est suspendu (ou rajoute une logique de "lâcher")
        if (_isJumping || _isDashing) return;

        _isJumping = true;
        _jumpCooldown = 0.2f;
        _animator.SetBool(HashIsJumping, true);
        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void CheckLanding()
    {
        if (!_isJumping) return;
        if (_jumpCooldown > 0f) { _jumpCooldown -= Time.deltaTime; return; }
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.35f))
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