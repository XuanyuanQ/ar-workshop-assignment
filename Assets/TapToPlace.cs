using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TapToPlace : MonoBehaviour
{
    [SerializeField] private GameObject objectToPlace;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField, Range(0.1f, 0.95f)] private float planeFillRatio = 0.35f;
    [SerializeField] private Color selectedColor = new(1f, 0.85f, 0.1f, 1f);

    private readonly List<ARRaycastHit> hits = new();
    private SurfacePlaneSizeFitter selectedObject;
    public int PlacementCount { get; private set; }

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = GetComponent<ARRaycastManager>();
        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();

        if (objectToPlace == null || raycastManager == null)
        {
            Debug.LogError("Assign Object To Place and Raycast Manager.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!TryGetPointer(out Vector2 screenPoint, out bool pressed, out bool held, out bool released))
            return;

        if (released)
            ClearSelection();

        if (pressed)
        {
            if (TrySelectObject(screenPoint))
                return;
            if (IsPointerOverMarkerContent(screenPoint))
                return;

            TryPlaceObject(screenPoint);
            return;
        }

        if (held && selectedObject != null)
            TryMoveSelectedObject(screenPoint);
    }

    private void TryPlaceObject(Vector2 screenPoint)
    {
        if (ARSession.state != ARSessionState.SessionTracking) return;
        if (!raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon)) return;

        ARRaycastHit hit = hits[0];
        Pose pose = hit.pose;
        GameObject placedObject = Instantiate(objectToPlace, pose.position, pose.rotation);
        FitObjectToHitPlane(placedObject, hit.trackableId);
        PlacementCount++;
    }

    private bool TrySelectObject(Vector2 screenPoint)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Ray ray = camera.ScreenPointToRay(screenPoint);
        if (!Physics.Raycast(ray, out RaycastHit hit, 50f))
            return false;

        var fitter = hit.collider.GetComponentInParent<SurfacePlaneSizeFitter>();
        if (fitter == null)
            return false;

        ClearSelection();
        selectedObject = fitter;
        selectedObject.SetHighlighted(true, selectedColor);
        return true;
    }

    private static bool IsPointerOverMarkerContent(Vector2 screenPoint)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Ray ray = camera.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, 50f);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && hit.collider.GetComponentInParent<SpinObjectOnMarker>() != null)
                return true;
        }

        return false;
    }

    private void TryMoveSelectedObject(Vector2 screenPoint)
    {
        if (ARSession.state != ARSessionState.SessionTracking) return;
        if (!raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon)) return;

        ARRaycastHit hit = hits[0];
        ARPlane plane = planeManager != null ? planeManager.GetPlane(hit.trackableId) : null;
        selectedObject.MoveTo(hit.pose, plane, planeFillRatio);
    }

    private void ClearSelection()
    {
        if (selectedObject == null)
            return;

        selectedObject.SetHighlighted(false, selectedColor);
        selectedObject = null;
    }

    private void FitObjectToHitPlane(GameObject placedObject, TrackableId planeId)
    {
        if (planeManager == null || placedObject == null)
            return;

        ARPlane plane = planeManager.GetPlane(planeId);
        if (plane == null)
            return;

        var fitter = placedObject.GetComponent<SurfacePlaneSizeFitter>();
        if (fitter == null)
            fitter = placedObject.AddComponent<SurfacePlaneSizeFitter>();

        fitter.Initialize(plane, planeFillRatio);
    }

    private static bool TryGetPointer(
        out Vector2 screenPoint,
        out bool pressed,
        out bool held,
        out bool released)
    {
        screenPoint = default;
        pressed = false;
        held = false;
        released = false;

#if UNITY_EDITOR
        if (Mouse.current != null &&
            !Mouse.current.rightButton.isPressed)
        {
            screenPoint = Mouse.current.position.ReadValue();
            pressed = Mouse.current.leftButton.wasPressedThisFrame;
            held = Mouse.current.leftButton.isPressed;
            released = Mouse.current.leftButton.wasReleasedThisFrame;
            return pressed || held || released;
        }
#endif
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            screenPoint = touch.position.ReadValue();
            pressed = touch.press.wasPressedThisFrame;
            held = touch.press.isPressed;
            released = touch.press.wasReleasedThisFrame;
            return pressed || held || released;
        }

        return false;
    }
}

public class SurfacePlaneSizeFitter : MonoBehaviour
{
    private const float MinimumScale = 0.02f;

    private ARPlane plane;
    private float fillRatio = 0.35f;
    private Vector3 authoredScale;
    private Vector2 unscaledFootprint = Vector2.one;
    private Renderer[] cachedRenderers;
    private Color[] originalColors;
    private Color[] originalBaseColors;
    private bool[] hadColorProperty;
    private bool[] hadBaseColorProperty;
    private bool isHighlighted;

