using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EvolutionPopupUI : MonoBehaviour
{
    public GameObject panel;
    public Image optionAImage;
    public TMP_Text optionAName;
    public TMP_Text optionADescription;
    public Button optionAButton;
    public Image optionBImage;
    public TMP_Text optionBName;
    public TMP_Text optionBDescription;
    public Button optionBButton;
    public Button cancelButton;

    private GameObject prefabA;
    private GameObject prefabB;

    void Awake()
    {
        optionAButton.onClick.AddListener(() => Confirm(prefabA));
        optionBButton.onClick.AddListener(() => Confirm(prefabB));
        if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
        panel.SetActive(false);
    }

    public void Show(Creature creature)
    {
        prefabA = creature.formEvolutionPrefab;
        prefabB = creature.essenceEvolutionPrefab;

        optionAButton.gameObject.SetActive(prefabA != null);
        if (prefabA != null)
        {
            optionAImage.sprite = creature.formEvolutionSprite;
            optionADescription.text = creature.formEvolutionDescription;
            optionAName.text = prefabA.name;
        }

        optionBButton.gameObject.SetActive(prefabB != null);
        if (prefabB != null)
        {
            optionBImage.sprite = creature.essenceEvolutionSprite;
            optionBDescription.text = creature.essenceEvolutionDescription;
            optionBName.text = prefabB.name;
        }

        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    void Confirm(GameObject chosenPrefab)
    {
        Hide();
        if (chosenPrefab != null)
            BlackBoard.gameManager.ExecuteEvolution(chosenPrefab);
    }
}