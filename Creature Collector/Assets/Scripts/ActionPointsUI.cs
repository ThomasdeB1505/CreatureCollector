using UnityEngine;
using UnityEngine.UI;

public class ActionPointsUI : MonoBehaviour
{
    public GameObject actionPointPrefab; // a UI Image object
    public Sprite fullCircle;
    public Sprite emptyCircle;

    private Image[] circles;

    public void SetupCircles(int amount)
    {
        // clear old ones
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        circles = new Image[amount];
        for (int i = 0; i < amount; i++)
        {
            GameObject obj = Instantiate(actionPointPrefab, transform);
            circles[i] = obj.GetComponent<Image>();
        }
    }

    public void UpdateCircles(int remaining, int total)
    {
        for (int i = 0; i < circles.Length; i++)
            circles[i].sprite = i < remaining ? fullCircle : emptyCircle;
    }
}