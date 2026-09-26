public sealed class CharacterStateMachine
{
    public CharacterState CurrentState { get; private set; }

    public void ChangeState(CharacterState nextState)
    {
        if (nextState == null || nextState == CurrentState)
        {
            return;
        }

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }

    public void Tick()
    {
        CurrentState?.Tick();
    }
}
