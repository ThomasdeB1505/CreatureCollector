using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Tooltip("Camera tilt angle in degrees (60 is a good top-down angle)")]
    public float tiltAngle = 60f;

    [Tooltip("Height of the camera above center for a 7x7 grid")]
    public float baseHeight = 14f;

    void Awake() => Instance = this;

    public void CenterOnGrid(int gridWidth, int gridHeight)
    {
        // Grid is centered at 0,0 (GenerateGrid uses x - width/2 offset)
        float centerX = 0f;
        float centerZ = 0f;

        // Scale height relative to the largest grid size (7)
        float maxDim = Mathf.Max(gridWidth, gridHeight);
        float height = baseHeight * (maxDim / 7f);

        float radians = tiltAngle * Mathf.Deg2Rad;
        float pullBack = height / Mathf.Tan(radians);

        transform.position = new Vector3(centerX, height, centerZ - pullBack);
        transform.LookAt(new Vector3(centerX, 0f, centerZ));
    }
}