using System;
using UnityEngine;

public sealed class BallDetector : MonoBehaviour
{
    private const int ConeArcSegments = 20;
    private const int InitialBufferSize = 8;
    private const float MinimumPlanarSqrMagnitude = 0.0001f;

    private Collider[] colliderBuffer = new Collider[InitialBufferSize];
    private LayerMask ballLayerMask;
    private float checkDistance;
    private float checkAngle;
    private Vector3 originOffset;

    public Collider CurrentBall { get; private set; }

    public event Action<Collider> TargetChanged;

    public void Configure(
        LayerMask layerMask,
        float distance,
        float angle,
        Vector3 checkOriginOffset)
    {
        ballLayerMask = layerMask;
        checkDistance = Mathf.Max(0f, distance);
        checkAngle = Mathf.Clamp(angle, 0f, 360f);
        originOffset = checkOriginOffset;
    }

    public Collider Scan()
    {
        Vector3 origin = transform.TransformPoint(originOffset);
        Collider nextBall = FindNearestBallInsideCone(origin);

#if UNITY_EDITOR
        DrawCheckCone(origin, nextBall != null ? Color.green : Color.red);
#endif

        if (!ReferenceEquals(nextBall, CurrentBall))
        {
            CurrentBall = nextBall;
            TargetChanged?.Invoke(CurrentBall);
        }

        return CurrentBall;
    }

    private Collider FindNearestBallInsideCone(Vector3 origin)
    {
        if (checkDistance <= 0f)
        {
            return null;
        }

        Vector3 planarForward = Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up);
        if (planarForward.sqrMagnitude <= MinimumPlanarSqrMagnitude)
        {
            return null;
        }

        int candidateCount = FindColliders(origin);
        float halfAngle = checkAngle * 0.5f;
        float nearestSqrDistance = float.PositiveInfinity;
        Collider nearestBall = null;

        for (int candidateIndex = 0;
             candidateIndex < candidateCount;
             candidateIndex++)
        {
            Collider candidate = colliderBuffer[candidateIndex];
            Vector3 directionToCandidate = Vector3.ProjectOnPlane(
                candidate.bounds.center - origin,
                Vector3.up);

            if (directionToCandidate.sqrMagnitude <= MinimumPlanarSqrMagnitude)
            {
                continue;
            }

            if (Vector3.Angle(planarForward, directionToCandidate) > halfAngle)
            {
                continue;
            }

            float sqrDistance = directionToCandidate.sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearestBall = candidate;
            }
        }

        return nearestBall;
    }

    private int FindColliders(Vector3 origin)
    {
        while (true)
        {
            int candidateCount = Physics.OverlapSphereNonAlloc(
                origin,
                checkDistance,
                colliderBuffer,
                ballLayerMask,
                QueryTriggerInteraction.Ignore);

            if (candidateCount < colliderBuffer.Length)
            {
                return candidateCount;
            }

            colliderBuffer = new Collider[colliderBuffer.Length * 2];
        }
    }

#if UNITY_EDITOR
    private void DrawCheckCone(Vector3 origin, Color color)
    {
        Vector3 planarForward = Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up);
        if (planarForward.sqrMagnitude <= MinimumPlanarSqrMagnitude)
        {
            return;
        }

        planarForward.Normalize();
        float halfAngle = checkAngle * 0.5f;
        Vector3 leftBoundary = Quaternion.AngleAxis(-halfAngle, Vector3.up)
            * planarForward
            * checkDistance;
        Vector3 rightBoundary = Quaternion.AngleAxis(halfAngle, Vector3.up)
            * planarForward
            * checkDistance;

        Debug.DrawRay(origin, leftBoundary, color);
        Debug.DrawRay(origin, rightBoundary, color);

        Vector3 previousPoint = origin + leftBoundary;
        for (int segment = 1; segment <= ConeArcSegments; segment++)
        {
            float angle = Mathf.Lerp(
                -halfAngle,
                halfAngle,
                segment / (float)ConeArcSegments);
            Vector3 arcDirection =
                Quaternion.AngleAxis(angle, Vector3.up) * planarForward;
            Vector3 currentPoint = origin + arcDirection * checkDistance;
            Debug.DrawLine(previousPoint, currentPoint, color);
            previousPoint = currentPoint;
        }
    }
#endif
}
