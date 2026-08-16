using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/PoisonLineMove")]
public class PoisonLineMove : CreatureMove
{
    void Reset()
    {
        moveName = "Poison Line";
        description = "Poison every tile in a straight line until it hits an obstacle. Any creature standing on or stepping onto a poisoned tile takes 1 damage.";
    }

    public override List<Tile> GetValidTargetTiles(Creature user)
    {
        List<Tile> result = new List<Tile>();
        GridManager gm = BlackBoard.gridManager;

        // Positive X direction
        for (int x = user.gridPosition.x + 1; x < gm.width; x++)
        {
            Tile t = gm.GetTileAt(new Vector2Int(x, user.gridPosition.y));
            if (t == null) break;
            result.Add(t);
            if (t.currentObstacle != null) break;
        }
        // Negative X direction
        for (int x = user.gridPosition.x - 1; x >= 0; x--)
        {
            Tile t = gm.GetTileAt(new Vector2Int(x, user.gridPosition.y));
            if (t == null) break;
            result.Add(t);
            if (t.currentObstacle != null) break;
        }
        // Positive Y direction
        for (int y = user.gridPosition.y + 1; y < gm.height; y++)
        {
            Tile t = gm.GetTileAt(new Vector2Int(user.gridPosition.x, y));
            if (t == null) break;
            result.Add(t);
            if (t.currentObstacle != null) break;
        }
        // Negative Y direction
        for (int y = user.gridPosition.y - 1; y >= 0; y--)
        {
            Tile t = gm.GetTileAt(new Vector2Int(user.gridPosition.x, y));
            if (t == null) break;
            result.Add(t);
            if (t.currentObstacle != null) break;
        }

        return result;
    }

    public override void Execute(Creature user, Tile targetTile, Creature targetCreature)
    {
        GridManager gm = BlackBoard.gridManager;
        bool horizontalLine = targetTile.gridPosition.y == user.gridPosition.y;
        bool positiveDirection = horizontalLine
            ? targetTile.gridPosition.x > user.gridPosition.x
            : targetTile.gridPosition.y > user.gridPosition.y;

        if (horizontalLine)
        {
            int step = positiveDirection ? 1 : -1;
            for (int x = user.gridPosition.x + step; x >= 0 && x < gm.width; x += step)
            {
                Tile t = gm.GetTileAt(new Vector2Int(x, user.gridPosition.y));
                if (t == null) break;
                PoisonTile(t, user);
                if (t.currentObstacle != null) break;
            }
        }
        else
        {
            int step = positiveDirection ? 1 : -1;
            for (int y = user.gridPosition.y + step; y >= 0 && y < gm.height; y += step)
            {
                Tile t = gm.GetTileAt(new Vector2Int(user.gridPosition.x, y));
                if (t == null) break;
                PoisonTile(t, user);
                if (t.currentObstacle != null) break;
            }
        }
    }

    void PoisonTile(Tile t, Creature user)
    {
        if (t == null) return;
        t.ApplyPoison(1);
    }
}