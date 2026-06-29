using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public int health = 20;
    public Tile currentTile;

    public void Initialize(Tile tile)
    {
        currentTile = tile;
        tile.currentObstacle = this;
        transform.position = tile.transform.position;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
            DestroyObstacle();
    }

    void DestroyObstacle()
    {
        if (currentTile != null)
            currentTile.currentObstacle = null;
        Destroy(gameObject);
    }
}