using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARStatus : MonoBehaviour
{
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARTrackedImageManager imageManager;
    [SerializeField] private TapToPlace placement;

    private GUIStyle style;

    private void OnGUI()
    {
        style ??= CreateStyle();

        float scale = Mathf.Max(1f, Screen.width / 600f);
        style.fontSize = Mathf.RoundToInt(14f * scale);

        int planes = 0;
        int trackedImages = 0;

        if (planeManager != null)
            foreach (var plane in planeManager.trackables) planes++;

        if (imageManager != null)
            foreach (var image in imageManager.trackables)
                if (image.trackingState == TrackingState.Tracking) trackedImages++;

        string status = $"Session: {ARSession.state}\n" +
            $"Reason: {ARSession.notTrackingReason}\n" +
            $"Planes: {planes} | Images tracking: {trackedImages}\n" +
            $"Objects placed: {(placement != null ? placement.PlacementCount : 0)}";

        Rect safe = Screen.safeArea;
        float top = Screen.height - safe.yMax;
        GUI.Box(new Rect(safe.x + 8f * scale, top + 8f * scale,
            safe.width - 16f * scale, 100f * scale), status, style);
    }

    private static GUIStyle CreateStyle()
    {
        var boxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        boxStyle.normal.textColor = Color.white;
        return boxStyle;
    }
}
