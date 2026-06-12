using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAI : MonoBehaviour
{
    public static SimpleAI Instance;

    void Awake() => Instance = this;

    public IEnumerator TakeTurn(int actionsAvailable)
    {
        yield return new WaitForSeconds(0.6f);

        var (myCreatures, enemies) = GetLivingCreatures();

        for (int i = 0; i < actionsAvailable; i++)
        {
            yield return new WaitForSeconds(0.4f);

            // Refresh lists each action (things may have died)
            (myCreatures, enemies) = GetLivingCreatures();
            if (enemies.Count == 0) break;

            bool acted = false;

            // Try to attack with any creature
            foreach (Creature mine in myCreatures)
            {
                Creature target = FindAttackTarget(mine, enemies);
                if (target != null)
                {
                    mine.Attack(target);
                    acted = true;
                    break;
                }
            }

            // If no attack possible, move the creature that can get closest to an enemy
            if (!acted)
            {
                foreach (Creature mine in myCreatures)
                {
                    Tile best = FindBestMoveTile(mine, enemies);
                    if (best != null)
                    {
                        mine.Moveto(best.transform.position, best);
                        acted = true;
                        break;
                    }
                }
            }

            if (!acted) break;
        }

        yield return new WaitForSeconds(0.5f);
        BlackBoard.gameManager.EndTurn();
    }

    (List<Creature> mine, List<Creature> enemies) GetLivingCreatures()
    {
        var mine = new List<Creature>();
        var enemies = new List<Creature>();
        foreach (var c in FindObjectsByType<Creature>(FindObjectsSortMode.None))
        {
            if (c.dead || !c.enabled) continue;
            if (c.assignedPlayer == 1) mine.Add(c);
            else enemies.Add(c);
        }
        return (mine, enemies);
    }

    Creature FindAttackTarget(Creature attacker, List<Creature> enemies)
    {
        List<Tile> inRange = BlackBoard.gridManager.GetTilesInRange(
            attacker.currentTile, attacker.attackRange);
        foreach (Tile t in inRange)
            if (t.currentCreatureOnTile != null && enemies.Contains(t.currentCreatureOnTile))
                return t.currentCreatureOnTile;
        return null;
    }

    Tile FindBestMoveTile(Creature mover, List<Creature> enemies)
    {
        // Find the enemy closest to us
        Creature target = null;
        float minDist = float.MaxValue;
        foreach (var e in enemies)
        {
            float d = Vector2Int.Distance(mover.gridPosition, e.gridPosition);
            if (d < minDist) { minDist = d; target = e; }
        }
        if (target == null) return null;

        List<Tile> moveable = BlackBoard.gridManager.GetTilesInRange(
            mover.currentTile, mover.moveRange);
        moveable.RemoveAll(t => t.currentCreatureOnTile != null);
        if (moveable.Count == 0) return null;

        Tile best = null;
        float bestDist = float.MaxValue;
        foreach (Tile t in moveable)
        {
            float d = Vector2Int.Distance(t.gridPosition, target.gridPosition);
            if (d < bestDist) { bestDist = d; best = t; }
        }
        return best;
    }
}