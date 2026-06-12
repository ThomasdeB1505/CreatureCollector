using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EncounterChoiceUI : MonoBehaviour
{
    public GameObject panel;
    public Button[] optionButtons;               // 2 buttons in Inspector
    public TextMeshProUGUI[] optionNameLabels;   // label per button
    public Image[] optionImages;                 // preview image per button

    private Action<EncounterOption> onChosen;
    private EncounterOption[] options;

    public void Show(EncounterOption[] encounterOptions, Action<EncounterOption> callback)
    {
        options = encounterOptions;
        onChosen = callback;

        for (int i = 0; i < optionButtons.Length && i < encounterOptions.Length; i++)
        {
            int idx = i;
            optionNameLabels[i].text = encounterOptions[i].encounterName;
            if (encounterOptions[i].previewSprite != null)
                optionImages[i].sprite = encounterOptions[i].previewSprite;
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