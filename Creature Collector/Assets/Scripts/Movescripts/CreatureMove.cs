using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CreatureMove : ScriptableObject
{
    public string moveName;
    [TextArea] public string description;
    public int actionCost = 1;

    // false = executes immediately on selection, no tile click needed
    public bool requiresTarget = true;

    public virtual List<Tile> GetValidTargetTiles(Creature user) => new List<Tile>();

    public abstract void Execute(Creature user, Tile targetTile, Creature targetCreature);

    // Per-move animation hook, no-op by default
    public virtual IEnumerator PlayAnimation(Creature user, Tile targetTile, Creature targetCreature) { yield break; }
}