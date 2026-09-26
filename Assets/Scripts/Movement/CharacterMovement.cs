using UnityEngine;

public sealed class CharacterMovement : MonoBehaviour
{
    private float moveSpeed;
    private float rotationSpeed;
    private Transform fieldCornerA;
    private Transform fieldCornerB;
    private float boundaryPadding;

    public void Configure(
        float speed,
        float turnSpeed,
        Transform cornerA,
        Transform cornerB,
        float padding)
    {
        moveSpeed = Mathf.Max(0f, speed);
        rotationSpeed = Mathf.Max(0f, turnSpeed);
        fieldCornerA = cornerA;
        fieldCornerB = cornerB;
        boundaryPadding = Mathf.Max(0f, padding);
    }

    public void Move(Vector3 direction, float deltaTime)
    {
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        Vector3 moveDirection = direction.normalized;
        Vector3 nextPosition =
            transform.position + moveDirection * (moveSpeed * safeDeltaTime);

        if (fieldCornerA != null && fieldCornerB != null)
        {
            float minX = Mathf.Min(
                fieldCornerA.position.x,
                fieldCornerB.position.x) + boundaryPadding;
            float maxX = Mathf.Max(
                fieldCornerA.position.x,
                fieldCornerB.position.x) - boundaryPadding;
            float minZ = Mathf.Min(
                fieldCornerA.position.z,
                fieldCornerB.position.z) + boundaryPadding;
            float maxZ = Mathf.Max(
                fieldCornerA.position.z,
                fieldCornerB.position.z) - boundaryPadding;

            nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
            nextPosition.z = Mathf.Clamp(nextPosition.z, minZ, maxZ);
        }

        transform.position = nextPosition;

        Quaternion targetRotation =
            Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * safeDeltaTime);
    }
}
