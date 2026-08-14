using System.Collections.Generic;
using UnityEngine;

public class PlayerRoster : MonoBehaviour
{
    public static PlayerRoster Instance;

    public const int MaxActive = 3;
    public const int MaxBench = 3;
    public const int MaxTotal = MaxActive + MaxBench;

    public List<GameObject> activeTeam = new List<GameObject>();
    public List<GameObject> benchTeam = new List<GameObject>();

    public GameObject[] startingCreatures; // Assign in Inspector for Level 1

    void Awake()
    {
        Instance = this;
        // starting creature now comes from ForceInitialCraft, not a direct grant
    }

    public int TotalCount => activeTeam.Count + benchTeam.Count;
    public bool IsFull => TotalCount >= MaxTotal;

    // Adds to active first if there's room, otherwise bench. Returns false if both are full.
    public bool AddCreature(GameObject prefab)
    {
        if (activeTeam.Count < MaxActive)
        {
            activeTeam.Add(prefab);
            return true;
        }
        if (benchTeam.Count < MaxBench)
        {
            benchTeam.Add(prefab);
            return true;
        }
        return false; // roster full
    }

    public bool MoveToBench(GameObject prefab)
    {
        if (benchTeam.Count >= MaxBench) return false;
        if (!activeTeam.Remove(prefab)) return false;
        benchTeam.Add(prefab);
        return true;
    }

    public bool MoveToActive(GameObject prefab)
    {
        if (activeTeam.Count >= MaxActive) return false;
        if (!benchTeam.Remove(prefab)) return false;
        activeTeam.Add(prefab);
        return true;
    }

    public bool Swap(GameObject prefabA, GameObject prefabB)
    {
        int activeIndexA = activeTeam.IndexOf(prefabA);
        int benchIndexA = benchTeam.IndexOf(prefabA);
        int activeIndexB = activeTeam.IndexOf(prefabB);
        int benchIndexB = benchTeam.IndexOf(prefabB);

        // A is active, B is bench
        if (activeIndexA >= 0 && benchIndexB >= 0)
        {
            activeTeam[activeIndexA] = prefabB;
            benchTeam[benchIndexB] = prefabA;
            return true;
        }
        // A is bench, B is active
        if (benchIndexA >= 0 && activeIndexB >= 0)
        {
            benchTeam[benchIndexA] = prefabB;
            activeTeam[activeIndexB] = prefabA;
            return true;
        }
        return false; // same side, or one wasn't found
    }


    public bool MoveToEmptySlot(GameObject prefab, bool targetIsActive)
    {
        if (targetIsActive)
        {
            if (activeTeam.Count >= MaxActive) return false;
            if (!benchTeam.Remove(prefab)) return false;
            activeTeam.Add(prefab);
            return true;
        }
        else
        {
            if (benchTeam.Count >= MaxBench) return false;
            if (!activeTeam.Remove(prefab)) return false;
            benchTeam.Add(prefab);
            return true;
        }
    }

    public void Sell(GameObject prefab)
    {
        activeTeam.Remove(prefab);
        benchTeam.Remove(prefab);
        // gives back nothing, for now
    }

    // Returns the active team, up to 'count' entries (should normally just be all 3)
    public GameObject[] GetCreaturesForLevel(int count)
    {
        var result = new GameObject[Mathf.Min(count, activeTeam.Count)];
        for (int i = 0; i < result.Length; i++)
            result[i] = activeTeam[i];
        return result;
    }
}