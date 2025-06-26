using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class AbilitySystemUI : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("The PlayerController this UI system belongs to.")]
    [SerializeField] private PlayerController targetPlayerController;

    [Header("Ability Slot UI Elements")]
    [Tooltip("List of Image components for the UI slots. Order determines display of FIFO queue.")]
    [SerializeField] private List<Image> abilitySlotImages;

    [System.Serializable]
    public struct AbilitySpriteMapping
    {
        public SpecialAbilityType abilityType;
        public Sprite abilitySprite;
    }

    [Header("Ability Icons")]
    [Tooltip("Assign the UI icon for each possible special ability type.")]
    [SerializeField] private List<AbilitySpriteMapping> abilityIconMappings;
    [Tooltip("Sprite to show for an empty slot if no specific icon is found for 'None' or if an icon is missing.")]
    [SerializeField] private Sprite defaultEmptySlotIcon;
    private Dictionary<SpecialAbilityType, Sprite> iconLookup = new Dictionary<SpecialAbilityType, Sprite>();

    private Color opaqueColor = Color.white;
    private Color transparentColor = new Color(1f, 1f, 1f, 0.35f);
    private Color cooldownEffectColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);

    private void Awake()
    {
        if (targetPlayerController == null)
        {
            Debug.LogError("AbilitySystemUI: targetPlayerController not assigned!", this);
            enabled = false;
            return;
        }

        if (abilitySlotImages == null || abilitySlotImages.Count == 0)
        {
            Debug.LogError("AbilitySystemUI: No ability slots configured!", this);
            enabled = false;
            return;
        }

        foreach (var mapping in abilityIconMappings)
        {
            if (!iconLookup.ContainsKey(mapping.abilityType))
            {
                iconLookup.Add(mapping.abilityType, mapping.abilitySprite);
            }
        }
    }

    private void Start()
    {
        InitializeAbilitySlotsUI();

        if (targetPlayerController != null)
        {
            UpdateAbilityVisualsFromQueue(targetPlayerController.GetHeldAbilitiesQueue(), false);
        }
    }

    private void Update()
    {
        if (targetPlayerController == null || GameManager.Instance == null ||
            (GameManager.Instance.CurrentState != GameState.Playing &&
             GameManager.Instance.CurrentState != GameState.Paused))
        {
            foreach (var slotImage in abilitySlotImages)
            {
                if (slotImage != null)
                {
                    slotImage.color = transparentColor;
                    slotImage.fillAmount = 1;
                    slotImage.raycastTarget = false;
                }
            }

            return;
        }

        bool isCooldownNow = targetPlayerController.GetGlobalCooldownRemaining() > 0;
        UpdateAbilityVisualsFromQueue(targetPlayerController.GetHeldAbilitiesQueue(), isCooldownNow);
    }

    private void InitializeAbilitySlotsUI()
    {
        for (int i = 0; i < abilitySlotImages.Count; i++)
        {
            Image slotImage = abilitySlotImages[i];
            if (slotImage != null)
            {
                slotImage.gameObject.SetActive(true);
                slotImage.sprite = defaultEmptySlotIcon;
                slotImage.color = transparentColor;
                slotImage.raycastTarget = false;
                slotImage.fillAmount = 1;
            }
        }
    }

    public void UpdateAbilityVisualsFromQueue(Queue<SpecialAbilityType> heldAbilitiesQueue, bool isGlobalCooldownActive)
    {
        if (abilitySlotImages == null) return;

        List<SpecialAbilityType> abilitiesInOrder = heldAbilitiesQueue.ToList();

        float cooldownRemaining = 0f;
        float totalCooldown = 1f;

        if (isGlobalCooldownActive && targetPlayerController != null)
        {
            cooldownRemaining = targetPlayerController.GetGlobalCooldownRemaining();
            totalCooldown = targetPlayerController.GlobalAbilityCooldownDuration;
        }

        for (int i = 0; i < abilitySlotImages.Count; i++)
        {
            Image slotImage = abilitySlotImages[i];
            if (slotImage == null || !slotImage.gameObject.activeSelf)
                continue;

            if (i < abilitiesInOrder.Count)
            {
                SpecialAbilityType ability = abilitiesInOrder[i];
                if (iconLookup.TryGetValue(ability, out Sprite abilityIcon) && abilityIcon != null)
                {
                    slotImage.sprite = abilityIcon;
                }
                else
                {
                    slotImage.sprite = defaultEmptySlotIcon;
                    if (ability != SpecialAbilityType.None) Debug.LogWarning($"No UI icon for ability: {ability}");
                }

                if (isGlobalCooldownActive)
                {
                    slotImage.color = cooldownEffectColor;
                    slotImage.raycastTarget = false;
                    slotImage.fillAmount = totalCooldown > 0 ? (totalCooldown - cooldownRemaining) / totalCooldown : 0;
                }
                else
                {
                    slotImage.color = opaqueColor;
                    slotImage.raycastTarget = false;
                    slotImage.fillAmount = 1;
                }
            }
            else
            {
                slotImage.sprite = defaultEmptySlotIcon;
                slotImage.color = transparentColor;
                slotImage.raycastTarget = false;
                slotImage.fillAmount = 1;
            }
        }
    }
}