using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/Split")]
public class SplitMove : CreatureMove
{
    void Reset()
    {
        moveName = "Split";
        description = "Split into two creatures, each taking half of the current health.";
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

        GameObject prefab = user.sourcePrefab != null ? user.sourcePrefab : user.gameObject;
        Creature clone = Object.Instantiate(prefab).GetComponent<Creature>();
        clone.assignedPlayer = user.assignedPlayer;

        // Assumption: health splits evenly between both copies, rounded up - tune as needed
        int splitHealth = Mathf.Max(1, Mathf.CeilToInt(user.health / 2f));
        user.health = splitHealth;
        user.RefreshHealthUI();

        clone.health = splitHealth;
        clone.Initialize(targetTile);
    }
}