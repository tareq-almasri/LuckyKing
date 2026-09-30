using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class CameraBattle : MonoBehaviour
{
    private Animator animator;
    public float moveSpeed = 10f;
    public float zoomSpeed = 10f;
    public float minY = 10f;
    public float maxY = 30f;

    public float minX = -20f;
    public float maxX = 20f;

    public float minZ = -20f;
    public float maxZ = 20f;

    private Vector2 lastMousePosition;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Ensure the camera starts at a valid position
        // transform.position = new Vector3(0, 20, -50);
    }
    void Update()
    {
        // Disable Animator when intro animation is done
        if (animator != null && animator.enabled)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("empty"))
            {
                animator.enabled = false;
                BattleManager.Instance.StartBattle();
            }
        }

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

        // Touch gestures
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
                Vector2 delta = touch.delta.ReadValue();
                move.x -= delta.x * 0.05f;
                move.z -= delta.y * 0.05f;
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

        // Camera pan with right mouse button (local to camera)
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                lastMousePosition = Mouse.current.position.ReadValue();
            }
            else
            {
                Vector2 mouseDelta = Mouse.current.position.ReadValue() - lastMousePosition;
                // Pan camera: move opposite to mouse drag direction, local axes
                Vector3 pan = 0.05f * mouseDelta.x * -transform.right - 0.05f * mouseDelta.y * transform.forward;
                pan.y = 0f; // Prevent Y movement during pan
                newPosition += pan;
                lastMousePosition = Mouse.current.position.ReadValue();
            }
        }

        // Camera rotation with middle mouse button
        if (Mouse.current != null && Mouse.current.middleButton.isPressed)
        {
            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                lastMousePosition = Mouse.current.position.ReadValue();
            }
            else
            {
                Vector2 mouseDelta = Mouse.current.position.ReadValue() - lastMousePosition;
                float rotationY = mouseDelta.x * 0.2f; // Yaw (horizontal)
                float rotationX = mouseDelta.y * 0.2f; // Pitch (vertical)

                // Only rotate around Y (yaw) and X (pitch), prevent Z (roll)
                transform.Rotate(Vector3.up, rotationY, Space.World);
                transform.Rotate(Vector3.right, rotationX, Space.Self); // Use right for pitch, Space.Self to avoid roll
                lastMousePosition = Mouse.current.position.ReadValue();
            }
        }

        // Clamp X and Z
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);

        // Zooming (Y axis)
        float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
        newPosition.y = Mathf.Clamp(newPosition.y - scroll * zoomSpeed * Time.deltaTime, minY, maxY);

        transform.position = newPosition;
    }
}
