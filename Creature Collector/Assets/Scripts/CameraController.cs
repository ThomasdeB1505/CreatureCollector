using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("Grid Centering")]
    [Tooltip("Camera tilt angle in degrees (60 is a good top-down angle)")]
    public float tiltAngle = 60f;
    [Tooltip("Height of the camera above center for a 7x7 grid")]
    public float baseHeight = 14f;

    [Header("Orbit")]
    public float orbitSensitivity = 0.4f;
    public float minPitch = 10f;
    public float maxPitch = 89f;

    [Header("Pan")]
    public float panSensitivity = 0.02f;

    [Header("Zoom")]
    public float zoomSensitivity = 2f;
    public float minZoom = 2f;
    public float maxZoom = 60f;

    // The point the camera orbits around
    private Vector3 _pivot;
    // Spherical coords relative to pivot
    private float _yaw;    // horizontal angle
    private float _pitch;  // vertical angle
    private float _distance;

    void Awake() => Instance = this;

    void Update()
    {
        HandleOrbit();
        HandleZoom();
        ApplyTransform();
    }

    // ── Input Handlers ────────────────────────────────────────────────────────

    void HandleOrbit()
    {
        // Right mouse button drag → orbit
        if (!Input.GetMouseButton(1)) return;

        _yaw += Input.GetAxisRaw("Mouse X") * orbitSensitivity * _distance;
        _pitch -= Input.GetAxisRaw("Mouse Y") * orbitSensitivity * _distance;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        _distance -= scroll * zoomSensitivity * _distance; // proportional feel
        _distance = Mathf.Clamp(_distance, minZoom, maxZoom);
    }

    // ── Transform Application ─────────────────────────────────────────────────

    void ApplyTransform()
    {
        // Convert spherical → cartesian offset from pivot
        float pitchRad = _pitch * Mathf.Deg2Rad;
        float yawRad = _yaw * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            _distance * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
            _distance * Mathf.Sin(pitchRad),
            _distance * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad)
        );

        transform.position = _pivot + offset;
        transform.LookAt(_pivot);
    }

    // ── Grid Centering (public API, unchanged behaviour) ──────────────────────

    public void CenterOnGrid(int gridWidth, int gridHeight)
    {
        _pivot = Vector3.zero; // grid is centered at origin

        float maxDim = Mathf.Max(gridWidth, gridHeight);
        float height = baseHeight * (maxDim / 7f);
        float radians = tiltAngle * Mathf.Deg2Rad;
        float pullBack = height / Mathf.Tan(radians);

        // Derive starting spherical coords from the original position formula
        Vector3 startPos = new Vector3(0f, height, -pullBack);
        Vector3 toCamera = startPos - _pivot;

        _distance = toCamera.magnitude;
        _pitch = Mathf.Asin(toCamera.y / _distance) * Mathf.Rad2Deg;
        _yaw = 0f;

        ApplyTransform();
    }
}