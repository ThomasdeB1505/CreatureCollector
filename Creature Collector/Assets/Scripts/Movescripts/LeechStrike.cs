using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/LeechStrike")]
public class LeechStrikeMove : CreatureMove
{
    public int damage = 20;

    void Reset()
    {
        moveName = "Leech Strike";
        description = "Strike an enemy and heal for half the damage dealt.";
    }

    public override List<Tile> GetValidTargetTiles(Creature user)
    {
        List<Tile> result = new List<Tile>();
        List<Tile> inRange = BlackBoard.gridManager.GetTilesInRange(user.currentTile, user.attackMinRange, user.attackRange);
        foreach (Tile t in inRange)
        {
            if (t.currentCreatureOnTile != null && !t.currentCreatureOnTile.dead && t.currentCreatureOnTile.assignedPlayer != user.assignedPlayer)
                result.Add(t);
        }
        return result;
    }

    public override void Execute(Creature user, Tile targetTile, Creature targetCreature)
    {
        if (targetCreature == null) return;

        targetCreature.TakeDamage(damage);

        user.health += damage / 2;
        user.health = Mathf.Min(user.health, user.maxHealth);
        user.RefreshHealthUI();
    }
}