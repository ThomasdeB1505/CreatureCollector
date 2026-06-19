using UnityEngine;

public abstract class CreatureMove : ScriptableObject
{
    public string moveName;
    public abstract void Execute(Creature user, Creature target);
}