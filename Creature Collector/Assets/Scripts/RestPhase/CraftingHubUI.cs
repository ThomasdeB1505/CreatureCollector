using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingHubUI : MonoBehaviour
{
    [Header("Root Panels")]
    public GameObject hubPanel;
    public GameObject craftPanel;
    public GameObject forcedEssencePanel;

    [Header("Hub")]
    public Button craftEntryButton;
    public TextMeshProUGUI formInventoryText;
    public TextMeshProUGUI essenceInventoryText;
    public Button doneButton;

    [Header("Craft Panel")]
    public Button[] formSelectButtons;       // 3, enum order: Attack, Defense, Support
    public TMP_Text[] formSelectCounts;
    public Button[] essenceSelectButtons;    // 3, enum order: Magic, Nature, Mechanical
    public TMP_Text[] essenceSelectCounts;
    public Image resultSilhouette;
    public TMP_Text resultLabel;
    public Button confirmCraftButton;
    public Button craftBackButton;

    private bool forcedInitialMode = false;
    private Action<EssenceType> onForcedEssenceChosen;

    [Header("Team Panel")]
    public GameObject teamPanel;
    public Button teamEntryButton;
    public Button teamBackButton;

    public Button[] activeSlotButtons;   // 3
    public Image[] activeSlotImages;
    public TMP_Text[] activeSlotLabels;

    public Button[] benchSlotButtons;    // 3
    public Image[] benchSlotImages;
    public TMP_Text[] benchSlotLabels;

    public GameObject selectedCreaturePanel; // contextual buttons for the selected creature
    public TMP_Text selectedCreatureDescription; // was selectedCreatureName
    public Button sellButton;
    public Button swapButton;
    public TMP_Text swapButtonLabel; // e.g. "Swap" / "Cancel Swap"

    private bool swapMode = false;

    private GameObject selectedPrefab;
    private bool selectedIsActive;

    [Header("Forced Initial Essence Choice")]
    public Button[] essenceChoiceButtons; // one per EssenceType, enum order
    public TMP_Text[] essenceChoiceLabels;

    private CraftingManager craftingManager;
    private Action onHubDone;

    private FormType? selectedForm;
    private EssenceType? selectedEssence;

    void Awake()
    {
        craftEntryButton.onClick.AddListener(ShowCraftPanel);
        doneButton.onClick.AddListener(CloseHub);
        confirmCraftButton.onClick.AddListener(OnConfirmCraft);
        if (craftBackButton != null)
            craftBackButton.onClick.AddListener(BackToHubFromCraft);
        teamEntryButton.onClick.AddListener(ShowTeamPanel);
        if (teamBackButton != null)
            teamBackButton.onClick.AddListener(BackToHubFromTeam);
        swapButton.onClick.AddListener(OnSwapButtonPressed);
        sellButton.onClick.AddListener(OnSellSelected);
        HideAllSubPanels();
    }

    void ShowTeamPanel()
    {
        hubPanel.SetActive(false);
        teamPanel.SetActive(true);
        selectedPrefab = null;
        selectedCreaturePanel.SetActive(false);
        RefreshTeamSlots();
    }

    void RefreshTeamSlots()
    {
        var roster = PlayerRoster.Instance;

        for (int i = 0; i < activeSlotButtons.Length; i++)
        {
            bool occupied = i < roster.activeTeam.Count;
            GameObject prefab = occupied ? roster.activeTeam[i] : null;
            SetupSlot(activeSlotButtons[i], activeSlotImages[i], activeSlotLabels[i], prefab, isActive: true);
        }

        for (int i = 0; i < benchSlotButtons.Length; i++)
        {
            bool occupied = i < roster.benchTeam.Count;
            GameObject prefab = occupied ? roster.benchTeam[i] : null;
            SetupSlot(benchSlotButtons[i], benchSlotImages[i], benchSlotLabels[i], prefab, isActive: false);
        }
    }

    void SetupSlot(Button btn, Image img, TMP_Text label, GameObject prefab, bool isActive)
    {
        btn.onClick.RemoveAllListeners();

        if (prefab == null)
        {
            img.gameObject.SetActive(false);
            label.text = "Empty";
            btn.onClick.AddListener(() => SelectEmptySlot(isActive));
            return;
        }

        Creature c = prefab.GetComponent<Creature>();
        img.gameObject.SetActive(true);
        img.sprite = c != null ? c.portrait : null;
        label.text = prefab.name.Replace("(Clone)", "").Trim();

        btn.onClick.AddListener(() => SelectSlot(prefab, isActive));
    }

    void SelectEmptySlot(bool isActive)
    {
        if (!swapMode || selectedPrefab == null) return;

        bool success = PlayerRoster.Instance.MoveToEmptySlot(selectedPrefab, isActive);
        if (success)
        {
            swapMode = false;
            swapButtonLabel.text = "Swap";
            selectedPrefab = null;
            selectedCreaturePanel.SetActive(false);
            RefreshTeamSlots();
        }
    }

    void SelectSlot(GameObject prefab, bool isActive)
    {
        if (swapMode)
        {
            if (prefab == selectedPrefab) return; // clicked the same creature, ignore

            bool success = PlayerRoster.Instance.Swap(selectedPrefab, prefab);
            if (success)
            {
                swapMode = false;
                swapButtonLabel.text = "Swap";
                selectedPrefab = null;
                selectedCreaturePanel.SetActive(false);
                RefreshTeamSlots();
            }
            return;
        }

        selectedPrefab = prefab;
        selectedIsActive = isActive;

        Creature c = prefab.GetComponent<Creature>();
        selectedCreatureDescription.text = c != null ? c.description : string.Empty;
        selectedCreaturePanel.SetActive(true);
    }

    void OnSwapButtonPressed()
    {
        swapMode = !swapMode;
        swapButtonLabel.text = swapMode ? "Cancel Swap" : "Swap";
    }

    void OnSellSelected()
    {
        if (selectedPrefab == null) return;

        PlayerRoster.Instance.Sell(selectedPrefab);
        selectedPrefab = null;
        selectedCreaturePanel.SetActive(false);
        RefreshTeamSlots();
    }

    void BackToHubFromTeam()
    {
        swapMode = false;
        swapButtonLabel.text = "Swap";
        teamPanel.SetActive(false);
        hubPanel.SetActive(true);
        Refresh();
    }

    // ── Entry point from LevelManager ───────────────────────────────────────
    public void Show(CraftingManager manager, Action onDone)
    {
        craftingManager = manager;
        onHubDone = onDone;

        hubPanel.SetActive(true);
        HideAllSubPanels();
        Refresh();
    }

    public void Refresh()
    {
        formInventoryText.text =
            $"Attack: {craftingManager.GetFormCount(FormType.Attack)}  " +
            $"Defense: {craftingManager.GetFormCount(FormType.Defense)}  " +
            $"Support: {craftingManager.GetFormCount(FormType.Support)}";

        essenceInventoryText.text =
            $"Magic: {craftingManager.GetEssenceCount(EssenceType.Magic)}  " +
            $"Nature: {craftingManager.GetEssenceCount(EssenceType.Nature)}  " +
            $"Mechanical: {craftingManager.GetEssenceCount(EssenceType.Mechanical)}";
        craftEntryButton.interactable = !PlayerRoster.Instance.IsFull;
    }

    void CloseHub()
    {
        hubPanel.SetActive(false);
        HideAllSubPanels();
        onHubDone?.Invoke();
    }

    // ── Craft flow ───────────────────────────────────────────────────────────
    void ShowCraftPanel()
    {
        hubPanel.SetActive(false);
        craftPanel.SetActive(true);
        selectedForm = null;
        selectedEssence = null;
        confirmCraftButton.interactable = false;

        FormType[] allForms = (FormType[])Enum.GetValues(typeof(FormType));
        for (int i = 0; i < formSelectButtons.Length && i < allForms.Length; i++)
        {
            FormType f = allForms[i];
            int count = craftingManager.GetFormCount(f);
            formSelectCounts[i].text = $"Available: {count}";
            formSelectButtons[i].interactable = count > 0;
            formSelectButtons[i].onClick.RemoveAllListeners();
            formSelectButtons[i].onClick.AddListener(() => { selectedForm = f; UpdateCraftPreview(); });
        }

        EssenceType[] allEssences = (EssenceType[])Enum.GetValues(typeof(EssenceType));
        for (int i = 0; i < essenceSelectButtons.Length && i < allEssences.Length; i++)
        {
            EssenceType e = allEssences[i];
            int count = craftingManager.GetEssenceCount(e);
            essenceSelectCounts[i].text = $"Available: {count}";
            essenceSelectButtons[i].interactable = count > 0;
            essenceSelectButtons[i].onClick.RemoveAllListeners();
            essenceSelectButtons[i].onClick.AddListener(() => { selectedEssence = e; UpdateCraftPreview(); });
        }

        resultSilhouette.gameObject.SetActive(false);
        resultLabel.text = string.Empty;
    }

    void UpdateCraftPreview()
    {
        if (selectedForm == null || selectedEssence == null) return;
        var recipe = Array.Find(craftingManager.recipes,
            r => r.form == selectedForm && r.essence == selectedEssence);
        if (recipe != null && recipe.resultPrefab != null)
        {
            Creature previewData = recipe.resultPrefab.GetComponent<Creature>();
            resultSilhouette.gameObject.SetActive(true);
            resultSilhouette.sprite = previewData != null ? previewData.portrait : null;
            resultLabel.text = recipe.resultPrefab.name.Replace("(Clone)", "").Trim();

            confirmCraftButton.interactable = forcedInitialMode
                ? true
                : craftingManager.CanCraftAnything(selectedForm.Value, selectedEssence.Value);
        }
        else
        {
            resultSilhouette.gameObject.SetActive(false);
            resultLabel.text = "No recipe found.";
            confirmCraftButton.interactable = false;
        }
    }

    void OnConfirmCraft()
    {
        if (selectedForm == null || selectedEssence == null) return;

        if (forcedInitialMode)
        {
            forcedInitialMode = false;
            EssenceType chosen = selectedEssence.Value;
            craftPanel.SetActive(false);
            if (craftBackButton != null) craftBackButton.interactable = true;
            onForcedEssenceChosen?.Invoke(chosen);
            onForcedEssenceChosen = null;
            return;
        }

        craftingManager.Craft(selectedForm.Value, selectedEssence.Value);
        BackToHubFromCraft();
    }

    void BackToHubFromCraft()
    {
        craftPanel.SetActive(false);
        hubPanel.SetActive(true);
        Refresh();
    }

    // ── Forced initial craft (game start only) ───────────────────────────────
    public void ShowForcedInitialCraft(CraftingManager manager, FormType lockedForm, Action<EssenceType> onChosen)
    {
        craftingManager = manager;
        hubPanel.SetActive(false);
        HideAllSubPanels();
        craftPanel.SetActive(true);
        forcedInitialMode = true;
        onForcedEssenceChosen = onChosen;

        selectedForm = lockedForm;
        selectedEssence = null;
        confirmCraftButton.interactable = false;
        if (craftBackButton != null) craftBackButton.interactable = false;

        FormType[] allForms = (FormType[])Enum.GetValues(typeof(FormType));
        for (int i = 0; i < formSelectButtons.Length && i < allForms.Length; i++)
        {
            FormType f = allForms[i];
            int count = craftingManager.GetFormCount(f);
            formSelectCounts[i].text = $"Available: {count}";
            formSelectButtons[i].interactable = false; // locked - form is preset
            formSelectButtons[i].onClick.RemoveAllListeners();
        }

        EssenceType[] allEssences = (EssenceType[])Enum.GetValues(typeof(EssenceType));
        for (int i = 0; i < essenceSelectButtons.Length && i < allEssences.Length; i++)
        {
            EssenceType e = allEssences[i];
            essenceSelectCounts[i].text = "Free";
            essenceSelectButtons[i].interactable = true; // always pickable here
            essenceSelectButtons[i].onClick.RemoveAllListeners();
            essenceSelectButtons[i].onClick.AddListener(() => { selectedEssence = e; UpdateCraftPreview(); });
        }

        resultSilhouette.gameObject.SetActive(false);
        resultLabel.text = string.Empty;
    }

    // ── Forced initial essence choice (unused by current flow, kept for reference) ──
    public void ShowForcedEssenceChoice(FormType startingForm, Action<EssenceType> onChosen)
    {
        forcedEssencePanel.SetActive(true);

        EssenceType[] allEssences = (EssenceType[])Enum.GetValues(typeof(EssenceType));
        for (int i = 0; i < essenceChoiceButtons.Length && i < allEssences.Length; i++)
        {
            EssenceType e = allEssences[i];
            essenceChoiceLabels[i].text = e.ToString();
            essenceChoiceButtons[i].onClick.RemoveAllListeners();
            essenceChoiceButtons[i].onClick.AddListener(() =>
            {
                forcedEssencePanel.SetActive(false);
                onChosen?.Invoke(e);
            });
        }
    }

    void HideAllSubPanels()
    {
        craftPanel.SetActive(false);
        forcedEssencePanel.SetActive(false);
        teamPanel.SetActive(false);
    }
}