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
    
    private Renderer tileRenderer;
    public static Tile selectedTile;
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
        if (selectedTile != this)
        {
            SetMaterial(HighlighMaterial);
        }
    }

    void OnMouseExit()
    {
        if (selectedTile != this)
        {
            tileRenderer.material = originalMaterial;
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
            currentCreatureOnTile.GetComponent<CreatureUI>()
                .ShowStats();

            CreaturePreviewManager.Instance.ShowPreview(currentCreatureOnTile.gameObject);
        }

        selectedTile = this;
        SetMaterial(SelectedMaterial);

        BlackBoard.gameManager.ClickOnTile(this);

        //FindAnyObjectByType<Unit>().Moveto(transform.position, gridPosition);

    }

}
