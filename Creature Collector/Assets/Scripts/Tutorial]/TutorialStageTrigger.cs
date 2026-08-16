using UnityEngine;

/// <summary>
/// Attach this to each stage's Canvas GameObject (CraftingCanvas, EnemySelectCanvas, CombatCanvas).
/// When Unity enables that Canvas (i.e. you switch to that stage), it tells the
/// TutorialManager to play the matching sequence.
///
/// Set "Stage Id" in the Inspector to match a stageId you configured on TutorialManager,
/// e.g. "Crafting", "EnemySelect", "Combat".
/// </summary>
public class TutorialStageTrigger : MonoBehaviour
{
    [Tooltip("Must match a TutorialSequence.stageId in TutorialManager")]
    public string stageId;

    private void OnEnable()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StartSequence(stageId);
        }
    }
}
