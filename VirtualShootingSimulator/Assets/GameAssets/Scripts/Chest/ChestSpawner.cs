using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class ChestSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct AbilityWeight
    {
        public SpecialAbilityType abilityType;
        [Tooltip("Relative chance for this ability to spawn. Higher values mean higher chance.")]
        [Range(0.1f, 100f)]
        public float weight;
    }

    [System.Serializable]
    public struct AbilityVisuals
    {
        public SpecialAbilityType abilityType;
        public Sprite closedChestSprite;
        public Sprite openedChestSprite;
    }

    [Header("Chest Settings")]
    [Tooltip("Chest prefab to be spawned. Must have SpecialAbilityChest, Collider2D, and NavMeshObstacle components.")]
    [SerializeField] private GameObject chestPrefab;
    [Tooltip("Minimum waiting time after a chest spawns before trying to spawn another.")]
    [SerializeField] private float minSpawnDelay = 3f;
    [Tooltip("Maximum waiting time after a chest spawns before trying to spawn another.")]
    [SerializeField] private float maxSpawnDelay = 6f;
    [Tooltip("Max chests allowed *per side* (if MP) or total (if SP).")]
    [SerializeField] private int maxChestsPerSideLimit = 2;

    [Header("Ability Probabilities & Visuals")]
    [Tooltip("Define the weighted chance and visuals for each ability. Total weight doesn't need to be 100.")]
    [SerializeField] private List<AbilityWeight> abilityWeights;
    [Tooltip("Define the closed and opened sprites for each ability type.")]
    [SerializeField] private List<AbilityVisuals> abilityVisuals;

    [Header("Spawn Points & Player References")]
    [Tooltip("Spawn points for the left side player.")]
    [SerializeField] private List<Transform> leftSpawnPoints;
    [Tooltip("PlayerController for the left side.")]
    [SerializeField] private PlayerController playerLeftController;

    [Tooltip("Spawn points for the right side player (MP only).")]
    [SerializeField] private List<Transform> rightSpawnPoints;
    [Tooltip("PlayerController for the right side (MP only).")]
    [SerializeField] private PlayerController playerRightController;

    private Dictionary<Transform, GameObject> occupiedLeftSpawnPoints = new Dictionary<Transform, GameObject>();
    private Dictionary<Transform, GameObject> occupiedRightSpawnPoints = new Dictionary<Transform, GameObject>();
    private float totalAbilityWeight = 0f;
    private GameModeType currentMode;

    private Dictionary<SpecialAbilityType, AbilityVisuals> visualsMap;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            currentMode = GameManager.Instance.currentGameMode;
        }
        else
        {
            Debug.LogError("ChestSpawner cannot determine Game Mode! GameManager not found.", this);
            enabled = false;
            return;
        }

        if (chestPrefab == null)
        {
            Debug.LogError("ChestSpawner missing required references (Prefab or Left Spawn Area)! Disabling.", this);
            enabled = false;
            return;
        }

        if (leftSpawnPoints == null || leftSpawnPoints.Count == 0 || playerLeftController == null)
        {
            Debug.LogError("ChestSpawner: Left Spawn Points or PlayerLeftController not assigned!", this);
            enabled = false;
            return;
        }

        if (currentMode == GameModeType.Multiplayer &&
           (rightSpawnPoints == null || rightSpawnPoints.Count == 0 || playerRightController == null))
        {
            Debug.LogError("ChestSpawner: Right Spawn Points or PlayerRightController not assigned (Required for MP)!", this);
            enabled = false;
            return;
        }

        if (chestPrefab.GetComponent<NavMeshObstacle>() == null ||
            chestPrefab.GetComponent<SpecialAbilityChest>() == null ||
            chestPrefab.GetComponent<Collider2D>() == null ||
            chestPrefab.GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogError("ChestSpawner has missing references or prefab components! Disabling.", this);
            enabled = false;
            return;
        }

        if (abilityWeights == null || abilityWeights.Count == 0)
        {
            Debug.LogError("ChestSpawner has no Ability Weights defined! Disabling.", this);
            enabled = false;
            return;
        }

        totalAbilityWeight = abilityWeights.Sum(aw => aw.weight);

        if (totalAbilityWeight <= 0)
        {
            Debug.LogError("Total weight of Ability Weights in ChestSpawner is zero or negative! Disabling.", this);
            enabled = false;
            return;
        }

        visualsMap = new Dictionary<SpecialAbilityType, AbilityVisuals>();

        if (abilityVisuals != null)
        {
            foreach (var v in abilityVisuals)
                visualsMap[v.abilityType] = v;
        }
        else
        {
            Debug.LogError("AbilityVisuals list is not assigned in ChestSpawner!", this);
            enabled = false; return;
        }

        foreach (var aw in abilityWeights)
        {
            if (!visualsMap.ContainsKey(aw.abilityType))
            {
                Debug.LogError($"No visual defined for weighted ability: {aw.abilityType}. Chests might not have correct sprites.", this);
            }
        }

        StartCoroutine(SpawnRoutineForSide(GameSide.Left, leftSpawnPoints, playerLeftController, occupiedLeftSpawnPoints));
        if (currentMode == GameModeType.Multiplayer)
        {
            StartCoroutine(SpawnRoutineForSide(GameSide.Right, rightSpawnPoints, playerRightController, occupiedRightSpawnPoints));
        }
    }

    private IEnumerator SpawnRoutineForSide(GameSide side, List<Transform> spawnPointsForSide, PlayerController relevantPlayer, Dictionary<Transform, GameObject> occupiedPointsForSide)
    {
        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
                yield break;

            var keysToRemove = occupiedPointsForSide.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                occupiedPointsForSide.Remove(key);
            }

            if (occupiedPointsForSide.Count < maxChestsPerSideLimit)
            {
                List<Transform> availablePoints = spawnPointsForSide.Where(p => p != null && !occupiedPointsForSide.ContainsKey(p)).ToList();
                if (availablePoints.Count > 0)
                {
                    Transform spawnPoint = availablePoints[Random.Range(0, availablePoints.Count)];
                    SpecialAbilityType abilityToSpawn = GetRandomWeightedAbilityForPlayer(relevantPlayer);

                    if (abilityToSpawn != SpecialAbilityType.None)
                    {
                        SpawnChestAtPoint(spawnPoint, side, abilityToSpawn, occupiedPointsForSide);
                    }
                }
            }

            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);
        }
    }

    private void SpawnChestAtPoint(Transform spawnPoint, GameSide assignedSide, SpecialAbilityType ability, Dictionary<Transform, GameObject> occupiedPointsForSide)
    {
        if (occupiedPointsForSide.ContainsKey(spawnPoint))
            return;

        GameObject chestInstance = Instantiate(chestPrefab, spawnPoint.position, spawnPoint.rotation, transform);
        chestInstance.name = $"Chest_{assignedSide}_{ability}_{spawnPoint.name}";

        SpecialAbilityChest chestScript = chestInstance.GetComponent<SpecialAbilityChest>();
        if (chestScript != null)
        {
            chestScript.AssignSide(assignedSide);
            chestScript.SetContainedAbility(ability);

            if (visualsMap.TryGetValue(ability, out AbilityVisuals visuals))
            {
                chestScript.SetVisuals(visuals.closedChestSprite, visuals.openedChestSprite);
            }
            else
            {
                Debug.LogWarning($"No visuals for {ability}");
            }
        }

        NavMeshObstacle obstacle = chestInstance.GetComponent<NavMeshObstacle>();
        if (obstacle != null && !obstacle.enabled)
            obstacle.enabled = true;

        occupiedPointsForSide[spawnPoint] = chestInstance;
    }

    private SpecialAbilityType GetRandomWeightedAbilityForPlayer(PlayerController player)
    {
        if (totalAbilityWeight <= 0 || player == null) return SpecialAbilityType.None;

        List<AbilityWeight> availableToSpawnWeights = new List<AbilityWeight>();
        float currentTotalAvailableWeight = 0f;

        foreach (AbilityWeight aw in abilityWeights)
        {
            if (!player.HasAbility(aw.abilityType))
            {
                availableToSpawnWeights.Add(aw);
                currentTotalAvailableWeight += aw.weight;
            }
        }

        if (availableToSpawnWeights.Count == 0 || currentTotalAvailableWeight <= 0)
        {
            return SpecialAbilityType.None;
        }

        float randomValue = Random.Range(0f, currentTotalAvailableWeight);
        float cumulativeWeight = 0f;

        foreach (AbilityWeight abilityWeight in availableToSpawnWeights)
        {
            cumulativeWeight += abilityWeight.weight;
            if (randomValue <= cumulativeWeight)
            {
                return abilityWeight.abilityType;
            }
        }
        return availableToSpawnWeights.Count > 0 ? availableToSpawnWeights.Last().abilityType : SpecialAbilityType.None;
    }
}