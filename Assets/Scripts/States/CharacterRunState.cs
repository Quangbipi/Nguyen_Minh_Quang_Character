public sealed class CharacterRunState : CharacterState
{
    public CharacterRunState(ICharacterStateContext character) : base(character)
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
            Character.ChangeState(CharacterStateId.Idle);
            return;
        }

        Character.Move(Character.MoveInput);
    }

    public override void Exit()
    {
    }
}
