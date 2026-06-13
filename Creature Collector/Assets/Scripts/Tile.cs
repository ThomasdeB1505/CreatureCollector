using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;
    //change selection colors
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
        {
            CreaturePreviewManager.Instance.ShowPreview(currentCreatureOnTile.gameObject); // ADD
            currentCreatureOnTile.GetComponent<CreatureUI>().ShowStats();                  // ADD

            if (BlackBoard.gameManager.GetSelectedCreature() == null)
            {
                BlackBoard.gridManager.HighlightMoveRange(this, currentCreatureOnTile.moveRange);
                BlackBoard.gridManager.HighlightAttackRange(this, currentCreatureOnTile.moveRange, currentCreatureOnTile.attackRange);
            }
        }
    }

    void OnMouseExit()
    {
        BlackBoard.gameManager.RefreshHighlights();

        if (selectedTile != this)
            tileRenderer.material = originalMaterial; // Runs first

        if (PlacementManager.Instance.IsPlacing)
            PlacementManager.Instance.HighlightPlacementZone(); // Now overwrites correctly

        if (hoveredTile != null && hoveredTile.currentCreatureOnTile != null
            && BlackBoard.gameManager.GetSelectedCreature() == null)
        {
            BlackBoard.gridManager.HighlightMoveRange(hoveredTile, hoveredTile.currentCreatureOnTile.moveRange);
            BlackBoard.gridManager.HighlightAttackRange(hoveredTile, hoveredTile.currentCreatureOnTile.moveRange, hoveredTile.currentCreatureOnTile.attackRange);
        }
    }

    void OnMouseDown()
    {
        if (selectedTile)
        {
            selectedTile.tileRenderer.material = selectedTile.originalMaterial;
        }
        if (currentCreatureOnTile != null)
        {
           // currentCreatureOnTile.GetComponent<CreatureUI>()
           //     .ShowStats();

           // CreaturePreviewManager.Instance.ShowPreview(currentCreatureOnTile.gameObject);
        }

        selectedTile = this;
        SetMaterial(SelectedMaterial);

        BlackBoard.gameManager.ClickOnTile(this);

        //FindAnyObjectByType<Unit>().Moveto(transform.position, gridPosition);

    }

}
