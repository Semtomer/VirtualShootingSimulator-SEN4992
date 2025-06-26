using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class DynamicEnemyCollider : MonoBehaviour
{
    [Header("Collider Settings")]
    [Tooltip("Sprite genişliğine göre collider boyutu çarpanı")]
    [Range(0.1f, 1.5f)]
    [SerializeField] private float colliderWidthMultiplier = 0.8f;

    [Tooltip("Sprite yüksekliğine göre collider boyutu çarpanı")]
    [Range(0.1f, 1.5f)]
    [SerializeField] private float colliderHeightMultiplier = 0.9f;

    [Tooltip("Collider X pozisyon düzeltmesi")]
    [SerializeField] private float colliderOffsetX = 0f;

    [Tooltip("Collider Y pozisyon düzeltmesi")]
    [SerializeField] private float colliderOffsetY = 0f;

    [Header("Attack State Collider Settings")]
    [Tooltip("Multiplier for collider width during attack.")]
    [SerializeField] private float attackWidthMultiplier = 1.2f;
    [Tooltip("Multiplier for collider height during attack.")]
    [SerializeField] private float attackHeightMultiplier = 1.0f;
    [Tooltip("Collider X offset during attack.")]
    [SerializeField] private float attackOffsetX = 0.2f;
    [Tooltip("Collider Y offset during attack.")]
    [SerializeField] private float attackOffsetY = 0f;

    private bool isAttackingForCollider = false;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Sprite previousSprite;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        UpdateCollider();
    }

    private void Update()
    {
        if (spriteRenderer.sprite != previousSprite)
        {
            UpdateCollider();
        }
    }

    public void SetColliderState(bool isAttacking)
    {
        isAttackingForCollider = isAttacking;
        UpdateCollider();
    }

    private void UpdateCollider()
    {
        previousSprite = spriteRenderer.sprite;

        if (spriteRenderer.sprite == null)
            return;

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        float currentWidthMultiplier = isAttackingForCollider ? attackWidthMultiplier : colliderWidthMultiplier;
        float currentHeightMultiplier = isAttackingForCollider ? attackHeightMultiplier : colliderHeightMultiplier;
        float currentOffsetX = isAttackingForCollider ? attackOffsetX : colliderOffsetX;
        float currentOffsetY = isAttackingForCollider ? attackOffsetY : colliderOffsetY;

        Vector2 colliderSize = new Vector2(
            spriteSize.x * currentWidthMultiplier,
            spriteSize.y * currentHeightMultiplier
        );

        boxCollider.size = colliderSize;

        boxCollider.offset = new Vector2(
            currentOffsetX,
            currentOffsetY
        );
    }

    private void OnValidate()
    {
        if (spriteRenderer != null && boxCollider != null && spriteRenderer.sprite != null)
        {
            UpdateCollider();
        }
    }
}