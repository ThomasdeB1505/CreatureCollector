using UnityEngine;
using UnityEngine.UI;

public class CreatureHealthUI : MonoBehaviour
{
    public GameObject heartPrefab;
    public float heightOffset = 1.5f;
    public Sprite fullHeart;
    public Sprite emptyHeart;

    private Image[] hearts;
    private Canvas canvas;
    private Creature creature;
    private Transform canvasTransform;
    public float heartSize = 0.3f;

    public void Initialize(Creature _creature, int maxHealth)
    {
        creature = _creature;

        // create a world space canvas above the creature
        GameObject canvasObj = new GameObject("HealthCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 1);

        HorizontalLayoutGroup layout = canvasObj.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(heartSize * maxHealth, heartSize);

        // spawn hearts
        hearts = new Image[maxHealth];
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject heart = Instantiate(heartPrefab, canvasObj.transform);
            heart.GetComponent<RectTransform>().sizeDelta = new Vector2(heartSize, heartSize);
            hearts[i] = heart.GetComponent<Image>();
        }

        UpdateHearts(maxHealth);
        canvasTransform = canvasObj.transform; // ADD: store canvas reference
    }

    public void UpdateHearts(int currentHealth)
    {
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].sprite = i < currentHealth ? fullHeart : emptyHeart;
    }



    void LateUpdate()
    {
        if (canvasTransform != null)
        {
            canvasTransform.forward = Camera.main.transform.forward;
            canvasTransform.position = transform.position + Camera.main.transform.up * heightOffset;
        }
    }
}