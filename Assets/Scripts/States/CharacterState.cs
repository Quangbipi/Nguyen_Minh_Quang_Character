using UnityEngine;

public abstract class CharacterState
{
    protected static readonly int SpeedHash = UnityEngine.Animator.StringToHash("Speed");

    protected CharacterController Character { get; }
    protected Animator CharacterAnimator => Character.Animator;

    protected CharacterState(CharacterController character)
    {
        Character = character;
    }

    public abstract void Enter();
    public abstract void Tick();
    public abstract void Exit();
}
