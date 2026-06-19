using UnityEngine;

[CreateAssetMenu(menuName = "Creature/Moves/GolemSmash")]
public class GolemSmash : CreatureMove
{
    public override void Execute(Creature user, Creature target)
    {
        target.TakeDamage(30);
    }
}