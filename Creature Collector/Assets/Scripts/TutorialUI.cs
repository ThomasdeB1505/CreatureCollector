using UnityEngine;
using UnityEngine.UI;

public class TutorialUI : MonoBehaviour
{
    public GameObject tutorialPanel;
    public Button tutorialButton;

    void Start()
    {
        tutorialPanel.SetActive(false);
        tutorialButton.onClick.AddListener(ToggleTutorial);
    }

    void ToggleTutorial()
    {
        bool isOpen = tutorialPanel.activeSelf;
        tutorialPanel.SetActive(!isOpen);
    }
}