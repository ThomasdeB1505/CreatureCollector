using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/DefensiveStance")]
public class DefensiveStanceMove : CreatureMove
{
    void Reset()
    {
        moveName = "Defensive Stance";
        description = "Brace defensively until your next turn, taking reduced damage and occupying an extra adjacent tile.";
    }

    public override List<Tile> GetValidTargetTiles(Creature user)
    {
        List<Tile> result = new List<Tile>();
        GridManager gm = BlackBoard.gridManager;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int d in dirs)
        {
            Tile t = gm.GetTileAt(user.gridPosition + d);
            if (t != null && t.currentCreatureOnTile == null && t.currentObstacle == null && !t.blocked)
                result.Add(t);
        }
        return result;
    }

    public override void Execute(Creature user, Tile targetTile, Creature targetCreature)
    {
        if (targetTile == null) return;
        user.EnterDefensiveStance(targetTile);
    }
}