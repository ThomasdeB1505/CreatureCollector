using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;

    [Header("Colors")]
    public Color originalColor;
    public Color highlightColor = Color.yellow;
    public Color selectedColor = Color.blue;
    public Color AttackRangeColor = Color.red;
    public Material HighlighMaterial;
    public Material SelectedMaterial;
    public Material AttackRangeMaterial;
    public Material originalMaterial;
    public Material CombinedRangeMaterial;
    private Renderer tileRenderer;
    public static Tile selectedTile;
    public static Tile hoveredTile;
    public bool inMoveRange = false;
    public Creature currentCreatureOnTile;

    // Generic blocking flag - used by Defensive Stance's second occupied tile
    public bool blocked = false;
    // Obstacle occupying this tile, if any
    public Obstacle currentObstacle;

    private void Start()
    {
        tileRenderer = GetComponentInChildren<Renderer>();
        originalMaterial = tileRenderer.material;
    }

    public void SetMaterial(Material mat)
    {
        tileRenderer.material = mat;
    }

    void OnMouseEnter()
    {
        hoveredTile = this;
        BlackBoard.gameManager.RefreshHighlights();
        if (PlacementManager.Instance.IsPlacing)
            PlacementManager.Instance.HighlightPlacementZone();
        if (selectedTile != this)
            SetMaterial(HighlighMaterial);

        if (currentCreatureOnTile != null)
        {
            currentCreatureOnTile.GetComponent<CreatureUI>().ShowStats();
            if (BlackBoard.gameManager.GetSelectedCreature() == null)
            {
                BlackBoard.gridManager.HighlightMoveRange(this, currentCreatureOnTile.moveMinRange, currentCreatureOnTile.moveRange);
                BlackBoard.gridManager.HighlightAttackRange(this, currentCreatureOnTile.moveMinRange, currentCreatureOnTile.moveRange, currentCreatureOnTile.attackMinRange, currentCreatureOnTile.attackRange);
            }
        }
    }

    void OnMouseExit()
    {
        BlackBoard.gameManager.RefreshHighlights();
        if (selectedTile != this)
            tileRenderer.material = originalMaterial;
        if (PlacementManager.Instance.IsPlacing)
            PlacementManager.Instance.HighlightPlacementZone();
        if (hoveredTile != null && hoveredTile.currentCreatureOnTile != null
            && BlackBoard.gameManager.GetSelectedCreature() == null)
        {
            BlackBoard.gridManager.HighlightMoveRange(hoveredTile, hoveredTile.currentCreatureOnTile.moveMinRange, hoveredTile.currentCreatureOnTile.moveRange);
            BlackBoard.gridManager.HighlightAttackRange(hoveredTile, hoveredTile.currentCreatureOnTile.moveMinRange, hoveredTile.currentCreatureOnTile.moveRange, hoveredTile.currentCreatureOnTile.attackMinRange, hoveredTile.currentCreatureOnTile.attackRange);
        }
    }

    void OnMouseDown()
    {
        BlackBoard.gameManager.ClickOnTile(this);
    }
}