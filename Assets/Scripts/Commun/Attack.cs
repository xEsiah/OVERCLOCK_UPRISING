using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Attack : MonoBehaviour
{
    public float attackRange = 0.4f;
    public float attackCooldown = 1f;
    public float pushForce = 25f;
    public string targetTag = "Player";
    public Transform attackOrigin;

    private Animator _animator;
    private float _lastAttackTime;
    private static readonly int HashAttack = Animator.StringToHash("Attack");

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public bool CanAttack()
    {
        return Time.time >= _lastAttackTime + attackCooldown;
    }

    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void PerformAttack()
    {
        _lastAttackTime = Time.time;
        _animator.SetTrigger(HashAttack);
    }

    public void TriggerAttackSound()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayAttackSound();
        }
    }

    public void TriggerAttackParticles()
    {
        if (attackOrigin != null)
        {
            ParticleManager.instance.SpawnParticle(ParticleManager.instance.DodgeParticles, attackOrigin.position, attackOrigin.rotation);
        }
    }

    public void TriggerHit()
    {
        if (attackOrigin == null) return;

        Collider[] hitColliders = Physics.OverlapSphere(attackOrigin.position, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag(targetTag))
            {
                Vector3 pushDirection = (hitCollider.transform.position - transform.position).normalized;
                
                pushDirection.y = 0.3f;
                pushDirection = pushDirection.normalized;

                PlayerController pc = hitCollider.GetComponentInParent<PlayerController>();
                if (pc != null)
                {
                    if (pc.IsDodging) continue;
                    pc.TakeKnockback(pushDirection, pushForce);
                    continue;
                }

                EnemyController ec = hitCollider.GetComponentInParent<EnemyController>();
                if (ec != null)
                {
                    ec.TakeKnockback(pushDirection, pushForce);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackOrigin.position, attackRange);
        }
    }
}