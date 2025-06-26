using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("The enemy prefab to be spawned.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Points where enemies can spawn.")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("The TargetManager used to assign target points to enemies.")]
    [SerializeField] private TargetManager targetManager;

    [Tooltip("The Castle instance that enemies spawned by this spawner should attack.")]
    [SerializeField] private Castle targetCastle;

    [Tooltip("The waiting time between two enemy spawns (seconds).")]
    [SerializeField] private float spawnInterval = 2f;

    [Tooltip("The side of the game area this spawner belongs to.")]
    [SerializeField] private GameSide associatedSide;

    private int enemySpawnCount = 0;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy Prefab is not assigned in EnemySpawner!");
            enabled = false;
            return;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn points are not assigned in EnemySpawner!");
            enabled = false;
            return;
        }

        if (targetManager == null)
        {
            Debug.LogError("Target Manager is not assigned in EnemySpawner!");
            enabled = false;
            return;
        }

        if (targetCastle == null)
        {
            Debug.LogError($"Target Castle is not assigned in EnemySpawner {gameObject.name}!");
            enabled = false;
            return;
        }

        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            {
                yield break;
            }

            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Transform targetPoint = targetManager.GetRandomAvailableTarget();

        if (targetPoint == null)
        {
            return;
        }

        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemySpawnCount++;
        enemyInstance.name = $"{enemyPrefab.name}_{associatedSide}_{enemySpawnCount}";

        Enemy enemyScript = enemyInstance.GetComponent<Enemy>();
        EnemyMovement movementScript = enemyInstance.GetComponent<EnemyMovement>();

        if (enemyScript != null && movementScript != null)
        {
            movementScript.SetTarget(targetPoint, targetManager);
            enemyScript.AssignTargetCastle(targetCastle);
            enemyScript.AssignSide(associatedSide);
        }
        else
        {
            Debug.LogError($"Spawned enemy {enemyInstance.name} is missing Enemy or EnemyMovement component! Check the prefab.");
            Destroy(enemyInstance);
        }
    }
}