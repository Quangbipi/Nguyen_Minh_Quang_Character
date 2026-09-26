using System;
using UnityEngine;

public sealed class BallKicker : MonoBehaviour
{
    private LayerMask ballLayerMask;
    private float kickTravelTime = 1f;
    private Collider[] balls;
    private Collider[] goals;

    public event Action<Transform, Collider> BallKicked;

    public void Configure(
        LayerMask layerMask,
        float travelTime,
        Collider[] candidateBalls,
        Collider[] candidateGoals)
    {
        ballLayerMask = layerMask;
        kickTravelTime = Mathf.Max(0.1f, travelTime);
        balls = candidateBalls;
        goals = candidateGoals;
    }

    public bool TryKick(Collider ball)
    {
        return TryKickAtNearestGoal(ball);
    }

    public bool TryAutoKick(Vector3 origin)
    {
        return TryKickAtNearestGoal(FindFarthestBall(origin));
    }

    private bool TryKickAtNearestGoal(Collider ball)
    {
        if (ball == null || !ball.TryGetComponent(out Rigidbody ballRigidbody))
        {
            return false;
        }

        Collider nearestGoal = FindNearestGoal(ball.bounds.center);
        if (nearestGoal == null)
        {
            return false;
        }

        Vector3 displacement = nearestGoal.bounds.center - ballRigidbody.position;
        ballRigidbody.velocity = displacement / kickTravelTime
            - Physics.gravity * (kickTravelTime * 0.5f);

        BallKicked?.Invoke(ballRigidbody.transform, nearestGoal);
        return true;
    }

    private Collider FindNearestGoal(Vector3 ballPosition)
    {
        if (goals == null)
        {
            return null;
        }

        Collider nearestGoal = null;
        float nearestSqrDistance = float.PositiveInfinity;

        foreach (Collider goal in goals)
        {
            if (goal == null || !goal.enabled || !goal.gameObject.activeInHierarchy)
            {
                continue;
            }

            float sqrDistance = (goal.bounds.center - ballPosition).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearestGoal = goal;
            }
        }

        return nearestGoal;
    }

    private Collider FindFarthestBall(Vector3 fromPosition)
    {
        Collider[] candidateBalls = GetAutoKickBallCandidates();
        Collider farthestBall = null;
        float farthestSqrDistance = float.NegativeInfinity;

        foreach (Collider candidate in candidateBalls)
        {
            if (candidate == null
                || !candidate.enabled
                || !candidate.gameObject.activeInHierarchy
                || !candidate.TryGetComponent(out Rigidbody ballRigidbody)
                || !ballRigidbody.gameObject.activeInHierarchy
                || ballRigidbody.isKinematic
                || IsBallInsideAnyGoal(candidate))
            {
                continue;
            }

            float sqrDistance =
                (candidate.bounds.center - fromPosition).sqrMagnitude;
            if (sqrDistance > farthestSqrDistance
                || (sqrDistance == farthestSqrDistance
                    && IsStableTieBreakPreferred(candidate, farthestBall)))
            {
                farthestSqrDistance = sqrDistance;
                farthestBall = candidate;
            }
        }

        return farthestBall;
    }

    private bool IsStableTieBreakPreferred(Collider candidate, Collider current)
    {
        if (current == null)
        {
            return true;
        }

        int keyComparison = string.CompareOrdinal(
            GetHierarchyKey(candidate.transform),
            GetHierarchyKey(current.transform));
        if (keyComparison != 0)
        {
            return keyComparison < 0;
        }

        return candidate.GetInstanceID() < current.GetInstanceID();
    }

    private string GetHierarchyKey(Transform target)
    {
        string hierarchyKey = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            hierarchyKey = target.name + "/" + hierarchyKey;
        }

        return hierarchyKey;
    }

    private Collider[] GetAutoKickBallCandidates()
    {
        if (balls != null)
        {
            foreach (Collider candidate in balls)
            {
                if (candidate != null && candidate.gameObject.activeInHierarchy)
                {
                    return balls;
                }
            }
        }

        Collider[] sceneColliders = FindObjectsOfType<Collider>();
        int matchingCount = 0;
        foreach (Collider sceneCollider in sceneColliders)
        {
            if (sceneCollider.enabled
                && IsInBallLayerMask(sceneCollider.gameObject.layer))
            {
                matchingCount++;
            }
        }

        Collider[] discoveredBalls = new Collider[matchingCount];
        int discoveredIndex = 0;
        foreach (Collider sceneCollider in sceneColliders)
        {
            if (sceneCollider.enabled
                && IsInBallLayerMask(sceneCollider.gameObject.layer))
            {
                discoveredBalls[discoveredIndex++] = sceneCollider;
            }
        }

        return discoveredBalls;
    }

    private bool IsBallInsideAnyGoal(Collider ball)
    {
        if (ball == null || goals == null)
        {
            return false;
        }

        Vector3 ballCenter = ball.bounds.center;
        foreach (Collider goal in goals)
        {
            if (goal != null
                && goal.enabled
                && goal.gameObject.activeInHierarchy
                && goal.bounds.Contains(ballCenter))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInBallLayerMask(int layer)
    {
        return (ballLayerMask.value & (1 << layer)) != 0;
    }
}
