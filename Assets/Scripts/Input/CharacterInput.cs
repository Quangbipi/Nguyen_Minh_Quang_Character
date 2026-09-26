using UnityEngine;

public sealed class CharacterInput : MonoBehaviour
{
    public Vector3 MoveDirection { get; private set; }

    public void Sample()
    {
        MoveDirection = Vector3.ClampMagnitude(
            new Vector3(
                Input.GetAxisRaw("Horizontal"),
                0f,
                Input.GetAxisRaw("Vertical")),
            1f);
    }
}
