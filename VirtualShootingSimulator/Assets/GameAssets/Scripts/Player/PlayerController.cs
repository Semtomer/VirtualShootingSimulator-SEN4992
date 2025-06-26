using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Player Identification")]
    [Tooltip("Identifier for the player (e.g., 1 or 2). Determines the control scheme and interaction side.")]
    public int playerID = 1;

    [Header("Input Keys")]
    [Tooltip("Key used by this player to fire.")]
    [SerializeField] private KeyCode fireKey = KeyCode.F;
    [Tooltip("Key used by this player to activate special ability.")]
    [SerializeField] private KeyCode specialKey = KeyCode.G;

    [Header("Combat Settings")]
    [Tooltip("Damage dealt by a single player shot.")]
    [SerializeField] private float shotDamage = 25f;
    [Tooltip("Layers that the player's shot can hit (e.g., Enemies, Chests).")]
    [SerializeField] private LayerMask shootableLayers;

    [Header("Special Ability Settings")]
    [Tooltip("The duration of the stun special ability in seconds.")]
    [SerializeField] private float stunAbilityDuration = 3f;
    [Tooltip("Duration of the Slow ability (seconds).")]
    [SerializeField] private float slowAbilityDuration = 6f;
    [Tooltip("Speed multiplier for the Slow ability (e.g., 0.5 for 50% speed).")]
    [SerializeField][Range(0.1f, 0.9f)] private float slowFactor = 0.5f;
    [Tooltip("Duration of the Weaken Enemy Attack ability (seconds).")]
    [SerializeField] private float weakenAttackDuration = 6f;

    private Queue<SpecialAbilityType> heldAbilities = new Queue<SpecialAbilityType>();
    [Tooltip("Maximum number of unique abilities the player can hold at once.")]
    [SerializeField] private int maxHeldAbilities = 3;

    [Header("UI")]
    [Tooltip("Reference to this player's AbilitySystemUI instance.")]
    [SerializeField] private AbilitySystemUI abilitySystemUI;

    [Header("Global Ability Cooldown")]
    [Tooltip("Cooldown in seconds after using any ability before another can be used.")]
    [SerializeField] private float globalAbilityCooldownDuration = 2.0f;
    public float GlobalAbilityCooldownDuration => globalAbilityCooldownDuration;
    private bool isGlobalCooldownActive = false;
    private float globalCooldownEndTime = 0f;

    private Camera mainCamera;
    private GameSide playerSide;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError($"Player {playerID} Controller could not find the main camera! Disabling.", this);
            enabled = false;
            return;
        }

        playerSide = (playerID == 1) ? GameSide.Left : GameSide.Right;

        if (shootableLayers == 0)
        {
            Debug.LogWarning($"Player {playerID} Controller has no Shootable Layers selected in the Inspector. Raycasts might fail.", this);
        }

        if (abilitySystemUI == null)
        {
            Debug.LogError($"Player {playerID}: AbilitySystemUI not assigned!", this);
        }
    }

    private void Start()
    {
        if (abilitySystemUI != null)
        {
            abilitySystemUI.UpdateAbilityVisualsFromQueue(heldAbilities, isGlobalCooldownActive);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (isGlobalCooldownActive && Time.time >= globalCooldownEndTime)
        {
            isGlobalCooldownActive = false;
            if (abilitySystemUI != null)
                abilitySystemUI.UpdateAbilityVisualsFromQueue(heldAbilities, isGlobalCooldownActive);
            Debug.Log($"Player {playerID} global ability cooldown finished.");
        }

        HandleFireInput();
        HandleSpecialAbilityActivation();
    }

    private void HandleFireInput()
    {
        if (Input.GetKeyDown(fireKey))
        {
            RaycastHit2D hit = Physics2D.Raycast(
                mainCamera.ScreenToWorldPoint(Input.mousePosition),
                Vector2.zero,
                Mathf.Infinity,
                shootableLayers
            );

            AudioManager.Instance?.PlaySFXPlayerAction(AudioManager.Instance.sfxPlayerFire);

            if (hit.collider != null)
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null && !enemy.IsDead)
                {
                    if (enemy.GetSide() == playerSide)
                    {
                        enemy.TakeDamage(shotDamage, playerID);
                    }

                    return;
                }

                SpecialAbilityChest chest = hit.collider.GetComponent<SpecialAbilityChest>();
                if (chest != null)
                {
                    if (chest.GetSide() == playerSide)
                    {
                        GainSpecialAbility(chest.GetContainedAbility(), chest);
                        chest.OpenChest();
                    }

                    return;
                }
            }
        }
    }

    private void HandleSpecialAbilityActivation()
    {
        if (isGlobalCooldownActive)
        {
            Debug.Log($"Player {playerID} special key pressed but global cooldown active.");
            return;
        }

        if (Input.GetKeyDown(specialKey) && heldAbilities.Count > 0)
        {
            ActivateNextAbilityInQueue();
        }
    }

    public void ActivateNextAbilityInQueue()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver() || GameManager.Instance.IsPaused())
            return;

        if (isGlobalCooldownActive)
            return;

        if (heldAbilities.Count == 0)
            return;

        SpecialAbilityType abilityToActivate = heldAbilities.Dequeue();

        Debug.Log($"Player {playerID} activating: {abilityToActivate}. Abilities now in queue: {heldAbilities.Count}");

        isGlobalCooldownActive = true;
        globalCooldownEndTime = Time.time + globalAbilityCooldownDuration;

        if (abilitySystemUI != null)
            abilitySystemUI.UpdateAbilityVisualsFromQueue(heldAbilities, isGlobalCooldownActive);

        bool activationSuccess = true;

        switch (abilityToActivate)
        {
            case SpecialAbilityType.Stun:
                ActivateSideStun();
                break;
            case SpecialAbilityType.Slow:
                if (GameManager.Instance != null)
                    GameManager.Instance.ActivateSlowOnSide(playerSide, slowAbilityDuration, slowFactor);
                else
                    activationSuccess = false;
                break;
            case SpecialAbilityType.WeakenAttack:
                if (GameManager.Instance != null)
                    GameManager.Instance.ActivateWeakenOnSide(playerSide, weakenAttackDuration);
                else
                    activationSuccess = false;
                break;
            case SpecialAbilityType.None:
                Debug.LogWarning($"Player {playerID} tried to activate 'None' ability.");
                activationSuccess = false;
                isGlobalCooldownActive = false;
                if (abilitySystemUI != null)
                    abilitySystemUI.UpdateAbilityVisualsFromQueue(heldAbilities, isGlobalCooldownActive);
                break;
            default:
                Debug.LogWarning($"Player {playerID} unhandled ability: {abilityToActivate}");
                activationSuccess = false;
                break;
        }

        if (!activationSuccess)
            Debug.LogError($"Player {playerID} failed to activate {abilityToActivate}.");
        else
            AudioManager.Instance?.PlaySFXPlayerAction(AudioManager.Instance.sfxSpecialAbilityUse);
    }

    private void ActivateSideStun()
    {
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int affectedCount = 0;

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null && !enemy.IsDead && enemy.GetSide() == playerSide)
            {
                enemy.Stun(stunAbilityDuration);
                affectedCount++;
            }
        }

        if (affectedCount > 0)
        {
            Debug.Log($"Player {playerID} stunned {affectedCount} enemies on side {playerSide} for {stunAbilityDuration} seconds.");
        }
        else
        {
            Debug.Log($"Player {playerID} used Stun, but no enemies were found on side {playerSide}.");
        }
    }

    public void GainSpecialAbility(SpecialAbilityType abilityType, SpecialAbilityChest chest)
    {
        if (abilityType == SpecialAbilityType.None || chest.IsOpen())
            return;

        if (heldAbilities.Count < maxHeldAbilities)
        {
            heldAbilities.Enqueue(abilityType);

            if (abilitySystemUI != null)
                abilitySystemUI.UpdateAbilityVisualsFromQueue(heldAbilities, isGlobalCooldownActive);
        }
        else
        {
            Debug.Log($"Player {playerID} ability queue full ({maxHeldAbilities}).");
        }
    }

    public Queue<SpecialAbilityType> GetHeldAbilitiesQueue()
    {
        return new Queue<SpecialAbilityType>(heldAbilities);
    }

    public bool HasAbility(SpecialAbilityType abilityType)
    {
        return heldAbilities.Contains(abilityType);
    }

    public float GetGlobalCooldownRemaining()
    {
        if (!isGlobalCooldownActive) return 0f;
        return Mathf.Max(0f, globalCooldownEndTime - Time.time);
    }
}