using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Animator))]
public class CharacterController : MonoBehaviour
{
    private const int ConeArcSegments = 20;
    private const int InitialBallColliderBufferSize = 8;
    private const float MinimumPlanarSqrMagnitude = 0.0001f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float rotationSpeed = 12f;

    [Header("Ball Check")]
    [SerializeField] private LayerMask ballLayerMask;
    [SerializeField, Min(0f)] private float ballCheckDistance = 2f;
    [SerializeField, Range(0f, 360f)] private float ballCheckAngle = 90f;
    [FormerlySerializedAs("ballRayOriginOffset")]
    [SerializeField] private Vector3 ballCheckOriginOffset = new Vector3(0f, -0.1f, 0f);

    [Header("Kick")]
    [SerializeField] private LayerMask goalLayerMask = 1 << 8;
    [SerializeField, Min(0.1f)] private float kickTravelTime = 1f;
    [SerializeField] private Collider[] goals;
    [SerializeField] private Collider[] balls;

    [Header("Movement Bounds")]
    [SerializeField] private Transform fieldCornerA;
    [SerializeField] private Transform fieldCornerB;
    [SerializeField, Min(0f)] private float boundaryPadding = 0.5f;

    private Collider[] ballColliderBuffer = new Collider[InitialBallColliderBufferSize];
    private Collider nearbyBall;
    private CharacterState currentState;

    public Animator Animator { get; private set; }
    public Vector3 MoveInput { get; private set; }
    public CharacterState IdleState { get; private set; }
    public CharacterState RunState { get; private set; }
    public bool IsBallInFront { get; private set; }

    public event Action<bool> BallProximityChanged;
    public event Action<Transform, Collider> BallKicked;

    private void Awake()
    {
        Animator = GetComponent<Animator>();
        Animator.applyRootMotion = false;

        IdleState = new CharacterIdleState(this);
        RunState = new CharacterRunState(this);
    }

    private void Start()
    {
        ChangeState(IdleState);
    }

    private void Update()
    {
        MoveInput = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical"));

        MoveInput = Vector3.ClampMagnitude(MoveInput, 1f);
        currentState?.Tick();
        CheckForBallInFront();
    }

    public void ChangeState(CharacterState nextState)
    {
        if (nextState == null || nextState == currentState)
        {
            return;
        }

        currentState?.Exit();
        currentState = nextState;
        currentState.Enter();
    }

