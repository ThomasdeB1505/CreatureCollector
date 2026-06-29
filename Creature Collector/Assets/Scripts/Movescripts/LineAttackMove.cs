using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/LineAttack")]
public class LineAttackMove : CreatureMove
{
    public int damage = 20;

    void Reset()
    {
        moveName = "Line Attack";
        description = "Strike every creature in a straight line across the whole grid, friend or foe.";
    }

    public override List<Tile> GetValidTargetTiles(Creature user)
    {
        List<Tile> result = new List<Tile>();
        GridManager gm = BlackBoard.gridManager;

        for (int x = 0; x < gm.width; x++)
        {
            if (x == user.gridPosition.x) continue;
            Tile t = gm.GetTileAt(new Vector2Int(x, user.gridPosition.y));
            if (t != null) result.Add(t);
        }
        for (int y = 0; y < gm.height; y++)
        {
            if (y == user.gridPosition.y) continue;
            Tile t = gm.GetTileAt(new Vector2Int(user.gridPosition.x, y));
            if (t != null) result.Add(t);
        }
        return result;
    }

    public override void Execute(Creature user, Tile targetTile, Creature targetCreature)
    {
        GridManager gm = BlackBoard.gridManager;
        bool horizontalLine = targetTile.gridPosition.y == user.gridPosition.y;

        if (horizontalLine)
        {
            for (int x = 0; x < gm.width; x++)
                HitTile(gm.GetTileAt(new Vector2Int(x, user.gridPosition.y)), user);
        }
        else
        {
            for (int y = 0; y < gm.height; y++)
                HitTile(gm.GetTileAt(new Vector2Int(user.gridPosition.x, y)), user);
        }
    }

    void HitTile(Tile t, Creature user)
    {
        if (t == null) return;
        if (t.currentCreatureOnTile != null && t.currentCreatureOnTile != user && !t.currentCreatureOnTile.dead)
            t.currentCreatureOnTile.TakeDamage(damage);
        if (t.currentObstacle != null)
            t.currentObstacle.TakeDamage(damage);
    }
}