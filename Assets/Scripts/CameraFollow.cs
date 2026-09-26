using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float smoothTime = 0.2f;
    [SerializeField] private CharacterController characterController;
    [SerializeField, Min(0f)] private float goalArrivalDistance = 0.25f;
    [SerializeField, Min(0f)] private float postGoalFollowDuration = 2f;
    [SerializeField, Min(0f)] private float ballFollowTimeout = 5f;

    private Vector3 offset;
    private Vector3 velocity;
    private Transform characterTarget;
    private Collider trackedGoal;
    private float trackingElapsed;
    private float goalHoldElapsed;
    private bool hasReachedGoal;

    public Transform CurrentTarget => target;

    private void Start()
    {
        Initialize();

        if (characterController == null && characterTarget != null)
        {
            characterController = characterTarget.GetComponent<CharacterController>();
        }

        if (characterController != null)
        {
            characterController.BallKicked += BeginBallFollow;
        }
    }

    private void OnDestroy()
    {
        if (characterController != null)
        {
            characterController.BallKicked -= BeginBallFollow;
        }
    }

    private void LateUpdate()
    {
        TickTracking(Time.deltaTime);
        Follow(Time.deltaTime);
    }

    public void Configure(Transform followTarget, float followSmoothTime)
    {
        target = followTarget;
        smoothTime = Mathf.Max(0f, followSmoothTime);
    }

    public void Initialize()
    {
        if (target == null)
        {
            return;
        }

        characterTarget = target;
        offset = transform.position - target.position;
        velocity = Vector3.zero;
    }

    public void ConfigureTracking(
        float arrivalDistance,
        float postGoalDuration,
        float followTimeout)
    {
        goalArrivalDistance = Mathf.Max(0f, arrivalDistance);
        postGoalFollowDuration = Mathf.Max(0f, postGoalDuration);
        ballFollowTimeout = Mathf.Max(0f, followTimeout);
    }

    public void BeginBallFollow(Transform ball, Collider goal)
    {
        if (ball == null || goal == null)
        {
            return;
        }

        target = ball;
        trackedGoal = goal;
        trackingElapsed = 0f;
        goalHoldElapsed = 0f;
        hasReachedGoal = false;
        velocity = Vector3.zero;
    }

    public void TickTracking(float deltaTime)
    {
        if (ReferenceEquals(trackedGoal, null))
        {
            return;
        }

        if (target == null || trackedGoal == null)
        {
            RestoreCharacterTarget();
            return;
        }

        deltaTime = Mathf.Max(0f, deltaTime);

        if (!hasReachedGoal)
        {
            Vector3 ballPosition = target.position;
            float distanceToGoal = Vector3.Distance(
                ballPosition,
                trackedGoal.ClosestPoint(ballPosition));
            hasReachedGoal = distanceToGoal <= goalArrivalDistance;
            if (hasReachedGoal)
            {
                if (postGoalFollowDuration <= 0f)
                {
                    RestoreCharacterTarget();
                }

                return;
            }
        }

        if (hasReachedGoal)
        {
            goalHoldElapsed += deltaTime;
            if (goalHoldElapsed >= postGoalFollowDuration)
            {
                RestoreCharacterTarget();
            }
        }
        else
        {
            trackingElapsed += deltaTime;
            if (trackingElapsed >= ballFollowTimeout)
            {
                RestoreCharacterTarget();
            }
        }
    }

    private void RestoreCharacterTarget()
    {
        trackedGoal = null;
        trackingElapsed = 0f;
        goalHoldElapsed = 0f;
        hasReachedGoal = false;
        target = characterTarget;
        velocity = Vector3.zero;
    }

    public void Follow(float deltaTime)
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime,
            Mathf.Infinity,
            deltaTime);
    }
}
