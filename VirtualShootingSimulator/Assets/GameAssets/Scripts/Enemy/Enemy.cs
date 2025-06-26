using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    #region Fields and Properties

    [Header("Enemy Stats")]
    [Tooltip("The initial health of the enemy.")]
    [SerializeField] private float health = 100f;
    [Tooltip("The enemy's damage with each attack.")]
    [SerializeField] private float attackDamage = 10f;

    private Animator animator;
    private EnemyMovement enemyMovement;

    private bool isDead = false;
    private bool isStunned = false;

    private Vector2 currentDirection = Vector2.down;

    private GameSide side;
    private Castle assignedTargetCastle;

    public bool IsDead => isDead;

    private BoxCollider2D boxCollider;

    private int killerPlayerID = 0;

    private DynamicEnemyCollider dynamicCollider;

    #endregion

    #region Unity Methods

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        enemyMovement = GetComponent<EnemyMovement>();
        boxCollider = GetComponent<BoxCollider2D>();

        currentDirection = new Vector2(0f, -1f);

        dynamicCollider = GetComponent<DynamicEnemyCollider>();
    }

    #endregion

    #region Public Methods

    public void AssignTargetCastle(Castle castleToAttack)
    {
        assignedTargetCastle = castleToAttack;
    }

    public void AssignSide(GameSide assignedSide)
    {
        side = assignedSide;
    }

    public GameSide GetSide()
    {
        return side;
    }

    public void TakeDamage(float damageAmount, int attackerPlayerID = 0)
    {
        if (isDead)
            return;

        int potentialKillerID = attackerPlayerID;

        health -= damageAmount;

        if (health > 0)
        {
            killerPlayerID = potentialKillerID;
        }

        if (health > 0)
        {
            SetDirection(currentDirection);
            animator.SetTrigger("Damaging");

            Stun(0.5f);
        }
        else
        {
            Die(potentialKillerID);
            return;
        }
    }

    public void GiveDamage()
    {
        if (isDead)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        if (assignedTargetCastle != null)
        {
            bool currentlyWeakened = GameManager.Instance != null && GameManager.Instance.IsWeakenActive(side);
            float currentDamage = currentlyWeakened ? (attackDamage * 0.5f) : attackDamage;

            Debug.Log($"{gameObject.name} dealing {(currentlyWeakened ? "weakened " : "")}{currentDamage} damage to {assignedTargetCastle.name}");
            assignedTargetCastle.TakeDamage(currentDamage);
        }
        else
        {
            StopAttack();
        }
    }

    public void SetColliderStateTrue()
    {
        if (dynamicCollider != null)
        {
            dynamicCollider.SetColliderState(true);
        }
    }

    public void SetColliderStateFalse()
    {
        if (dynamicCollider != null)
        {
            dynamicCollider.SetColliderState(false);
        }
    }

    public void Attack()
    {
        if (isDead)
            return;

        if (assignedTargetCastle == null)
        {
            StopAttack();
            SetIdleDirection();
            return;
        }

        SetIdleDirection();
        animator.SetBool("isAttacking", true);
    }

    public void StopAttack()
    {
        animator.SetBool("isAttacking", false);
    }

    public void SetWalkingAnimation(bool isWalking)
    {
        animator.SetBool("isWalking", isWalking);
    }

    public void SetIdleDirection()
    {
        currentDirection = new Vector2(0f, -1f);
        animator.SetFloat("MoveX", currentDirection.x);
        animator.SetFloat("MoveY", currentDirection.y);
    }

    public void SetDirection(Vector2 direction)
    {
        if ((direction.normalized - currentDirection).sqrMagnitude > 0.001f)
        {
            currentDirection = direction.normalized;
        }

        animator.SetFloat("MoveX", currentDirection.x);
        animator.SetFloat("MoveY", currentDirection.y);
    }

    public void Stun(float duration)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        if (isStunned || isDead)
            return;

        isStunned = true;
        ApplyStunEffects();

        StartCoroutine(StunRoutine(duration));
    }

    #endregion

    #region Private Methods

    private void Die(int finalAttackerPlayerID)
    {
        if (isDead)
            return;

        isDead = true;

        AudioManager.Instance?.PlayDieSound(GameSettings.SelectedDifficulty);

        if (GameManager.Instance != null && finalAttackerPlayerID != 0)
        {
            GameManager.Instance.ReportEnemyKill(this, finalAttackerPlayerID);
        }

        if (enemyMovement != null) enemyMovement.StopMovement();
        StopAttack();
        StopAllCoroutines();

        SetWalkingAnimation(false);
        SetDirection(currentDirection);

        animator.SetTrigger("Dying");

        if (boxCollider != null)
            boxCollider.enabled = false;

        NavMeshAgent agent = enemyMovement?.GetAgent();
        if (agent != null)
            agent.enabled = false;

        Destroy(gameObject, 2f);
    }

    private void ApplyStunEffects()
    {
        if (!enemyMovement.HasReached)
        {
            enemyMovement.StopMovement();
        }
        else
        {
            animator.SetBool("isAttacking", false);
        }

        SetWalkingAnimation(false);
        SetDirection(currentDirection);

        animator.SetBool("isStunned", true);
    }

    private void RemoveStunEffects()
    {
        if (isDead)
            return;

        isStunned = false;
        animator.SetBool("isStunned", false);

        if (enemyMovement.HasReached)
        {
            if (assignedTargetCastle != null)
                Attack();
            else
                SetIdleDirection();
        }
        else
        {
            enemyMovement.CancelStopMovement();
        }
    }

    private IEnumerator StunRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        RemoveStunEffects();
    }
    #endregion
}