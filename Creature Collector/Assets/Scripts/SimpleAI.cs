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

        // ── Evolve first if unlocked - simple heuristic: evolve the first
        // eligible creature that hasn't evolved yet this combat. ──────────────
        if (BlackBoard.gameManager.EvolutionUnlocked)
        {
            foreach (Creature mine in myCreatures)
            {
                if (mine.isEvolvedThisCombat) continue;

                GameObject chosen = mine.formEvolutionPrefab != null
                    ? mine.formEvolutionPrefab
                    : mine.essenceEvolutionPrefab;

                if (chosen != null)
                {
                    yield return StartCoroutine(EvolveAICreature(mine, chosen));
                    break; // one evolution per turn
                }
            }
        }

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

    // ── AI evolution execution ────────────────────────────────────────────────
    IEnumerator EvolveAICreature(Creature creature, GameObject chosenPrefab)
    {
        yield return creature.StartCoroutine(creature.PlayEvolveAnimation());

        Tile tile = creature.currentTile;
        int player = creature.assignedPlayer;
        GameObject sourcePrefab = creature.sourcePrefab;

        tile.currentCreatureOnTile = null;
        Object.Destroy(creature.gameObject);

        Creature evolved = Object.Instantiate(chosenPrefab).GetComponent<Creature>();
        evolved.assignedPlayer = player;
        evolved.isEvolvedThisCombat = true;
        evolved.sourcePrefab = sourcePrefab;
        evolved.Initialize(tile);
    }

    // -----------------------------------------------------------------------
    // Danger zone, attack targeting, movement scoring, helpers - unchanged
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

        if (target.health <= attacker.attackDamage)
            score += 1000;

        score -= target.health;

        if (!dangerZones.Contains(attacker.gridPosition))
            score += 100;

        if (dangerZones.Contains(attacker.currentTile.gridPosition))
            score += 200;

        return score;
    }

    (Tile tile, float score) FindBestMoveTile(
        Creature mover, List<Creature> enemies,
        HashSet<Vector2Int> dangerZones, int actionsAfterThisMove)
    {
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
                    score += 60f;
                else
                    score -= 60f;
            }

            if (score > bestScore) { bestScore = score; best = t; }
        }

        return (best, bestScore);
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
}