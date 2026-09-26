public sealed class CharacterRunState : CharacterState
{
    public CharacterRunState(CharacterController character) : base(character)
    {
    }

    public override void Enter()
    {
        CharacterAnimator.SetFloat(SpeedHash, 1f);
    }

    public override void Tick()
    {
        if (Character.MoveInput.sqrMagnitude <= 0f)
        {
            Character.ChangeState(Character.IdleState);
            return;
        }

        Character.Move(Character.MoveInput);
    }

    public override void Exit()
    {
    }
}
