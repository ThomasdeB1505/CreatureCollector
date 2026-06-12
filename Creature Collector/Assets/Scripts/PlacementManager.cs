using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;

    public GameObject placementPanel;
    public TextMeshProUGUI instructionText;

    private GameObject[] pendingPrefabs;
    private int placedCount = 0;
    private int gridWidth, gridHeight;
    private Action onComplete;
    private bool isPlacing = false;

    public bool IsPlacing => isPlacing;
    public UnityEngine.UI.Image creaturePreviewImage;
    public TextMeshProUGUI creatureNameText;

    void Awake() => Instance = this;

    public void StartPlacement(GameObject[] prefabs, int width, int height, Action onDone)
    {
        pendingPrefabs = prefabs;
        placedCount = 0;
        gridWidth = width;
        gridHeight = height;
        onComplete = onDone;
        isPlacing = true;

        placementPanel.SetActive(true);
        HighlightPlacementZone();
        UpdateInstruction();
    }

    public void HighlightPlacementZone()
    {
        // Highlight column 0 (player 1's side) with available tiles
        for (int y = 0; y < gridHeight; y++)
        {
            Tile t = BlackBoard.gridManager.map[0, y];
            if (t.currentCreatureOnTile == null)
                t.SetMaterial(t.HighlighMaterial);
        }
    }

    void UpdateInstruction()
    {
        instructionText.text = $"Place creature {placedCount + 1} of {pendingPrefabs.Length} — click a highlighted tile";

        Creature c = pendingPrefabs[placedCount].GetComponent<Creature>();
        if (c != null)
        {
            creatureNameText.text = c.name.Replace("(Clone)", "").Trim();
            creaturePreviewImage.sprite = c.portrait;
            creaturePreviewImage.enabled = c.portrait != null;
        }
    }

    // Called from GameManager.ClickOnTile when IsPlacing == true
    public void HandleTileClick(Tile tile)
    {
        if (!isPlacing) return;

        // Only allow column 0
        if (tile.gridPosition.x != 0) return;
        if (tile.currentCreatureOnTile != null) return;

        GameObject prefab = pendingPrefabs[placedCount];
        Creature c = Instantiate(prefab).GetComponent<Creature>();
        c.assignedPlayer = 0;
        c.Initialize(tile);

        placedCount++;

        if (placedCount >= pendingPrefabs.Length)
            FinishPlacement();
        else
        {
            HighlightPlacementZone(); // refresh to exclude newly occupied tile
            UpdateInstruction();
        }
    }

    void FinishPlacement()
    {
        isPlacing = false;
        placementPanel.SetActive(false);
        BlackBoard.gridManager.ResetGridHighlights();
        onComplete?.Invoke();
    }
}