    public void Initialize(ARPlane targetPlane, float targetFillRatio)
    {
        plane = targetPlane;
        fillRatio = Mathf.Clamp(targetFillRatio, 0.1f, 0.95f);
        authoredScale = transform.localScale;
        unscaledFootprint = CalculateUnscaledFootprint();
        ApplyScale();
    }

    public void MoveTo(Pose pose, ARPlane targetPlane, float targetFillRatio)
    {
        transform.SetPositionAndRotation(pose.position, pose.rotation);

        if (targetPlane != null)
            plane = targetPlane;

        fillRatio = Mathf.Clamp(targetFillRatio, 0.1f, 0.95f);
        ApplyScale();
    }

    public void SetHighlighted(bool highlighted, Color highlightColor)
    {
        if (isHighlighted == highlighted)
            return;

        CacheRendererColors();
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Material material = cachedRenderers[i].material;
            if (highlighted)
            {
                if (hadColorProperty[i])
                    material.SetColor("_Color", highlightColor);
                if (hadBaseColorProperty[i])
                    material.SetColor("_BaseColor", highlightColor);
            }
            else
            {
                if (hadColorProperty[i])
                    material.SetColor("_Color", originalColors[i]);
                if (hadBaseColorProperty[i])
                    material.SetColor("_BaseColor", originalBaseColors[i]);
            }
        }

        isHighlighted = highlighted;
    }

    private void LateUpdate()
    {
        ApplyScale();
    }

    private void ApplyScale()
    {
        if (plane == null)
            return;

        Vector2 planeSize = plane.size * fillRatio;
        if (planeSize.x <= 0f || planeSize.y <= 0f)
            return;

        float xFactor = planeSize.x / Mathf.Max(unscaledFootprint.x * authoredScale.x, 0.0001f);
        float zFactor = planeSize.y / Mathf.Max(unscaledFootprint.y * authoredScale.z, 0.0001f);
        float factor = Mathf.Min(1f, xFactor, zFactor);

        transform.localScale = new Vector3(
            Mathf.Max(authoredScale.x * factor, MinimumScale),
            Mathf.Max(authoredScale.y * factor, MinimumScale),
            Mathf.Max(authoredScale.z * factor, MinimumScale));
    }

    private Vector2 CalculateUnscaledFootprint()
    {
        var meshFilters = GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
            return Vector2.one;

        bool hasBounds = false;
        Bounds combinedBounds = default;
        Matrix4x4 rootWorldToLocal = transform.worldToLocalMatrix;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
                continue;

            Bounds meshBounds = mesh.bounds;
            Matrix4x4 meshToRoot = rootWorldToLocal * meshFilter.transform.localToWorldMatrix;
            foreach (Vector3 corner in GetBoundsCorners(meshBounds))
            {
                Vector3 rootPoint = meshToRoot.MultiplyPoint3x4(corner);
                if (!hasBounds)
                {
                    combinedBounds = new Bounds(rootPoint, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(rootPoint);
                }
            }
        }

        if (!hasBounds)
            return Vector2.one;

        Vector3 size = combinedBounds.size;
        return new Vector2(Mathf.Max(size.x, 0.0001f), Mathf.Max(size.z, 0.0001f));
    }

    private void CacheRendererColors()
    {
        if (cachedRenderers != null)
            return;

        cachedRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[cachedRenderers.Length];
        originalBaseColors = new Color[cachedRenderers.Length];
        hadColorProperty = new bool[cachedRenderers.Length];
        hadBaseColorProperty = new bool[cachedRenderers.Length];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Material material = cachedRenderers[i].material;
            hadColorProperty[i] = material.HasProperty("_Color");
            hadBaseColorProperty[i] = material.HasProperty("_BaseColor");

            if (hadColorProperty[i])
                originalColors[i] = material.GetColor("_Color");
            if (hadBaseColorProperty[i])
                originalBaseColors[i] = material.GetColor("_BaseColor");
        }
    }

    private static IEnumerable<Vector3> GetBoundsCorners(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        yield return new Vector3(min.x, min.y, min.z);
        yield return new Vector3(min.x, min.y, max.z);
        yield return new Vector3(min.x, max.y, min.z);
        yield return new Vector3(min.x, max.y, max.z);
        yield return new Vector3(max.x, min.y, min.z);
        yield return new Vector3(max.x, min.y, max.z);
        yield return new Vector3(max.x, max.y, min.z);
        yield return new Vector3(max.x, max.y, max.z);
    }
}
