using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Enemy))]
public class EnemyMovement : MonoBehaviour
{
    private Enemy enemy;
    private NavMeshAgent agent;
    private TargetManager targetManager;

    [Tooltip("Determines how close to the target the enemy will stop and attack (added to the Agent's stoppingDistance).")]
    [SerializeField] private float attackDistance = 0.2f;

    public bool HasReached { get; private set; } = false;

    private Transform currentMoveTarget;
    private Transform finalDestinationPoint;

    private float originalSpeed;

    public NavMeshAgent GetAgent() => agent;

    public Transform GetFinalDestination() => finalDestinationPoint;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        agent = GetComponent<NavMeshAgent>();

        if (enemy == null)
            Debug.LogError("Enemy component not found on " + gameObject.name, this);
        if (agent == null)
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name, this);
        else
        {
            originalSpeed = agent.speed;
        }

        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            if (agent != null && agent.isOnNavMesh && agent.enabled && !agent.isStopped)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            return;
        }

        if (enemy == null || enemy.IsDead || agent == null || !agent.isOnNavMesh || !agent.enabled)
            return;

        ApplySpeedFromGlobalState();

        if (currentMoveTarget == null)
        {
            if (!HasReached)
            {
                StopMovement();

                if (enemy != null)
                    enemy.SetIdleDirection();

                HasReached = true;
            }
            return;
        }

        if (!HasReached)
        {
            MoveTowardsTarget();

            float distanceToTarget = Vector2.Distance(transform.position, currentMoveTarget.position);
            if (distanceToTarget < agent.stoppingDistance + attackDistance)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance &&
                   (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f))
                {
                    ReachedTarget();
                }
            }
        }
    }

    private void ApplySpeedFromGlobalState()
    {
        if (agent == null || !agent.enabled || enemy == null)
            return;

        if (GameManager.Instance != null)
        {
            GameSide side = enemy.GetSide();
            if (GameManager.Instance.IsSlowActive(side))
            {
                agent.speed = originalSpeed * GameManager.Instance.GetSlowFactor(side);
            }
            else
            {
                agent.speed = originalSpeed;
            }
        }
        else
        {
            agent.speed = originalSpeed;
        }
    }

    private void ReachedTarget()
    {
        if (HasReached)
            return;

        HasReached = true;
        StopMovement();

        if (enemy != null)
        {
            enemy.SetWalkingAnimation(false);
            enemy.SetIdleDirection();

            enemy.Attack();
        }
    }

    public void SetTarget(Transform targetPoint, TargetManager manager)
    {
        if (targetPoint == null)
        {
            if (enemy != null)
                enemy.SetIdleDirection();
            StopMovement();
            HasReached = true;
            currentMoveTarget = null;
            finalDestinationPoint = null;
            targetManager = null;
            return;
        }

        if (agent == null || !agent.enabled)
        {
            Debug.LogWarning($"{gameObject.name} cannot set target, NavMeshAgent is invalid or disabled.");
            return;
        }

        currentMoveTarget = targetPoint;
        finalDestinationPoint = targetPoint;
        targetManager = manager;

        HasReached = false;

        if (agent.isOnNavMesh)
            agent.isStopped = false;
    }
    private void MoveTowardsTarget()
    {
        if (currentMoveTarget == null || agent == null || !agent.isOnNavMesh || !agent.enabled || agent.isStopped)
            return;

        agent.SetDestination(currentMoveTarget.position);

        bool isMoving = agent.velocity.sqrMagnitude > 0.01f;

        if (enemy != null)
        {
            enemy.SetWalkingAnimation(isMoving);

            if (isMoving)
            {
                enemy.SetDirection(agent.velocity.normalized);
            }
        }
    }

    public void CancelStopMovement()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        if (enemy.IsDead || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
    }

    public void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    private void OnDestroy()
    {
        if (targetManager != null && currentMoveTarget != null)
        {
            targetManager.ReleaseTarget(currentMoveTarget);
        }
    }
}