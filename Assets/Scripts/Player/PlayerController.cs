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
    public float walkSpeed = 2f;
    public float runSpeed  = 5f;
    public float backwardSpeed = 1f;
    public float jumpForce = 5f;
    public float dashForce = 10f;
    public float dashDuration = 0.2f;
    public float rotationSpeed = 20f;
    private bool _canDash = true;

    [Header("Animator")]
    public float animationDampTime = 0.1f;

    private Animator    _animator;
    private Rigidbody   _rb;
    private Transform   _mainCamera;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _dashAction;

    private Vector2 _inputValue;
    private bool    _isJumping;
    private bool    _isRunning;
    private bool    _isDashing;
    private float   _jumpCooldown; 

    private static readonly int HashMoveZ     = Animator.StringToHash("MoveZ");
    private static readonly int HashIsJumping = Animator.StringToHash("IsJumping");
    private static readonly int HashDash      = Animator.StringToHash("Dash");

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb       = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        if (Camera.main != null) _mainCamera = Camera.main.transform;
    }

    void OnEnable()
    {
        var map = inputActions.FindActionMap("Player", throwIfNotFound: true);
        _moveAction   = map.FindAction("Move", throwIfNotFound: true);
        _jumpAction   = map.FindAction("Jump", throwIfNotFound: true);
        _sprintAction = map.FindAction("Sprint", throwIfNotFound: true);
        _dashAction   = map.FindAction("Dash", throwIfNotFound: true);

        _moveAction.performed += OnMove;
        _moveAction.canceled  += OnMoveCanceled;
        _jumpAction.performed += OnJump;
        _dashAction.performed += OnDash;

        map.Enable();
    }

    void OnDisable()
    {
        _moveAction.performed -= OnMove;
        _moveAction.canceled  -= OnMoveCanceled;
        _jumpAction.performed -= OnJump;
        _dashAction.performed -= OnDash;

        inputActions.FindActionMap("Player").Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx) => _inputValue = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => _inputValue = Vector2.zero;

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (_isJumping || _isDashing) return;
        _isJumping = true;
        _jumpCooldown = 0.2f; 
        _animator.SetBool(HashIsJumping, true);
        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (!_canDash || _isJumping || _isDashing) return;
        
        float dir = (_inputValue.x != 0) ? Mathf.Sign(_inputValue.x) : 1f;
        StartCoroutine(DashRoutine(dir));
    }

    void Update()
    {
        _isRunning = _sprintAction.IsPressed();
        
        if(!_isDashing) MoveAndRotate();
        UpdateAnimator();
        CheckLanding();
    }

    private void MoveAndRotate()
    {
        Vector3 camForward = _mainCamera.forward;
        camForward.y = 0; camForward.Normalize();

        if (camForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Mathf.Abs(_inputValue.y) < 0.1f) 
        {
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
            return;
        }

        float currentSpeed = (_inputValue.y < -0.1f) ? backwardSpeed : (_isRunning && _inputValue.y > 0.1f) ? runSpeed : walkSpeed;
        
        Vector3 moveDir = camForward * _inputValue.y; 
        _rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, _rb.linearVelocity.y, moveDir.z * currentSpeed);
    }

    private void UpdateAnimator()
    {
        if (_isDashing) return;

        float targetZ = (_inputValue.y > 0.01f) ? (_isRunning ? 1f : 0.75f) : (_inputValue.y < -0.01f) ? -1f : 0f;
        
        _animator.SetFloat(HashMoveZ, targetZ, animationDampTime, Time.deltaTime);
    }

    private void CheckLanding()
    {
        if (!_isJumping) return;
        if (_jumpCooldown > 0f) { _jumpCooldown -= Time.deltaTime; return; }
        
        bool grounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.35f);
        if (grounded && _rb.linearVelocity.y <= 0.1f)
        {
            _isJumping = false;
            _animator.SetBool(HashIsJumping, false);
        }
    }

    private IEnumerator DashRoutine(float direction)
    {
        _canDash = false;
        _isDashing = true;
        _animator.SetTrigger(HashDash);
        
        Vector3 dashDir = (direction > 0) ? _mainCamera.right : -_mainCamera.right;
        dashDir.y = 0;
        
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        _rb.AddForce(dashDir * dashForce, ForceMode.Impulse);
        
        yield return new WaitForSeconds(dashDuration);
        _isDashing = false;
        
        yield return new WaitForSeconds(0.5f);
        _canDash = true;
    }
}