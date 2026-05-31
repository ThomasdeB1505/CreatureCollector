using UnityEngine;

public class Golem : Creature
{
    public override void Attack(Creature _target)
    {
        //golem attack friendlies too!! oh nooo
        _target.TakeDamage(attackDamage);
    }

    public override void Die()
    {
        //spawn little golems

        base.Die();
    }
}
