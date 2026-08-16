using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // remove this line + swap Text types below if you're not using TextMeshPro

/// <summary>
/// Singleton that drives step-by-step tutorial overlays.
/// Attach this to an empty GameObject alongside your tutorial UI Canvas.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    /// <summary>
    /// True whenever a tutorial step is currently showing. Other scripts
    /// (like your combat grid's click handler) should check this and bail
    /// out early if true, since 3D world raycasts aren't blocked by UI.
    /// </summary>
    public bool IsTutorialActive => tutorialPanel != null && tutorialPanel.activeSelf;

    [Serializable]
    public class TutorialStep
    {
        public string title;
        [TextArea(2, 5)] public string body;

        [Tooltip("Optional: the UI element this step should point at. Leave empty for no arrow.")]
        public RectTransform targetElement;

        [Tooltip("Offset from the target's position, so the arrow doesn't sit exactly on top of it.")]
        public Vector2 arrowOffset = new Vector2(0f, 60f);

        [Tooltip("Optional: an empty object (just needs a RectTransform, no Image required) you manually position and size in the tutorial panel to mark the highlighted area for this step. Leave empty for no highlight.")]
        public RectTransform highlightArea;

        [Tooltip("Optional: an empty object marking where the text frame (title/body/Next button) should move to for THIS step, so it doesn't cover what you're describing. Leave empty to keep the frame at its normal default position.")]
        public RectTransform framePositionOverride;
    }

    [Serializable]
    public class TutorialSequence
    {
        public string stageId;              // e.g. "Crafting", "EnemySelect", "Combat"
        public List<TutorialStep> steps;    // multiple steps per stage
    }

    [Header("Sequences (set these up in the Inspector)")]
    public List<TutorialSequence> sequences = new List<TutorialSequence>();

    [Header("Tutorial Overlay UI")]
    public GameObject tutorialPanel;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public Button nextButton;
    public Button skipButton; // optional, can leave unassigned

    [Tooltip("The RectTransform that actually holds the title/body/Next button visuals - i.e. the box that should physically move per step. This is usually a CHILD of tutorialPanel, not tutorialPanel itself (so the dim/arrow/highlight layers don't move with it).")]
    public RectTransform textFrame;

    private Vector2 defaultFrameAnchoredPosition;
    private bool hasDefaultFramePosition;

    [Tooltip("An arrow/pointer Image on the tutorial Canvas. Gets shown and repositioned per step.")]
    public RectTransform arrowIndicator;

    [Header("Spotlight Dim (dark everywhere except the highlighted area)")]
    [Tooltip("A single full-screen Image using the 'UI/DimWithHole' shader (see DimWithHole.shader). The script sets the hole's position/size per step via shader properties - no extra objects needed.")]
    public Image dimImage;

    private Material dimMaterialInstance;

    private bool tutorialEnabled = true;
    private List<TutorialStep> currentSteps;
    private int currentStepIndex;
    private string currentStageId;
    private readonly HashSet<string> playedStagesThisRun = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        if (textFrame != null)
        {
            defaultFrameAnchoredPosition = textFrame.anchoredPosition;
            hasDefaultFramePosition = true;
        }

        if (dimImage != null)
        {
            // Instantiate our own copy of the material so we don't permanently
            // edit the shared shader asset when setting hole values at runtime.
            dimMaterialInstance = new Material(dimImage.material);
            dimImage.material = dimMaterialInstance;
            dimImage.gameObject.SetActive(false);
        }

        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (skipButton != null) skipButton.onClick.AddListener(EndSequence);
    }

    /// <summary>
    /// Call this whenever a stage's Canvas becomes active.
    /// Looks up the matching sequence by stageId and plays it from the start.
    /// </summary>
    public void StartSequence(string stageId)
    {
        if (!tutorialEnabled) return;
        if (playedStagesThisRun.Contains(stageId)) return; // already shown this session

        TutorialSequence sequence = sequences.Find(s => s.stageId == stageId);
        if (sequence == null || sequence.steps == null || sequence.steps.Count == 0)
            return;

        currentStageId = stageId;
        currentSteps = sequence.steps;
        currentStepIndex = 0;
        playedStagesThisRun.Add(stageId);

        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (currentSteps == null || currentStepIndex >= currentSteps.Count)
        {
            EndSequence();
            return;
        }

        TutorialStep step = currentSteps[currentStepIndex];
        if (titleText != null) titleText.text = step.title;
        if (bodyText != null) bodyText.text = step.body;

        if (arrowIndicator != null)
        {
            if (step.targetElement != null)
            {
                arrowIndicator.gameObject.SetActive(true);
                // Both are Screen Space - Overlay, so .position is already in matching screen space
                arrowIndicator.position = step.targetElement.position + (Vector3)step.arrowOffset;
            }
            else
            {
                arrowIndicator.gameObject.SetActive(false);
            }
        }

        ApplySpotlight(step.highlightArea);
        ApplyFramePosition(step.framePositionOverride);

        if (tutorialPanel != null) tutorialPanel.SetActive(true);
    }

    /// <summary>
    /// Moves textFrame to match the given marker's position, or back to its
    /// captured default position if the marker is null (i.e. no override this step).
    /// </summary>
    private void ApplyFramePosition(RectTransform overrideMarker)
    {
        if (textFrame == null) return;

        if (overrideMarker != null)
        {
            // World position match works even if the marker sits elsewhere in the
            // same Screen Space - Overlay hierarchy.
            textFrame.position = overrideMarker.position;
        }
        else if (hasDefaultFramePosition)
        {
            textFrame.anchoredPosition = defaultFrameAnchoredPosition;
        }
    }

    /// <summary>
    /// Dims the screen via dimImage's shader, punching a see-through hole around
    /// the given window (if any). Pass null to dim with no hole.
    /// </summary>
    private void ApplySpotlight(RectTransform window)
    {
        if (dimImage == null || dimMaterialInstance == null) return;

        dimImage.gameObject.SetActive(true);

        if (window == null)
        {
            // Sentinel values that no UV coordinate can ever fall inside -> no hole
            dimMaterialInstance.SetVector("_HoleMin", new Vector4(2f, 2f, 0f, 0f));
            dimMaterialInstance.SetVector("_HoleMax", new Vector4(-2f, -2f, 0f, 0f));
            return;
        }

        RectTransform dimRect = dimImage.rectTransform;

        // Get the window's corners in world space, works even across different Canvases
        Vector3[] corners = new Vector3[4];
        window.GetWorldCorners(corners); // [0] bottom-left, [2] top-right

        Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(dimRect, screenBL, null, out Vector2 localBL);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dimRect, screenTR, null, out Vector2 localTR);

        Rect r = dimRect.rect;

        // Normalize into the dim Image's own 0-1 UV space
        float nx0 = Mathf.Clamp01((localBL.x - r.xMin) / r.width);
        float nx1 = Mathf.Clamp01((localTR.x - r.xMin) / r.width);
        float ny0 = Mathf.Clamp01((localBL.y - r.yMin) / r.height);
        float ny1 = Mathf.Clamp01((localTR.y - r.yMin) / r.height);

        dimMaterialInstance.SetVector("_HoleMin", new Vector4(nx0, ny0, 0f, 0f));
        dimMaterialInstance.SetVector("_HoleMax", new Vector4(nx1, ny1, 0f, 0f));
    }

    private void OnNextClicked()
    {
        currentStepIndex++;
        ShowCurrentStep();
    }

    private void EndSequence()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false);
        if (dimImage != null) dimImage.gameObject.SetActive(false);
        if (textFrame != null && hasDefaultFramePosition) textFrame.anchoredPosition = defaultFrameAnchoredPosition;
        currentSteps = null;
        currentStageId = null;
    }
}