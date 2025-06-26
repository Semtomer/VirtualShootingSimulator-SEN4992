using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider2D), typeof(NavMeshObstacle), typeof(SpriteRenderer))]
public class SpecialAbilityChest : MonoBehaviour
{
    private SpecialAbilityType abilityContained = SpecialAbilityType.None;
    private Sprite closedSprite;
    private Sprite openedSprite;

    [Header("Timing")]
    [Tooltip("How long the chest stays spawned before disappearing if not collected (seconds).")]
    [SerializeField] private float lifetime = 5.0f;
    [Tooltip("How long the opened chest sprite stays visible before the object is destroyed (seconds).")]
    [SerializeField] private float openedStateDuration = 1.0f;

    [Header("Difficulty Based Lifetime")]
    [SerializeField] private float easyModeLifetime = 7.0f;
    [SerializeField] private float hardModeLifetime = 3.0f;

    private GameSide side;
    private SpriteRenderer spriteRenderer;
    private bool isOpened = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpecialAbilityChest is missing a SpriteRenderer component!", this);
            enabled = false;
        }
    }

    public bool IsOpen()
    {
        return isOpened;
    }

    private void Start()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = closedSprite;
        }

        float currentLifetime = lifetime;
        if (GameManager.Instance != null)
        {
            switch (GameManager.Instance.currentDifficulty)
            {
                case GameDifficulty.Easy: currentLifetime = easyModeLifetime; break;
                case GameDifficulty.Hard: currentLifetime = hardModeLifetime; break;
            }
        }
        Destroy(gameObject, currentLifetime);
    }

    public void AssignSide(GameSide assignedSide)
    {
        side = assignedSide;
    }

    public GameSide GetSide()
    {
        return side;
    }

    public void SetContainedAbility(SpecialAbilityType ability)
    {
        abilityContained = ability;
    }

    public SpecialAbilityType GetContainedAbility()
    {
        return abilityContained;
    }

    public void OpenChest()
    {
        if (isOpened)
            return;

        isOpened = true;

        Invoke(nameof(ChestOpening), 1f);
    }

    private void ChestOpening()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxChestOpen);

        if (spriteRenderer != null && openedSprite != null)
        {
            spriteRenderer.sprite = openedSprite;
        }
        else if (spriteRenderer != null && openedSprite == null)
        {
            Debug.LogWarning($"Opened sprite not set for chest: {gameObject.name}", this);
        }

        CancelInvoke(nameof(SelfDestructByLifetime));
        Destroy(gameObject, openedStateDuration);
    }

    private void SelfDestructByLifetime()
    {
        if (!isOpened)
        {
            Destroy(gameObject);
        }
    }

    public void SetVisuals(Sprite closed, Sprite opened)
    {
        closedSprite = closed;
        openedSprite = opened;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = closedSprite;
        }
    }
}