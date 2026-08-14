using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int gridWidth;
    public int gridHeight;
    public int playerCreatureCount;
    public EncounterOption[] encounterOptions; // always length 2
}

[System.Serializable]
public class EncounterOption
{
    public string encounterName;
    public Sprite previewSprite;
    public GameObject[] enemyCreaturePrefabs;
    public FormType formReward; // NEW - the Form the player receives on completing this encounter
}