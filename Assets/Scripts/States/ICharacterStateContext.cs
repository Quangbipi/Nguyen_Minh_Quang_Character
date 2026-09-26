using UnityEngine;

public interface ICharacterStateContext
{
    Animator Animator { get; }
    Vector3 MoveInput { get; }

    void Move(Vector3 direction);
    void ChangeState(CharacterStateId stateId);
}
