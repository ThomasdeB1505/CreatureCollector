using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class CreatureUI : MonoBehaviour, IPointerClickHandler
{
    private Creature creature;
    public TextMeshProUGUI statsText;

    public CreaturePreviewManager previewManager;

    private static CreatureUI currentlySelected;

    void Start()
    {
        creature = GetComponent<Creature>();

        statsText.gameObject.SetActive(false);
        UpdateText();
    }

    void UpdateText()
    {
        statsText.text =
            $"HP: {creature.health}\n\n" +
            $"Move Range: {creature.moveRange}\n" +
            //  $"Move Cost: {creature.moveActionCost}\n\n" +
            $"Attack Range: {creature.attackRange}\n" +
            $"Damage: {creature.attackDamage}\n";
          //  $"Attack Cost: {creature.attackActionCost}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // If clicking the currently selected creature, close it
        if (currentlySelected == this)
        {
            statsText.gameObject.SetActive(false);
            currentlySelected = null;
            return;
        }

        // If another creature is selected, close it
        if (currentlySelected != null)
        {
            currentlySelected.statsText.gameObject.SetActive(false);
        }

        // Open this one
        statsText.gameObject.SetActive(true);
        currentlySelected = this;
        previewManager.ShowPreview(creature.gameObject);
    }

    public void ShowStats()
    {
        // Force select this creature properly
        if (currentlySelected != null && currentlySelected != this)
        {
            currentlySelected.statsText.gameObject.SetActive(false);
        }

        statsText.gameObject.SetActive(true);
        currentlySelected = this;
    }
    //Update health when taking damage
    void Update()
    {
        if (statsText.gameObject.activeSelf)
        {
            UpdateText();
        }
    }
}