using UnityEngine;

public class CreaturePreviewManager : MonoBehaviour
{
    public static CreaturePreviewManager Instance;

    public Transform spawnPoint;
    public GameObject currentPreview;

    void Awake()
    {
        Instance = this;
    }
    public void ShowPreview(GameObject creaturePrefab)
    {
        Debug.Log("Spawn called");

        if (currentPreview != null)
            Destroy(currentPreview);

        currentPreview = Instantiate(creaturePrefab, spawnPoint.position, spawnPoint.rotation);

        currentPreview.transform.SetParent(spawnPoint);
        currentPreview.transform.localPosition = Vector3.zero;
        currentPreview.transform.localRotation = Quaternion.identity;
        currentPreview.transform.localScale = Vector3.one;

        foreach (var c in currentPreview.GetComponentsInChildren<Creature>())
            c.enabled = false;

        SetLayerRecursive(currentPreview, LayerMask.NameToLayer("Preview"));
    }

    void Update()
    {
        if (currentPreview != null)
            currentPreview.transform.Rotate(0, 50f * Time.deltaTime, 0);
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}