using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CaptureUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public Transform buttonContainer;
    public GameObject captureButtonPrefab;
    public GameObject finishedAllLevelsPanel; // Assign a separate "All Done!" panel in the Inspector

    private Action<GameObject> onCaptured;
    private List<GameObject> prefabs;

    public void Show(List<GameObject> enemyPrefabs, Action<GameObject> callback, bool isFinalLevel = false)
    {
        if (isFinalLevel)
        {
            ShowFinishedScreen();
            return;
        }

        onCaptured = callback;
        prefabs = enemyPrefabs;

        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        titleText.text = "Choose a creature to capture!";

        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            int idx = i;
            GameObject btnObj = Instantiate(captureButtonPrefab, buttonContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = enemyPrefabs[i].name.Replace("(Clone)", "").Trim();
            btnObj.GetComponent<Button>().onClick.AddListener(() => Capture(idx));

            Creature c = enemyPrefabs[i].GetComponent<Creature>();
            UnityEngine.UI.Image img = btnObj.GetComponentInChildren<UnityEngine.UI.Image>();
            if (img != null && c != null && c.portrait != null)
                img.sprite = c.portrait;
        }

        panel.SetActive(true);
    }

    void ShowFinishedScreen()
    {
        panel.SetActive(false);
        if (finishedAllLevelsPanel != null)
            finishedAllLevelsPanel.SetActive(true);
    }

    void Capture(int index)
    {
        panel.SetActive(false);
        onCaptured?.Invoke(prefabs[index]);
    }
}