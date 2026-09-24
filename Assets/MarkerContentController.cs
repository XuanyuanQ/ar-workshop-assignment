using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class MarkerContentController : MonoBehaviour
{
    [SerializeField] private GameObject contentPrefab;
    [SerializeField] private float positionSmoothing = 8f;
    [SerializeField] private float rotationSmoothing = 8f;

    private ARTrackedImageManager manager;
    private readonly Dictionary<TrackableId, SmoothedMarkerContent> content = new();

    private void Awake()
    {
        manager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        if (contentPrefab == null)
        {
            Debug.LogError("Assign the Marker Content prefab.", this);
            enabled = false;
            return;
        }

        manager.trackablesChanged.AddListener(OnChanged);
        foreach (var trackedImage in manager.trackables)
            UpdateContent(trackedImage);
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.trackablesChanged.RemoveListener(OnChanged);

        foreach (var instance in content.Values)
            if (instance != null) Destroy(instance);

        content.Clear();
    }

    private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        foreach (var trackedImage in changes.added)
            UpdateContent(trackedImage);

        foreach (var trackedImage in changes.updated)
            UpdateContent(trackedImage);

        foreach (var removed in changes.removed)
        {
            if (content.TryGetValue(removed.Key, out var instance))
            {
                if (instance != null) Destroy(instance);
                content.Remove(removed.Key);
            }
        }
    }

    private void UpdateContent(ARTrackedImage trackedImage)
    {
        bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
        if (!content.TryGetValue(trackedImage.trackableId, out var instance) || instance == null)
        {
            if (!isTracking) return;

            GameObject contentObject = Instantiate(contentPrefab);
            contentObject.transform.SetPositionAndRotation(
                trackedImage.transform.position,
                trackedImage.transform.rotation);

            instance = contentObject.GetComponent<SmoothedMarkerContent>();
            if (instance == null)
                instance = contentObject.AddComponent<SmoothedMarkerContent>();

            ConfigureSwipeSpinner(contentObject);
            instance.Configure(positionSmoothing, rotationSmoothing);
            content[trackedImage.trackableId] = instance;
        }

        if (isTracking)
            instance.SetTarget(trackedImage.transform);

        instance.gameObject.SetActive(isTracking);
    }

    private static void ConfigureSwipeSpinner(GameObject contentObject)
    {
        Transform rotationTarget = FindVisibleTopLevelModel(contentObject.transform);
        var spinner = contentObject.GetComponent<SpinObjectOnMarker>();
        if (spinner == null)
            spinner = rotationTarget.GetComponent<SpinObjectOnMarker>();
        if (spinner == null)
            spinner = contentObject.AddComponent<SpinObjectOnMarker>();

        spinner.enabled = true;
        spinner.SetRotationTarget(rotationTarget);

        foreach (var otherSpinner in contentObject.GetComponentsInChildren<SpinObjectOnMarker>(true))
        {
            if (otherSpinner != spinner)
                otherSpinner.enabled = false;
        }
    }

    private static Transform FindVisibleTopLevelModel(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
        return renderers.Length > 0
            ? GetTopLevelChild(root, renderers[0].transform)
            : root;
    }

    private static Transform GetTopLevelChild(Transform root, Transform child)
    {
        Transform current = child;
        while (current.parent != null && current.parent != root)
            current = current.parent;

        return current;
    }
}

public class SmoothedMarkerContent : MonoBehaviour
{
    private Transform target;
    private float positionSmoothing = 18f;
    private float rotationSmoothing = 18f;

    public void Configure(float targetPositionSmoothing, float targetRotationSmoothing)
    {
        positionSmoothing = Mathf.Max(0f, targetPositionSmoothing);
        rotationSmoothing = Mathf.Max(0f, targetRotationSmoothing);
    }

    public void SetTarget(Transform markerTransform)
    {
        target = markerTransform;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float positionBlend = 1f - Mathf.Exp(-positionSmoothing * Time.deltaTime);
        float rotationBlend = 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, target.position, positionBlend);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, rotationBlend);
    }
}
