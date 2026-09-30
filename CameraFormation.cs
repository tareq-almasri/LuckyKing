using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFormation : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float zoomSpeed = 60f;
    public float minY = 10f;
    public float maxY = 30f;

    public float minX = -15f;
    public float maxX = 15f;

    public float minZ = -15f;
    public float maxZ = 0f;

    private Vector2 lastMousePosition;

    void Start()
    {
        // Ensure the camera starts at a valid position
        transform.position = new Vector3(0, 20, -10);
    }
    void Update()
    {
        // Unified movement vector
        Vector3 move = Vector3.zero;
        // Keyboard movement (WASD)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) move += transform.right * -1f;
            if (Keyboard.current.dKey.isPressed) move += transform.right * 1f;
            if (Keyboard.current.sKey.isPressed) move += transform.forward * -1f;
            if (Keyboard.current.wKey.isPressed) move += transform.forward * 1f;
        }
        move *= moveSpeed * Time.deltaTime;

        // Mouse pan with right button
        Vector3 pan = Vector3.zero;
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                lastMousePosition = Mouse.current.position.ReadValue();
            }
            else
            {
                Vector2 mouseDelta = Mouse.current.position.ReadValue() - lastMousePosition;
                pan.x -= mouseDelta.x * 0.05f;
                pan.z -= mouseDelta.y * 0.05f;
                lastMousePosition = Mouse.current.position.ReadValue();
            }
        }
        move += pan;

        // Touch gestures (filter only moving touches)
        if (Touchscreen.current != null)
        {
            var activeTouches = new System.Collections.Generic.List<UnityEngine.InputSystem.Controls.TouchControl>();
            foreach (var t in Touchscreen.current.touches)
            {
                if (t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
                    activeTouches.Add(t);
            }

            if (activeTouches.Count == 1)
            {
                var touch = activeTouches[0];
                // Prevent pan if touch started on a unit or if any unit is being dragged
                bool anyUnitDragging = false;
                foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
                {
                    if (unit.isDragging)
                    {
                        anyUnitDragging = true;
                        break;
                    }
                }
                Vector2 screenStart = touch.startPosition.ReadValue();
                Ray ray = Camera.main.ScreenPointToRay(screenStart);
                bool hitUnit = false;
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.GetComponent<Unit>() != null)
                        hitUnit = true;
                }
                if (!hitUnit && !anyUnitDragging)
                {
                    Vector2 delta = touch.delta.ReadValue();
                    Vector3 tpan = 0.05f * delta.x * -transform.right - 0.05f * delta.y * transform.forward;
                    tpan.y = 0f;
                    move += tpan;
                }
            }
            else if (activeTouches.Count >= 2)
            {
                var t0 = activeTouches[0];
                var t1 = activeTouches[1];
                Vector2 t0Prev = t0.position.ReadValue() - t0.delta.ReadValue();
                Vector2 t1Prev = t1.position.ReadValue() - t1.delta.ReadValue();
                float prevDist = (t0Prev - t1Prev).magnitude;
                float currDist = (t0.position.ReadValue() - t1.position.ReadValue()).magnitude;
                float pinchDelta = currDist - prevDist;
                move.y -= pinchDelta * 0.05f; // Zoom
            }
        }

        Vector3 newPosition = transform.position + move;

        // Clamp X and Z
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);

        // Zooming (Y axis)
        float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
        newPosition.y = Mathf.Clamp(newPosition.y - scroll * zoomSpeed * Time.deltaTime, minY, maxY);

        transform.position = newPosition;
    }
}
