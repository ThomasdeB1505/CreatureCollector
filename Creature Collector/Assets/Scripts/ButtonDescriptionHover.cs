using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Attached per-button via ActionUI.AddHoverDescription.
// On hover, positions the shared description element above THIS button and shows it.
public class ButtonDescriptionHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public TextMeshProUGUI descriptionText;
    [HideInInspector] public string description;

    // The element that actually gets moved and shown/hidden.
    // This is the background panel if ActionUI has one assigned, otherwise it's
    // just the text's own RectTransform/GameObject.
    [HideInInspector] public RectTransform positionTarget;
    [HideInInspector] public GameObject toggleTarget;

    [HideInInspector] public float verticalPadding = 8f;

    private RectTransform buttonRect;

    void Awake()
    {
        buttonRect = transform as RectTransform;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (descriptionText == null) return;
        descriptionText.text = description;
        if (toggleTarget != null) toggleTarget.SetActive(true);

        RectTransform target = positionTarget != null ? positionTarget : descriptionText.rectTransform;

        // Content Size Fitter / Layout Group only recalculate during Unity's normal
        // layout pass, which runs AFTER this frame's code. Force it now so the
        // background actually resizes this frame and target.rect is up to date
        // before we use it to position the box.
        LayoutRebuilder.ForceRebuildLayoutImmediate(target);

        PositionAboveButton(target);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (toggleTarget != null) toggleTarget.SetActive(false);
    }

    void PositionAboveButton(RectTransform target)
    {
        RectTransform targetParent = target.parent as RectTransform;
        if (targetParent == null || buttonRect == null) return;

        Canvas canvas = targetParent.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        // World position of the button's top-CENTER edge.
        // rect.center.x (not 0) is used so this is correct regardless of the
        // button's pivot setting - 0 only equals "center" when pivot.x == 0.5.
        Vector3 worldTop = buttonRect.TransformPoint(new Vector3(buttonRect.rect.center.x, buttonRect.rect.yMax, 0f));
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldTop);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetParent, screenPoint, cam, out Vector2 localPoint))
        {
            float halfHeight = target.rect.height * 0.5f;
            target.anchoredPosition = localPoint + new Vector2(0f, halfHeight + verticalPadding);
        }
    }
}