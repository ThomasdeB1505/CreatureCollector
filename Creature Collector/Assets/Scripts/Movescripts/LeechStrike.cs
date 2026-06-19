using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/LeechStrike")]
public class LeechStrike : CreatureMove
{
    public int damage = 20;

    public override void Execute(Creature user, Creature target)
    {
        target.TakeDamage(damage);
        user.health += damage / 2;
        user.health = Mathf.Min(user.health, user.maxHealth); // clamp to max
    }
}
