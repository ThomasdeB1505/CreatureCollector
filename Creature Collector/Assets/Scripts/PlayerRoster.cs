using System.Collections.Generic;
using UnityEngine;

public class PlayerRoster : MonoBehaviour
{
    public static PlayerRoster Instance;

    public List<GameObject> creaturePrefabs = new List<GameObject>();
    public GameObject[] startingCreatures; // Assign in Inspector for Level 1

    void Awake()
    {
        Instance = this;
        foreach (var c in startingCreatures)
            creaturePrefabs.Add(c);
    }

    public void AddCreature(GameObject prefab)
    {
        creaturePrefabs.Add(prefab);
    }

    // Returns the first 'count' creatures from the roster
    public GameObject[] GetCreaturesForLevel(int count)
    {
        var result = new GameObject[Mathf.Min(count, creaturePrefabs.Count)];
        for (int i = 0; i < result.Length; i++)
            result[i] = creaturePrefabs[i];
        return result;
    }
}