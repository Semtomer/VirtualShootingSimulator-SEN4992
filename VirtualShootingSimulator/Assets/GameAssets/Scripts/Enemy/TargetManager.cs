using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    [Tooltip("Available target points that enemies can move towards.")]
    [SerializeField] private List<Transform> availableTargets = new List<Transform>();
    [Tooltip("Points currently targeted (occupied) by an enemy.")]
    [SerializeField] private List<Transform> occupiedTargets = new List<Transform>();

    private void Awake()
    {
        availableTargets.Clear();
        foreach (Transform child in transform)
        {
            if (child != this.transform)
            {
                availableTargets.Add(child);
            }
        }
        if (availableTargets.Count == 0)
        {
            Debug.LogWarning("TargetManager found no child objects to use as targets.");
        }
    }

    public Transform GetRandomAvailableTarget()
    {
        if (availableTargets.Count == 0)
        {
            return null;
        }

        int attempts = 0;
        int maxAttempts = availableTargets.Count * 2;

        while (attempts < maxAttempts)
        {
            Transform selectedTarget = availableTargets[Random.Range(0, availableTargets.Count)];
            if (TryOccupyTarget(selectedTarget))
            {
                return selectedTarget;
            }
            attempts++;
        }

        Debug.LogError("TargetManager failed to occupy a target after multiple attempts. Possible list corruption?");
        return null;
    }

    public bool TryOccupyTarget(Transform target)
    {
        if (target != null && availableTargets.Contains(target))
        {
            availableTargets.Remove(target);
            occupiedTargets.Add(target);
            return true;
        }
        return false;
    }

    public void ReleaseTarget(Transform target)
    {
        if (target != null && occupiedTargets.Contains(target))
        {
            bool removed = occupiedTargets.Remove(target);
            if (!availableTargets.Contains(target))
            {
                availableTargets.Add(target);
            }
        }
        else
        {
            if (target == null) Debug.LogWarning("ReleaseTarget called with null target.");
            else Debug.LogWarning($"Target '{target.name}' not found in occupiedTargets or already released.");
        }
    }
}