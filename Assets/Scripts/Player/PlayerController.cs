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
        float walkSpeed = 2f;
        float runSpeed  = 5f;
        float backwardSpeed = 1f;
        float jumpForce = 5f;
        float dashForce = 10f;
        float dashDuration = 0.2f;
        float rotationSpeed = 7.5f;
        private bool _canDash = true;
        float movementSmoothTime = 0.5f;
        private Vector3 _currentMoveDir;
        private Vector3 _moveDirVelocity;

        [Header("Double Tap Dash")]
        public float doubleTapTime = 0.3f; 
        private float _lastTapTimeX;
        private float _lastTapDirectionX;

        [Header("Animator")]
        public float animationDampTime = 0.1f;

        private Animator    _animator;
        private Rigidbody   _rb;
        private Transform   _mainCamera;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;

        private Vector2 _inputValue;
        private bool    _isJumping;
        private bool    _isRunning;
        private bool    _isDashing;
        private float   _jumpCooldown; 

        // Nouveaux Hashes pour l'Animator
        private static readonly int HashMoveX     = Animator.StringToHash("MoveX");
        private static readonly int HashMoveZ     = Animator.StringToHash("MoveZ");
        private static readonly int HashIsJumping = Animator.StringToHash("IsJumping");
        private static readonly int HashDashLeft  = Animator.StringToHash("DashLeft");
        private static readonly int HashDashRight = Animator.StringToHash("DashRight");

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _rb       = GetComponent<Rigidbody>();
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
            var map = inputActions.FindActionMap("Player", throwIfNotFound: true);
            _moveAction   = map.FindAction("Move", throwIfNotFound: true);
            _jumpAction   = map.FindAction("Jump", throwIfNotFound: true);
            _sprintAction = map.FindAction("Sprint", throwIfNotFound: true);

            _moveAction.performed += OnMove;
            _moveAction.canceled  += OnMoveCanceled;
            _jumpAction.performed += OnJump;

            map.Enable();
        }

        void OnDisable()
        {
            _moveAction.performed -= OnMove;
            _moveAction.canceled  -= OnMoveCanceled;
            _jumpAction.performed -= OnJump;

            inputActions.FindActionMap("Player").Disable();
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            Vector2 newValue = ctx.ReadValue<Vector2>();

            if (newValue.x != 0 && _inputValue.x == 0)
            {
                float currentDir = Mathf.Sign(newValue.x);
                
                if (Time.time - _lastTapTimeX < doubleTapTime && _lastTapDirectionX == currentDir)
                {
                    if (_canDash && !_isJumping && !_isDashing)
                    {
                        StartCoroutine(DashRoutine(currentDir));
                    }
                }
                
                _lastTapTimeX = Time.time;
                _lastTapDirectionX = currentDir;
            }

            _inputValue = newValue;
        }

        private void OnMoveCanceled(InputAction.CallbackContext ctx) => _inputValue = Vector2.zero;

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (_isJumping || _isDashing) return;
            _isJumping = true;
            _jumpCooldown = 0.2f; 
            _animator.SetBool(HashIsJumping, true);
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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
            Vector3 camRight   = _mainCamera.right;
            camForward.y = 0; camForward.Normalize();
            camRight.y   = 0; camRight.Normalize();

            Vector3 targetMoveDir = (camForward * _inputValue.y + camRight * _inputValue.x).normalized;

            _currentMoveDir = Vector3.SmoothDamp(_currentMoveDir, targetMoveDir, ref _moveDirVelocity, movementSmoothTime);

            if (_inputValue.sqrMagnitude < 0.01f && _currentMoveDir.sqrMagnitude < 0.01f) 
            {
                _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
                return;
            }

            bool isMovingBackward = _inputValue.y < -0.1f;
            float currentSpeed;

            if (isMovingBackward)
            {
                if (camForward != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(camForward);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                currentSpeed = backwardSpeed;
            }
            else
            {
                if (_currentMoveDir != Vector3.zero && _currentMoveDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(_currentMoveDir.normalized);
                    
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                currentSpeed = _isRunning ? runSpeed : walkSpeed;
            }

            float speedMultiplier = Mathf.Clamp01(_currentMoveDir.magnitude);
            Vector3 velocityDir = _currentMoveDir.normalized;
            
            _rb.linearVelocity = new Vector3(velocityDir.x * currentSpeed * speedMultiplier, _rb.linearVelocity.y, velocityDir.z * currentSpeed * speedMultiplier);
        }

        private void UpdateAnimator()
        {
            if (_isDashing) return;

            float targetZ = 0f;

            if (_inputValue.sqrMagnitude > 0.01f)
            {
                if (_inputValue.y < -0.1f)
                {
                    targetZ = -1f;
                }
                else
                {
                    targetZ = _isRunning ? 1f : 0.75f;
                }
            }

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

        if (direction > 0)
            _animator.SetTrigger(HashDashRight);
        else
            _animator.SetTrigger(HashDashLeft);
        
        Vector3 dashDir = (direction > 0) ? _mainCamera.right : -_mainCamera.right;
        dashDir.y = 0;
        
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        _rb.AddForce(dashDir * dashForce, ForceMode.Impulse);
        
        yield return new WaitForFixedUpdate();
        
        float timer = 0f;
        while (timer < dashDuration)
        {
            timer += Time.deltaTime;
            
            Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
            
            if (horizontalVelocity.magnitude < 0.5f)
            {
                _animator.CrossFade("Locomotion", 0.1f);
                break;
            }

            yield return null;
        }
        
        _isDashing = false;
        
        yield return new WaitForSeconds(0.5f);
        _canDash = true;
    }
    }