    public void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 moveDirection = direction.normalized;
        Vector3 nextPosition =
            transform.position + moveDirection * (moveSpeed * Time.deltaTime);

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
            rotationSpeed * Time.deltaTime);
    }

    public void ConfigureBallCheck(
        LayerMask layerMask,
        float checkDistance,
        float checkAngle,
        Vector3 checkOriginOffset)
    {
        ballLayerMask = layerMask;
        ballCheckDistance = Mathf.Max(0f, checkDistance);
        ballCheckAngle = Mathf.Clamp(checkAngle, 0f, 360f);
        ballCheckOriginOffset = checkOriginOffset;
    }

    public void ConfigureAutoKick(
        Collider[] candidateBalls,
        Collider[] candidateGoals)
    {
        balls = candidateBalls;
        goals = candidateGoals;
    }

    public bool CheckForBallInFront()
    {
        Vector3 origin = transform.TransformPoint(ballCheckOriginOffset);
        bool wasBallInFront = IsBallInFront;

        nearbyBall = FindNearestBallInsideCone(origin);
        IsBallInFront = nearbyBall != null;
#if UNITY_EDITOR
        DrawBallCheckCone(origin, IsBallInFront ? Color.green : Color.red);
#endif

        if (IsBallInFront != wasBallInFront)
        {
            if (IsBallInFront)
            {
                HandleBallDetected();
            }

            BallProximityChanged?.Invoke(IsBallInFront);
        }

        return IsBallInFront;
    }

    public bool TryKick()
    {
        if (nearbyBall == null
            || !nearbyBall.TryGetComponent(out Rigidbody ballRigidbody))
        {
            return false;
        }

        Collider nearestGoal = FindNearestGoal(nearbyBall.bounds.center);
        if (nearestGoal == null)
        {
            return false;
        }

        float travelTime = Mathf.Max(0.1f, kickTravelTime);
        Vector3 displacement = nearestGoal.bounds.center - ballRigidbody.position;
        ballRigidbody.velocity = displacement / travelTime
            - Physics.gravity * (travelTime * 0.5f);

        BallKicked?.Invoke(ballRigidbody.transform, nearestGoal);
        return true;
    }

    public bool TryAutoKick()
    {
        Collider farthestBall = FindFarthestBall(transform.position);
        if (farthestBall == null
            || !farthestBall.TryGetComponent(out Rigidbody ballRigidbody))
        {
            return false;
        }

        Collider nearestGoal = FindNearestGoal(farthestBall.bounds.center);
        if (nearestGoal == null)
        {
            return false;
        }

        float travelTime = Mathf.Max(0.1f, kickTravelTime);
        Vector3 displacement = nearestGoal.bounds.center - ballRigidbody.position;
        ballRigidbody.velocity = displacement / travelTime
            - Physics.gravity * (travelTime * 0.5f);

        BallKicked?.Invoke(ballRigidbody.transform, nearestGoal);
        return true;
    }

    private Collider FindNearestBallInsideCone(Vector3 origin)
    {
        if (ballCheckDistance <= 0f)
        {
            return null;
        }

        Vector3 planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (planarForward.sqrMagnitude <= MinimumPlanarSqrMagnitude)
        {
            return null;
        }

        int candidateCount = FindBallColliders(origin);
        float halfAngle = ballCheckAngle * 0.5f;
        float nearestSqrDistance = float.PositiveInfinity;
        Collider nearestBall = null;

        for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
        {
            Collider candidate = ballColliderBuffer[candidateIndex];
            Vector3 directionToCandidate = Vector3.ProjectOnPlane(
                candidate.bounds.center - origin,
                Vector3.up);

            if (directionToCandidate.sqrMagnitude <= MinimumPlanarSqrMagnitude)
            {
                continue;
            }

            if (Vector3.Angle(planarForward, directionToCandidate) <= halfAngle)
            {
                float sqrDistance = directionToCandidate.sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearestBall = candidate;
                }
            }
        }

        return nearestBall;
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

            float sqrDistance =
                (goal.bounds.center - ballPosition).sqrMagnitude;

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
                || IsBallInsideAnyGoal(candidate, goals))
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

        string candidateKey = GetHierarchyKey(candidate.transform);
        string currentKey = GetHierarchyKey(current.transform);
        int keyComparison = string.CompareOrdinal(candidateKey, currentKey);
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
                if (candidate != null
                    && candidate.gameObject.activeInHierarchy)
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

    private bool IsBallInsideAnyGoal(Collider ball, Collider[] goalColliders)
    {
        if (ball == null || goalColliders == null)
        {
            return false;
        }

        Vector3 ballCenter = ball.bounds.center;
        foreach (Collider goal in goalColliders)
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

    private int FindBallColliders(Vector3 origin)
    {
        while (true)
        {
            int candidateCount = Physics.OverlapSphereNonAlloc(
                origin,
                ballCheckDistance,
                ballColliderBuffer,
                ballLayerMask,
                QueryTriggerInteraction.Ignore);

            if (candidateCount < ballColliderBuffer.Length)
            {
                return candidateCount;
            }

            ballColliderBuffer = new Collider[ballColliderBuffer.Length * 2];
        }
    }

#if UNITY_EDITOR
    private void DrawBallCheckCone(Vector3 origin, Color color)
    {
        Vector3 planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (planarForward.sqrMagnitude <= MinimumPlanarSqrMagnitude)
        {
            return;
        }

        planarForward.Normalize();
        float halfAngle = ballCheckAngle * 0.5f;
        Vector3 leftBoundary = Quaternion.AngleAxis(-halfAngle, Vector3.up)
            * planarForward
            * ballCheckDistance;
        Vector3 rightBoundary = Quaternion.AngleAxis(halfAngle, Vector3.up)
            * planarForward
            * ballCheckDistance;

        Debug.DrawRay(origin, leftBoundary, color);
        Debug.DrawRay(origin, rightBoundary, color);

        Vector3 previousPoint = origin + leftBoundary;
        for (int segment = 1; segment <= ConeArcSegments; segment++)
        {
            float angle = Mathf.Lerp(
                -halfAngle,
                halfAngle,
                segment / (float)ConeArcSegments);
            Vector3 arcDirection = Quaternion.AngleAxis(angle, Vector3.up) * planarForward;
            Vector3 currentPoint = origin + arcDirection * ballCheckDistance;
            Debug.DrawLine(previousPoint, currentPoint, color);
            previousPoint = currentPoint;
        }
    }
#endif

    private void HandleBallDetected()
    {
        Debug.Log("Ball is front");
    }

}
