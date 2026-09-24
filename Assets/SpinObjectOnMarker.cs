using UnityEngine;
using UnityEngine.InputSystem;

public class SpinObjectOnMarker : MonoBehaviour
{
    [SerializeField] private float swipeToSpinMultiplier = 0.35f;
    [SerializeField] private float spinDamping = 2.4f;
    [SerializeField] private float stopSpeed = 3f;
    [SerializeField] private float maxSpinSpeed = 1800f;
    [SerializeField] private float minimumSwipePixels = 3f;
    [SerializeField] private float settleSmoothing = 10f;

    private Vector2 pressPosition;
    private Vector2 lastPointerPosition;
    private Transform rotationTarget;
    private Quaternion settleRotation;
    private float pressTime;
    private Vector3 spinAxis = Vector3.up;
    private float spinSpeed;
    private bool isDragging;
    private bool isSettling;

    public void SetRotationTarget(Transform target)
    {
        rotationTarget = target != null ? target : transform;
    }

    private void Awake()
    {
        if (rotationTarget == null)
            rotationTarget = FindVisibleTopLevelModel(transform);
    }

    private void Update()
    {
        HandleSwipeInput();
        ApplySpin();
        ApplySettle();
    }

    private void HandleSwipeInput()
    {
        if (!TryGetPointer(out Vector2 pointerPosition, out bool pressed, out bool held, out bool released))
            return;

        if (pressed)
        {
            isDragging = true;
            isSettling = false;
            pressPosition = pointerPosition;
            lastPointerPosition = pointerPosition;
            pressTime = Time.time;
        }

        if (held && isDragging)
        {
            Vector2 delta = pointerPosition - lastPointerPosition;
            lastPointerPosition = pointerPosition;

            if (delta.sqrMagnitude >= minimumSwipePixels * minimumSwipePixels)
                AddSpinFromSwipe(delta);
        }

        if (released && isDragging)
        {
            Vector2 releaseDelta = pointerPosition - pressPosition;
            float duration = Mathf.Max(Time.time - pressTime, 0.02f);
            if (releaseDelta.sqrMagnitude >= minimumSwipePixels * minimumSwipePixels)
                AddSpinFromSwipe(releaseDelta / duration * Time.deltaTime);

            isDragging = false;
        }
    }

    private void AddSpinFromSwipe(Vector2 delta)
    {
        Camera camera = Camera.main;
        Vector3 screenAxis = new(delta.y, -delta.x, 0f);
        Vector3 worldAxis = camera != null
            ? camera.transform.TransformDirection(screenAxis).normalized
            : screenAxis.normalized;

        if (worldAxis.sqrMagnitude < 0.0001f)
            return;

        isSettling = false;
        spinAxis = worldAxis;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        spinSpeed = Mathf.Clamp(speed * swipeToSpinMultiplier, 0f, maxSpinSpeed);
    }

    private void ApplySpin()
    {
        if (spinSpeed <= 0f)
            return;

        Transform target = rotationTarget != null ? rotationTarget : transform;
        target.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);
        spinSpeed *= Mathf.Exp(-spinDamping * Time.deltaTime);

        if (spinSpeed <= stopSpeed)
        {
            spinSpeed = 0f;
            BeginSettle();
        }
    }

    private void BeginSettle()
    {
        Transform target = rotationTarget != null ? rotationTarget : transform;
        settleRotation = Quaternion.Euler(
            RoundToRightAngle(target.localEulerAngles.x),
            RoundToRightAngle(target.localEulerAngles.y),
            RoundToRightAngle(target.localEulerAngles.z));
        isSettling = true;
    }

    private void ApplySettle()
    {
        if (!isSettling || spinSpeed > 0f || isDragging)
            return;

        Transform target = rotationTarget != null ? rotationTarget : transform;
        float blend = 1f - Mathf.Exp(-settleSmoothing * Time.deltaTime);
        target.localRotation = Quaternion.Slerp(target.localRotation, settleRotation, blend);

        if (Quaternion.Angle(target.localRotation, settleRotation) <= 0.2f)
        {
            target.localRotation = settleRotation;
            isSettling = false;
        }
    }

    private static float RoundToRightAngle(float angle)
    {
        return Mathf.Round(angle / 90f) * 90f;
    }

    private static bool TryGetPointer(
        out Vector2 pointerPosition,
        out bool pressed,
        out bool held,
        out bool released)
    {
        pointerPosition = default;
        pressed = false;
        held = false;
        released = false;

#if UNITY_EDITOR
        if (Mouse.current != null && !Mouse.current.rightButton.isPressed)
        {
            pointerPosition = Mouse.current.position.ReadValue();
            pressed = Mouse.current.leftButton.wasPressedThisFrame;
            held = Mouse.current.leftButton.isPressed;
            released = Mouse.current.leftButton.wasReleasedThisFrame;
            return pressed || held || released;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointerPosition = touch.position;
            pressed = touch.phase == UnityEngine.TouchPhase.Began;
            held = touch.phase == UnityEngine.TouchPhase.Moved ||
                   touch.phase == UnityEngine.TouchPhase.Stationary;
            released = touch.phase == UnityEngine.TouchPhase.Ended ||
                       touch.phase == UnityEngine.TouchPhase.Canceled;
            return true;
        }
#endif
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            pointerPosition = touch.position.ReadValue();
            pressed = touch.press.wasPressedThisFrame;
            held = touch.press.isPressed;
            released = touch.press.wasReleasedThisFrame;
            return pressed || held || released;
        }

        return false;
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
