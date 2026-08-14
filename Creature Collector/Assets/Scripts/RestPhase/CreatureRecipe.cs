using UnityEngine;

[CreateAssetMenu(fileName = "CreatureRecipe", menuName = "Crafting/Creature Recipe")]
public class CreatureRecipe : ScriptableObject
{
    public FormType form;
    public EssenceType essence;
    public GameObject resultPrefab;
}