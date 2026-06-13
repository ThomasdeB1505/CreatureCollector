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
            (myCreatures, enemies) = GetLivingCreatures();
            if (enemies.Count == 0) break;

            HashSet<Vector2Int> dangerZones = BuildDangerZones(enemies);

            bool acted = false;

            // --- 1. Find the best attack across all my creatures ---
            Creature bestAttacker = null;
            Creature bestTarget = null;
            int bestAtkScore = int.MinValue;

            foreach (Creature mine in myCreatures)
            {
                Creature candidate = FindBestAttackTarget(mine, enemies);
                if (candidate == null) continue;

                int score = ScoreAttack(mine, candidate, dangerZones);
                if (score > bestAtkScore)
                {
                    bestAtkScore = score;
                    bestAttacker = mine;
                    bestTarget = candidate;
                }
            }

            if (bestAttacker != null)
            {
                bestAttacker.Attack(bestTarget);
                acted = true;
            }

            // --- 2. If no attack, move the creature with the best strategic tile ---
            if (!acted)
            {
                // Actions remaining after this move — used to decide whether
                // entering attack range is safe or a wasted exposure
                int actionsAfterThisMove = actionsAvailable - i - 1;

                Creature bestMover = null;
                Tile bestMoveTile = null;
                float bestMoveScore = float.MinValue;

                foreach (Creature mine in myCreatures)
                {
                    var (tile, score) = FindBestMoveTile(mine, enemies, dangerZones, actionsAfterThisMove);
                    if (tile != null && score > bestMoveScore)
                    {
                        bestMoveScore = score;
                        bestMover = mine;
                        bestMoveTile = tile;
                    }
                }

                if (bestMover != null)
                {
                    bestMover.Moveto(bestMoveTile.transform.position, bestMoveTile);
                    acted = true;
                }
            }

            if (!acted) break;
        }

        yield return new WaitForSeconds(0.5f);
        BlackBoard.gameManager.EndTurn();
    }

    // -----------------------------------------------------------------------
    // Danger zone: every tile a player-0 creature could attack this turn,
    // accounting for both their movement range AND their attack range.
    // -----------------------------------------------------------------------
    HashSet<Vector2Int> BuildDangerZones(List<Creature> enemies)
    {
        var danger = new HashSet<Vector2Int>();

        foreach (Creature enemy in enemies)
        {
            List<Tile> reachable = BlackBoard.gridManager.GetTilesInRange(
                enemy.currentTile, enemy.moveRange);
            reachable.Add(enemy.currentTile);

            foreach (Tile moveTile in reachable)
            {
                List<Tile> threatened = BlackBoard.gridManager.GetTilesInRange(
                    moveTile, enemy.attackRange);
                foreach (Tile t in threatened)
                    danger.Add(t.gridPosition);
            }
        }

        return danger;
    }

    // -----------------------------------------------------------------------
    // Attack target: prefer finishing off weak enemies (kill priority),
    // fall back to whoever has the lowest HP.
    // -----------------------------------------------------------------------
    Creature FindBestAttackTarget(Creature attacker, List<Creature> enemies)
    {
        List<Tile> inRange = BlackBoard.gridManager.GetTilesInRange(
            attacker.currentTile, attacker.attackRange);

        Creature best = null;
        int bestHP = int.MaxValue;

        foreach (Tile t in inRange)
        {
            Creature occupant = t.currentCreatureOnTile;
            if (occupant == null || !enemies.Contains(occupant)) continue;

            if (occupant.health < bestHP)
            {
                bestHP = occupant.health;
                best = occupant;
            }
        }

        return best;
    }

    int ScoreAttack(Creature attacker, Creature target, HashSet<Vector2Int> dangerZones)
    {
        int score = 0;

        // Huge bonus for a killing blow
        if (target.health <= attacker.attackDamage)
            score += 1000;

        // Prefer lower HP targets
        score -= target.health;

        // Bonus for attacking from a safe tile
        if (!dangerZones.Contains(attacker.gridPosition))
            score += 100;

        // Bonus for attacking an enemy that is already threatening us
        if (dangerZones.Contains(attacker.currentTile.gridPosition))
            score += 200;

        return score;
    }

    // -----------------------------------------------------------------------
    // Movement: approach the best target while respecting danger zones.
    // Only commit to entering attack range if we have an action left to
    // actually attack — otherwise hold at a safe distance.
    // -----------------------------------------------------------------------
    (Tile tile, float score) FindBestMoveTile(
        Creature mover, List<Creature> enemies,
        HashSet<Vector2Int> dangerZones, int actionsAfterThisMove)
    {
        // Target the lowest HP enemy to focus fire
        Creature target = null;
        int lowestHP = int.MaxValue;
        foreach (Creature e in enemies)
        {
            if (e.health < lowestHP)
            {
                lowestHP = e.health;
                target = e;
            }
        }
        if (target == null) return (null, float.MinValue);

        List<Tile> moveable = BlackBoard.gridManager.GetTilesInRange(
            mover.currentTile, mover.moveRange);
        moveable.RemoveAll(t => t.currentCreatureOnTile != null);
        if (moveable.Count == 0) return (null, float.MinValue);

        Tile best = null;
        float bestScore = float.MinValue;

        foreach (Tile t in moveable)
        {
            float distToTarget = Vector2Int.Distance(t.gridPosition, target.gridPosition);
            float distFromIdeal = Mathf.Abs(distToTarget - mover.attackRange);
            float score = -(distFromIdeal * 5f);

            bool inAttackRange = distToTarget <= mover.attackRange;
            bool inDangerZone = dangerZones.Contains(t.gridPosition);

            if (inDangerZone)
                score -= 20f;

            if (inAttackRange)
            {
                if (actionsAfterThisMove > 0)
                    // We can follow up with an attack — moving here is worthwhile
                    score += 60f;
                else
                    // No actions left to attack; entering range just eats a free hit
                    score -= 60f;
            }

            if (score > bestScore) { bestScore = score; best = t; }
        }

        return (best, bestScore);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
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
}