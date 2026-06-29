using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStateManager))]
[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(Attack))]
public class PlayerController : MonoBehaviour
{
    #region Variables
    [Header("Input")]
    public InputActionAsset inputActions;

    [Header("Movement")]
    private float walkSpeed = 5.5f;
    public float runSpeed = 12.5f;
    public float jumpForce = 4.5f;
    private float dodgeDashForce = 2f;
    private float dodgeDashDuration = 1f;
    private float rotationSpeed = 7.5f;
    private float movementSmoothTime = 0.25f;
    private float doubleTapTime = 0.3f;

    [Header("Snapping offsets")]
    private Vector3 hangOffset = new Vector3(0f, -2.15f, -0.1f);

    [Header("Références")]
    private PlayerStateManager _stateManager;
    private PlayerAnimator _playerAnimator;
    private Rigidbody _rb;
    private Attack _attack;
    private Transform _mainCamera;
    private Collider _currentLedgeCollider;
    private bool _wasLedgeAvailable;

    [Header("États locaux")]
    private bool _isJumping;
    private bool _isRunning;
    private bool _isDodging;
    private bool _canDodge = true;
    public bool IsDodging => _isDodging;

    [Header("Données de mouvement")]
    private float _jumpCooldown;
    private float _lastTapTimeX;
    private float _lastTapDirectionX;
    private Vector3 _moveDirVelocity;
    private Vector2 _inputValue;
    private float _knockbackTimer;
    
    [Header("Actions")]
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _interactAction; 
    private InputAction _dropAction;     
    private InputAction _attackAction;
    #endregion

