using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{

    private Animator _animator;

    private static readonly int HashMoveZ = Animator.StringToHash("MoveZ");
    private static readonly int HashIsJumping = Animator.StringToHash("IsJumping");
    private static readonly int HashDashLeft = Animator.StringToHash("DashLeft");
    private static readonly int HashDashRight = Animator.StringToHash("DashRight");
    private static readonly int HashIsHanging = Animator.StringToHash("IsHanging");
    private static readonly int HashMantle = Animator.StringToHash("Mantle");

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }


    public void UpdateMoveAnimation(float targetZ)
    {
        _animator.SetFloat(HashMoveZ, targetZ, 0.05f, Time.deltaTime);
    }

    public void SetJumping(bool isJumping) => _animator.SetBool(HashIsJumping, isJumping);
    
    public void SetHanging(bool isHanging) => _animator.SetBool(HashIsHanging, isHanging);
    
    public void TriggerMantle() => _animator.SetTrigger(HashMantle);
    
    public void TriggerDash(float direction)
    {
        _animator.SetTrigger(direction > 0 ? HashDashRight : HashDashLeft);
    }

    public void SetRootMotion(bool applyRootMotion)
    {
        _animator.applyRootMotion = applyRootMotion;
    }
}