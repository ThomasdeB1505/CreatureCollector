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

    private Renderer tileRenderer;
    public static Tile selectedTile;
    public bool inMoveRange = false;

    public Creature currentCreatureOnTile;

    private void Start()
    {
        tileRenderer = GetComponentInChildren<Renderer>();
    }

    public void ChangeColor(Color newColor)
    {
        tileRenderer.material.color = newColor;
    }

    void OnMouseEnter()
    {
        if (selectedTile != this)
        {
            ChangeColor(highlightColor);

            if(currentCreatureOnTile != null)
            {
                //reference UI
                
            }
        }
    }

    void OnMouseExit()
    {
        if (selectedTile != this)
        {
            ChangeColor(originalColor);
        }

    }

    void OnMouseDown()
    {
        if (selectedTile)
        {
            selectedTile.ChangeColor(selectedTile.originalColor);
        }

        selectedTile = this;
        ChangeColor(selectedColor);

        BlackBoard.gameManager.ClickOnTile(this);

        //FindAnyObjectByType<Unit>().Moveto(transform.position, gridPosition);
    }
}