    #region Unity Lifecycle
    /// <summary>
    /// Initialise les références aux composants attachés au joueur.
    /// </summary>
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _stateManager = GetComponent<PlayerStateManager>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _attack = GetComponent<Attack>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        if (Camera.main != null) _mainCamera = Camera.main.transform;
    }

    /// <summary>
    /// Configuration initiale de la scène (verrouillage du curseur).
    /// </summary>
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Abonne les événements du système d'entrées (Input System).
    /// </summary>
    void OnEnable()
    {
        var map = inputActions.FindActionMap("Player", true);
        _moveAction = map.FindAction("Move", true);
        _jumpAction = map.FindAction("Jump", true);
        _sprintAction = map.FindAction("Sprint", true);
        _interactAction = map.FindAction("Interact", true);
        _dropAction = map.FindAction("Drop", true);
        _attackAction = map.FindAction("Attack", true);

        _moveAction.performed += OnMove;
        _moveAction.canceled += OnMoveCanceled;
        _jumpAction.performed += OnJump;
        _attackAction.performed += OnAttack;
        map.Enable();
    }

    /// <summary>
    /// Désabonne les événements pour éviter les fuites de mémoire.
    /// </summary>
    void OnDisable()
    {
        _moveAction.performed -= OnMove;
        _moveAction.canceled -= OnMoveCanceled;
        _jumpAction.performed -= OnJump;
        _attackAction.performed -= OnAttack;
        inputActions.FindActionMap("Player").Disable();
    }

    /// <summary>
    /// Boucle principale : gère les timers, vérifie les états et applique les mouvements.
    /// </summary>
    void Update()
    {
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.deltaTime;
            return;
        }

        _isRunning = _sprintAction.IsPressed();

        if (_stateManager.CurrentState == PlayerState.Mantling) return;

        if (_stateManager.CurrentState == PlayerState.Hanging)
        {
            HandleHangingState();
            return; 
        }

        if (!_isDodging)
        {
            MoveAndRotate();
            UpdateAnimator();
            CheckAirborne();
            CheckLedgeAvailability();
        }
    }

    public void ResetStates()
    {
        _inputValue = Vector2.zero;
        _moveDirVelocity = Vector3.zero;
        _isJumping = false;
        _isRunning = false;
        _isDodging = false;
        _canDodge = true;
        _knockbackTimer = 0f;
        _jumpCooldown = 0f;

        _stateManager.ChangeState(PlayerState.Default);
        
        _playerAnimator.UpdateMoveAnimation(0f);
        _playerAnimator.SetJumping(false);
        _playerAnimator.SetFalling(false);
        _playerAnimator.SetHanging(false);
    }
    #endregion

    #region Input Handlers
    /// <summary>
    /// Lit la direction d'entrée et détecte le double-tap pour déclencher l'esquive.
    /// </summary>
    private void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 newValue = ctx.ReadValue<Vector2>();
        if (newValue.x != 0 && _inputValue.x == 0)
        {
            float currentDir = Mathf.Sign(newValue.x);
            if (Time.time - _lastTapTimeX < doubleTapTime && _lastTapDirectionX == currentDir)
            {
                if (_canDodge && !_isJumping && !_isDodging && _stateManager.CurrentState == PlayerState.Default)
                    StartCoroutine(DodgeRoutine(currentDir));
            }
            _lastTapTimeX = Time.time;
            _lastTapDirectionX = currentDir;
        }
        _inputValue = newValue;
    }

    /// <summary>
    /// Réinitialise la valeur de mouvement quand les touches sont relâchées.
    /// </summary>
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => _inputValue = Vector2.zero;

    /// <summary>
    /// Applique une force verticale pour faire sauter le joueur.
    /// </summary>
    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (_stateManager.CurrentState != PlayerState.Default || _isJumping || _isDodging) return;

        _isJumping = true;
        AudioManager.instance.PlayJumpSound();
        _jumpCooldown = 0.2f;
        _playerAnimator.SetJumping(true);
        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    
        StartCoroutine(JumpTimeoutRoutine());
    }

    private IEnumerator JumpTimeoutRoutine()
    {
        yield return new WaitForSeconds(1f);
        if (_isJumping)
        {
            _isJumping = false;
            _playerAnimator.SetJumping(false);
        }
    }

    /// <summary>
    /// Demande l'exécution de l'attaque via le composant Attack.
    /// </summary>
    private void OnAttack(InputAction.CallbackContext ctx)
    {
        bool canAttackInCurrentState = _stateManager.CurrentState == PlayerState.Default || 
                                       _stateManager.CurrentState == PlayerState.Falling || 
                                       _isJumping;

        if (canAttackInCurrentState && !_isDodging && _attack.CanAttack())
        {
            _attack.PerformAttack();
        }
    }
    #endregion

    #region Core Movement & Physics
    /// <summary>
    /// Calcule la direction par rapport à la caméra et applique la vélocité et la rotation.
    /// </summary>
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

    /// <summary>
    /// Projette un SphereCast vers le sol pour gérer l'état de chute et l'atterrissage.
    /// </summary>
    private void CheckAirborne()
    {
        if (_jumpCooldown > 0f) 
        { 
            _jumpCooldown -= Time.deltaTime; 
            return; 
        }
        
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.5f);

        if (!isGrounded)
        {
            if (!_isJumping && _stateManager.CurrentState != PlayerState.Falling)
            {
                _stateManager.ChangeState(PlayerState.Falling);
                _playerAnimator.SetFalling(true);
            }
        }
        else
        {
            if (_isJumping)
            {
                _isJumping = false;
                _playerAnimator.SetJumping(false);
            }

            if (_stateManager.CurrentState == PlayerState.Falling)
            {
                _stateManager.ChangeState(PlayerState.Default);
                _playerAnimator.SetFalling(false);
            }
        }
    }

    /// <summary>
    /// Coroutine gérant l'impulsion physique et la durée de l'esquive.
    /// </summary>
    private IEnumerator DodgeRoutine(float direction)
    {
        _canDodge = false;
        _isDodging = true;
        _playerAnimator.TriggerDodge(direction);
        AudioManager.instance.PlayDashSound();
        
        GameObject activeDodgeParticles = ParticleManager.instance.SpawnParticle(ParticleManager.instance.DodgeParticles, transform.position, Quaternion.identity, transform, 0f);

        Vector3 dodgeDir = (direction > 0 ? _mainCamera.right : -_mainCamera.right);
        dodgeDir.y = 0;
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        _rb.AddForce(dodgeDir * dodgeDashForce, ForceMode.Impulse);
        
        yield return new WaitForSeconds(dodgeDashDuration);

        if (activeDodgeParticles != null) Destroy(activeDodgeParticles);

        _rb.linearVelocity = Vector3.zero;
        _isDodging = false;
        _canDodge = true;
    }

    /// <summary>
    /// Interrompt le contrôle du joueur et applique une force de recul.
    /// </summary>
    public void TakeKnockback(Vector3 direction, float force)
    {
        _knockbackTimer = 0.3f;
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        _rb.AddForce(direction * force, ForceMode.VelocityChange);

        _stateManager.ChangeState(PlayerState.Falling);
        _playerAnimator.SetFalling(true);
    }
    #endregion

    #region Ledge & Mantling
    /// <summary>
    /// Vérifie si le joueur remplit les conditions pour s'accrocher au rebord détecté.
    /// </summary>
    private void CheckLedgeAvailability()
    {
        bool canHang = (_currentLedgeCollider != null) 
                    && (_stateManager.CurrentState == PlayerState.Default || _stateManager.CurrentState == PlayerState.Falling);

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

    /// <summary>
    /// Positionne le joueur contre le rebord et fige la physique.
    /// </summary>
    private void StartHanging(Transform ledgeTransform)
    {
        _stateManager.ChangeState(PlayerState.Hanging);
        _isJumping = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true; 

        _playerAnimator.SetJumping(false); 
        _playerAnimator.SetFalling(false);
        _playerAnimator.SetHanging(true);

        transform.rotation = ledgeTransform.rotation;

        Vector3 snapPosition = ledgeTransform.position 
                             + (ledgeTransform.up * hangOffset.y) 
                             + (ledgeTransform.forward * hangOffset.z) 
                             + (ledgeTransform.right * hangOffset.x);
                             
        transform.position = snapPosition;
    }

    /// <summary>
    /// Écoute les entrées du joueur (monter ou lâcher) lorsqu'il est suspendu.
    /// </summary>
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

    /// <summary>
    /// Gère la transition d'animation (Root Motion) pour grimper sur le rebord.
    /// </summary>
    private IEnumerator MantleRoutine()
    {
        _stateManager.ChangeState(PlayerState.Mantling);
        
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider != null) playerCollider.enabled = false;

        transform.position -= transform.forward * 0.03f; 

        _playerAnimator.SetRootMotion(true);
        AudioManager.instance.PlayMantleSound();
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

    /// <summary>
    /// Relâche le rebord, réactive la gravité et repasse à l'état par défaut.
    /// </summary>
    public void StopHanging()
    {
        _stateManager.ChangeState(PlayerState.Default);
        _stateManager.SetLedgeAvailable(false);
        _rb.isKinematic = false;
        _playerAnimator.SetHanging(false);
    }
    #endregion

    #region Triggers & Environment
    /// <summary>
    /// Détecte l'entrée dans les zones interactives (rebords, spawn).
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ledge"))
        {
            _currentLedgeCollider = other;
        }

        if (other.CompareTag("SpawnPoint") && SceneManager.GetActiveScene().name == "Init")
        {
            _stateManager.ChangeState(PlayerState.AtSpawn);
        }
    }

    /// <summary>
    /// Gère la sortie des zones interactives et nettoie les références.
    /// </summary>
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
    #endregion

    #region Animation
    /// <summary>
    /// Transmet la vitesse cible à l'Animator pour gérer les transitions marche/course.
    /// </summary>
    private void UpdateAnimator()
    {
        if (_isDodging) return;
        float targetZ = _inputValue.sqrMagnitude > 0.01f ? (_isRunning ? 1f : 0.75f) : 0f;
        _playerAnimator.UpdateMoveAnimation(targetZ);
    }
    #endregion
}