public sealed class CharacterIdleState : CharacterState
{
    public CharacterIdleState(CharacterCtrl character) : base(character)
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
            Character.ChangeState(Character.RunState);
        }
    }

    public override void Exit()
    {
    }
}
