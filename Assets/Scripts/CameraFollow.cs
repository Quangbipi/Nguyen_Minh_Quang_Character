using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float smoothTime = 0.2f;
    [SerializeField] private CharacterController characterController;


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

        
    }

    private void OnDestroy()
    {
        
    }

    private void LateUpdate()
    {
        
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
