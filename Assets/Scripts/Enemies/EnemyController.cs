using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Attack))]
public class EnemyController : MonoBehaviour
{
    private Animator _animator;
    private Rigidbody _rb;
    private Attack _attack;
    private Transform _player;

    private float _knockbackTimer;
    public float detectionRange = 3f;
    
    private static readonly int HashIsFalling = Animator.StringToHash("IsFalling");

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        _attack = GetComponent<Attack>();
    }

    void Start()
    {
        if (ParticleManager.instance != null)
        {
            ParticleManager.instance.SpawnParticle(ParticleManager.instance.EnemyIntegrationParticles, transform.position, transform.rotation);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
    }

    void Update()
    {
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.deltaTime;
            return;
        }

        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.5f);
        _animator.SetBool(HashIsFalling, !isGrounded);

        if (_player != null && isGrounded)
        {
            float distance = Vector3.Distance(transform.position, _player.position);
            
            if (distance <= detectionRange && _attack.CanAttack())
            {
                RotateTowardsPlayer(); 
                _attack.PerformAttack();
            }
        }

        if (transform.position.y < -5f)
        {
            Die();
        }
    }

    private void RotateTowardsPlayer()
    {
        if (_player == null) return;

        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void Die()
    {
        if (ParticleManager.instance != null)
        {
            ParticleManager.instance.SpawnParticle(ParticleManager.instance.EnemyDisintegrationParticles, transform.position, transform.rotation);
        }
        Destroy(gameObject);
    }

    public void TakeKnockback(Vector3 direction, float force)
    {
        _animator.applyRootMotion = false;
        _knockbackTimer = 0.3f;
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        _rb.AddForce(direction * force, ForceMode.VelocityChange);
    }
}