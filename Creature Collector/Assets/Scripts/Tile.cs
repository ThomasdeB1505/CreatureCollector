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

    private Renderer tileRenderer;
    public static Tile selectedTile;
    public bool inMoveRange = false;

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

        FindAnyObjectByType<Unit>().Moveto(transform.position, gridPosition);
    }
}
