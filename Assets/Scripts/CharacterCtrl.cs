using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterInput))]
[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(BallDetector))]
[RequireComponent(typeof(BallKicker))]
public class CharacterCtrl : MonoBehaviour, ICharacterStateContext
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float rotationSpeed = 12f;

    [Header("Ball Check")]
    [SerializeField] private LayerMask ballLayerMask;
    [SerializeField, Min(0f)] private float ballCheckDistance = 2f;
    [SerializeField, Range(0f, 360f)] private float ballCheckAngle = 90f;
    [FormerlySerializedAs("ballRayOriginOffset")]
    [SerializeField] private Vector3 ballCheckOriginOffset =
        new Vector3(0f, -0.1f, 0f);

    [Header("Kick")]
    [SerializeField] private LayerMask goalLayerMask = 1 << 8;
    [SerializeField, Min(0.1f)] private float kickTravelTime = 1f;
    [SerializeField] private Collider[] goals;
    [SerializeField] private Collider[] balls;

    [Header("Movement Bounds")]
    [SerializeField] private Transform fieldCornerA;
    [SerializeField] private Transform fieldCornerB;
    [SerializeField, Min(0f)] private float boundaryPadding = 0.5f;

    private readonly Dictionary<CharacterStateId, CharacterState> states =
        new Dictionary<CharacterStateId, CharacterState>();

    private CharacterInput characterInput;
    private CharacterMovement characterMovement;
    private BallDetector ballDetector;
    private BallKicker ballKicker;
    private CharacterStateMachine stateMachine;
    private bool isInitialized;
    private bool eventsSubscribed;

    public Animator Animator { get; private set; }
    public Vector3 MoveInput { get; private set; }
    public CharacterState IdleState { get; private set; }
    public CharacterState RunState { get; private set; }
    public bool IsBallInFront { get; private set; }

    public event Action<bool> BallProximityChanged;
    public event Action<Transform, Collider> BallKicked;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        SubscribeToCollaborators();
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        Animator = GetComponent<Animator>();
        characterInput = GetOrAddComponent<CharacterInput>();
        characterMovement = GetOrAddComponent<CharacterMovement>();
        ballDetector = GetOrAddComponent<BallDetector>();
        ballKicker = GetOrAddComponent<BallKicker>();

        Animator.applyRootMotion = false;
        ConfigureCollaborators();

        IdleState = new CharacterIdleState(this);
        RunState = new CharacterRunState(this);
        states[CharacterStateId.Idle] = IdleState;
        states[CharacterStateId.Run] = RunState;
        stateMachine = new CharacterStateMachine();
        isInitialized = true;
    }

    private void Start()
    {
        ChangeState(CharacterStateId.Idle);
    }

    private void Update()
    {
        characterInput.Sample();
        MoveInput = characterInput.MoveDirection;
        stateMachine.Tick();
        ballDetector.Scan();
    }

    private void OnDisable()
    {
        if (!eventsSubscribed)
        {
            return;
        }

        ballDetector.TargetChanged -= HandleDetectedBallChanged;
        ballKicker.BallKicked -= HandleBallKicked;
        eventsSubscribed = false;
    }

    public void ChangeState(CharacterState nextState)
    {
        EnsureInitialized();
        stateMachine.ChangeState(nextState);
    }

    public void ChangeState(CharacterStateId stateId)
    {
        EnsureInitialized();
        if (states.TryGetValue(stateId, out CharacterState nextState))
        {
            stateMachine.ChangeState(nextState);
        }
    }

    public void Move(Vector3 direction)
    {
        EnsureInitialized();
        characterMovement.Move(direction, Time.deltaTime);
    }

    public void ConfigureBallCheck(
        LayerMask layerMask,
        float checkDistance,
        float checkAngle,
        Vector3 checkOriginOffset)
    {
        EnsureInitialized();
        ballLayerMask = layerMask;
        ballCheckDistance = Mathf.Max(0f, checkDistance);
        ballCheckAngle = Mathf.Clamp(checkAngle, 0f, 360f);
        ballCheckOriginOffset = checkOriginOffset;

        if (ballDetector != null)
        {
            ballDetector.Configure(
                ballLayerMask,
                ballCheckDistance,
                ballCheckAngle,
                ballCheckOriginOffset);
        }

        ConfigureKicker();
    }

    public void ConfigureAutoKick(
        Collider[] candidateBalls,
        Collider[] candidateGoals)
    {
        EnsureInitialized();
        balls = candidateBalls;
        goals = candidateGoals;
        ConfigureKicker();
    }

    public bool CheckForBallInFront()
    {
        EnsureInitialized();
        SubscribeToCollaborators();
        ballDetector.Scan();
        return IsBallInFront;
    }

    public bool TryKick()
    {
        EnsureInitialized();
        SubscribeToCollaborators();
        return ballKicker.TryKick(ballDetector.CurrentBall);
    }

    public bool TryAutoKick()
    {
        EnsureInitialized();
        SubscribeToCollaborators();
        return ballKicker.TryAutoKick(transform.position);
    }

    private void ConfigureCollaborators()
    {
        characterMovement.Configure(
            moveSpeed,
            rotationSpeed,
            fieldCornerA,
            fieldCornerB,
            boundaryPadding);
        ballDetector.Configure(
            ballLayerMask,
            ballCheckDistance,
            ballCheckAngle,
            ballCheckOriginOffset);
        ConfigureKicker();
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private void ConfigureKicker()
    {
        if (ballKicker == null)
        {
            return;
        }

        ballKicker.Configure(
            ballLayerMask,
            kickTravelTime,
            balls,
            goals);
    }

    private void SubscribeToCollaborators()
    {
        if (eventsSubscribed || !isActiveAndEnabled)
        {
            return;
        }

        ballDetector.TargetChanged += HandleDetectedBallChanged;
        ballKicker.BallKicked += HandleBallKicked;
        eventsSubscribed = true;
    }

    private void HandleDetectedBallChanged(Collider detectedBall)
    {
        bool isBallInFront = detectedBall != null;
        if (isBallInFront == IsBallInFront)
        {
            return;
        }

        IsBallInFront = isBallInFront;
        if (IsBallInFront)
        {
            Debug.Log("Ball is front");
        }

        BallProximityChanged?.Invoke(IsBallInFront);
    }

    private void HandleBallKicked(Transform ball, Collider goal)
    {
        BallKicked?.Invoke(ball, goal);
    }
}
