using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ActionUI : MonoBehaviour
{
    public static ActionUI Instance;
    [Header("Main Panel")]
    public GameObject panelRoot;
    public TextMeshProUGUI descriptionText; // shows above whichever button is hovered
    [Tooltip("Optional background panel that wraps descriptionText (Image + layout group). " +
             "Leave empty to show the text with no background, positioned directly.")]
    public RectTransform descriptionPanel;
    [Tooltip("Gap in pixels between the top of the button and the description box.")]
    public float descriptionPadding = 8f;
    public Button moveButton;
    public Button attackButton;
    public Button specialButton;
    public Button evolveButton;
    [Header("Special Moves Sub-Panel")]
    public GameObject specialMovesPanel;
    public Transform specialMovesContainer;
    public GameObject moveButtonPrefab; // a Button with a TextMeshProUGUI child
    private Creature shownCreature;
    private List<GameObject> spawnedMoveButtons = new List<GameObject>();
    void Awake()
    {
        Instance = this;
        moveButton.onClick.AddListener(() => {
            CloseSpecialMovesPanel();
            BlackBoard.gameManager.SelectActionMode(ActionMode.Move);
        });
        attackButton.onClick.AddListener(() => {
            CloseSpecialMovesPanel();
            BlackBoard.gameManager.SelectActionMode(ActionMode.Attack);
        });
        specialButton.onClick.AddListener(ToggleSpecialMovesPanel);
        evolveButton.onClick.AddListener(() => {
            CloseSpecialMovesPanel();
            BlackBoard.gameManager.TryEvolveSelected();
        });
        AddHoverDescription(moveButton.gameObject, "Move this creature within its move range.");
        AddHoverDescription(attackButton.gameObject, "Attack an enemy creature within attack range.");
        AddHoverDescription(specialButton.gameObject, "Choose one of this creature's special moves.");
        AddHoverDescription(evolveButton.gameObject, "Evolve this creature using an available evolution point.");
        Hide();
    }
    void CloseSpecialMovesPanel()
    {
        specialMovesPanel.SetActive(false);
        ClearSpecialMoveButtons();
    }
    void AddHoverDescription(GameObject buttonObj, string description)
    {
        ButtonDescriptionHover hover = buttonObj.GetComponent<ButtonDescriptionHover>();
        if (hover == null) hover = buttonObj.AddComponent<ButtonDescriptionHover>();
        hover.descriptionText = descriptionText;
        hover.description = description;
        hover.positionTarget = descriptionPanel != null ? descriptionPanel : descriptionText.rectTransform;
        hover.toggleTarget = descriptionPanel != null ? descriptionPanel.gameObject : descriptionText.gameObject;
        hover.verticalPadding = descriptionPadding;
    }
    public void Show(Creature creature)
    {
        shownCreature = creature;
        panelRoot.SetActive(true);
        specialMovesPanel.SetActive(false);
        HideDescription();
        evolveButton.interactable = !creature.isEvolved
            && BlackBoard.gameManager.deathPoints > 0
            && (creature.evolvedFormPrefab != null || creature.evolvedFormPrefabB != null);
        specialButton.interactable = creature.moves != null && creature.moves.Count > 0;
    }
    public void Hide()
    {
        shownCreature = null;
        panelRoot.SetActive(false);
        specialMovesPanel.SetActive(false);
        HideDescription();
        ClearSpecialMoveButtons();
    }
    void HideDescription()
    {
        if (descriptionPanel != null) descriptionPanel.gameObject.SetActive(false);
        else descriptionText.gameObject.SetActive(false);
    }
    void ToggleSpecialMovesPanel()
    {
        if (shownCreature == null) return;
        bool turningOn = !specialMovesPanel.activeSelf;
        specialMovesPanel.SetActive(turningOn);
        if (!turningOn) return;
        ClearSpecialMoveButtons();
        foreach (CreatureMove move in shownCreature.moves)
        {
            GameObject buttonObj = Instantiate(moveButtonPrefab, specialMovesContainer);
            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = move.moveName;
            Button btn = buttonObj.GetComponent<Button>();
            CreatureMove capturedMove = move;
            btn.onClick.AddListener(() => BlackBoard.gameManager.SelectSpecialMove(capturedMove));
            AddHoverDescription(buttonObj, move.description);
            spawnedMoveButtons.Add(buttonObj);
        }
    }
    void ClearSpecialMoveButtons()
    {
        foreach (GameObject go in spawnedMoveButtons)
            Destroy(go);
        spawnedMoveButtons.Clear();
    }
}