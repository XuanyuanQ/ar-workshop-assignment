using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TapToPlace : MonoBehaviour
{
    [SerializeField] private GameObject objectToPlace;
    [SerializeField] private ARRaycastManager raycastManager;

    private readonly List<ARRaycastHit> hits = new();
    public int PlacementCount { get; private set; }

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = GetComponent<ARRaycastManager>();

        if (objectToPlace == null || raycastManager == null)
        {
            Debug.LogError("Assign Object To Place and Raycast Manager.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!TryGetPress(out Vector2 screenPoint)) return;
        if (ARSession.state != ARSessionState.SessionTracking) return;
        if (!raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon)) return;

        Pose pose = hits[0].pose;
        Instantiate(objectToPlace, pose.position, pose.rotation);
        PlacementCount++;
    }

    private static bool TryGetPress(out Vector2 screenPoint)
    {
        screenPoint = default;
#if UNITY_EDITOR
        if (Mouse.current != null &&
            !Mouse.current.rightButton.isPressed &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPoint = Mouse.current.position.ReadValue();
            return true;
        }
#endif
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                screenPoint = touch.position.ReadValue();
                return true;
            }
        }

        return false;
    }
}
