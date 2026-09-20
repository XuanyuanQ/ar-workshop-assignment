using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class MarkerContentController : MonoBehaviour
{
    [SerializeField] private GameObject contentPrefab;

    private ARTrackedImageManager manager;
    private readonly Dictionary<TrackableId, GameObject> content = new();

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

            instance = Instantiate(contentPrefab, trackedImage.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            content[trackedImage.trackableId] = instance;
        }

        instance.SetActive(isTracking);
    }
}
