using UnityEngine;

public class Unit : MonoBehaviour
{
    public Vector2Int gridPosition;
    public Tile currentTile;
    public float moveSpeed = 5f;
    private bool isMoving = false;
    private Vector3 targetPosition;

    private const float STOPPING_DISTANCE = 0.01f;

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
    }

    public virtual void Moveto(Vector3 position, Tile _tile)
    {
        currentTile.currentCreatureOnTile = null;
        targetPosition = position;
        gridPosition = _tile.gridPosition;
        currentTile = _tile;
        isMoving = true;
    }

    private void HandleMovement()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < STOPPING_DISTANCE)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }
}
