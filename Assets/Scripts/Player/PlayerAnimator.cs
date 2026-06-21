using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;

    private static readonly int HashMoveZ = Animator.StringToHash("MoveZ");
    private static readonly int HashIsJumping = Animator.StringToHash("IsJumping");
    private static readonly int HashDodgedLeft = Animator.StringToHash("DodgeLeft");
    private static readonly int HashDodgedRight = Animator.StringToHash("DodgeRight");
    private static readonly int HashIsHanging = Animator.StringToHash("IsHanging");
    private static readonly int HashMantle = Animator.StringToHash("Mantle");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashIsFalling = Animator.StringToHash("IsFalling");

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void UpdateMoveAnimation(float targetZ)
    {
        _animator.SetFloat(HashMoveZ, targetZ, 0.05f, Time.deltaTime);
    }

    public void SetJumping(bool isJumping) => _animator.SetBool(HashIsJumping, isJumping);
    
    public void SetFalling(bool isFalling) => _animator.SetBool(HashIsFalling, isFalling);
    
    public void SetHanging(bool isHanging) => _animator.SetBool(HashIsHanging, isHanging);
    
    public void TriggerMantle() => _animator.SetTrigger(HashMantle);
    
    public void TriggerDodge(float direction)
    {
        _animator.SetTrigger(direction > 0 ? HashDodgedRight : HashDodgedLeft);
    }

    public void TriggerAttack() => _animator.SetTrigger(HashAttack);

    public void SetRootMotion(bool applyRootMotion)
    {
        _animator.applyRootMotion = applyRootMotion;
    }
}