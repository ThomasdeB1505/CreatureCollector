using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to a canvas panel that contains two "evolution option" buttons.
/// Wire up all fields in the Inspector.
/// 
/// Hierarchy suggestion:
///   EvolutionChoicePanel (this script)
///     ├── OptionA_Button
///     │     ├── Image (OptionAImage)
///     │     └── TextMeshProUGUI (OptionAText)
///     └── OptionB_Button
///           ├── Image (OptionBImage)
///           └── TextMeshProUGUI (OptionBText)
/// </summary>
public class EvolutionChoiceUI : MonoBehaviour
{
    [Header("Option A")]
    public Button optionAButton;
    public Image optionAImage;
    public TMP_Text optionAText;

    [Header("Option B")]
    public Button optionBButton;
    public Image optionBImage;
    public TMP_Text optionBText;

    [Header("Cancel")]
    [Tooltip("Optional cancel button — hides the panel without evolving.")]
    public Button cancelButton;

    // ── data set by GameManager before showing ──────────────────────────────
    private GameObject _prefabA;
    private GameObject _prefabB;

    private void Awake()
    {
        // Wire button listeners once
        optionAButton.onClick.AddListener(() => Confirm(_prefabA));
        optionBButton.onClick.AddListener(() => Confirm(_prefabB));
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Hide);

        gameObject.SetActive(false);   // hidden by default
    }

    /// <summary>
    /// Called by GameManager to populate and show the panel.
    /// Any parameter can be null — that option's button will be disabled.
    /// </summary>
    public void Show(
        GameObject prefabA, Sprite spriteA, string labelA,
        GameObject prefabB, Sprite spriteB, string labelB)
    {
        _prefabA = prefabA;
        _prefabB = prefabB;

        SetupOption(optionAButton, optionAImage, optionAText, prefabA, spriteA, labelA);
        SetupOption(optionBButton, optionBImage, optionBText, prefabB, spriteB, labelB);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ── private helpers ──────────────────────────────────────────────────────

    void SetupOption(Button btn, Image img, TMP_Text label,
                     GameObject prefab, Sprite sprite, string text)
    {
        bool valid = prefab != null;
        btn.interactable = valid;

        if (img != null) img.sprite = sprite;
        if (label != null) label.text = valid ? text : string.Empty;
    }

    void Confirm(GameObject chosenPrefab)
    {
        Hide();
        if (chosenPrefab != null)
            BlackBoard.gameManager.ExecuteEvolution(chosenPrefab);
    }
}