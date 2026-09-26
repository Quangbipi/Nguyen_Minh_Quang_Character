public sealed class CharacterIdleState : CharacterState
{
    public CharacterIdleState(ICharacterStateContext character) : base(character)
    {
    }

    public override void Enter()
    {
        CharacterAnimator.SetFloat(SpeedHash, 0f);
    }

    public override void Tick()
    {
        if (Character.MoveInput.sqrMagnitude > 0f)
        {
            Character.ChangeState(CharacterStateId.Run);
        }
    }

    public override void Exit()
    {
    }
}
