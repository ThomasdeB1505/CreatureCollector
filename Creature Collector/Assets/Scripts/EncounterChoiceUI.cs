using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EncounterChoiceUI : MonoBehaviour
{
    public GameObject panel;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionNameLabels;
    public Image[] optionImages;
    public TextMeshProUGUI[] essenceRewardLabels; // NEW - shows essence types on offer
    public TextMeshProUGUI[] formRewardLabels;    // NEW - shows form on offer

    private Action<EncounterOption> onChosen;
    private EncounterOption[] options;

    void Awake() => panel.SetActive(false);

    public void Show(EncounterOption[] encounterOptions, Action<EncounterOption> callback)
    {
        options = encounterOptions;
        onChosen = callback;

        for (int i = 0; i < optionButtons.Length && i < encounterOptions.Length; i++)
        {
            int idx = i;
            var opt = encounterOptions[i];

            optionNameLabels[i].text = opt.encounterName;
            if (opt.previewSprite != null)
                optionImages[i].sprite = opt.previewSprite;

            // Derive essence types on offer from the enemies in this encounter
            var essenceTypes = opt.enemyCreaturePrefabs
                .Select(p => p.GetComponent<Creature>().essenceDropType)
                .Distinct();
            essenceRewardLabels[i].text = "Essence: " + string.Join(", ", essenceTypes);

            formRewardLabels[i].text = "Form: " + opt.formReward;

            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => Choose(idx));
        }
        panel.SetActive(true);
    }

    void Choose(int index)
    {
        panel.SetActive(false);
        onChosen?.Invoke(options[index]);
    }
}