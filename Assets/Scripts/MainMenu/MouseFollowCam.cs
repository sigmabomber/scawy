using UnityEngine;

public class CameraMouseRotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;          // Smooth delay amount
    public float maxAngleX = 30f;             // Limit for X rotation (up/down)
    public float maxAngleY = 60f;             // Limit for Y rotation (left/right)

    private Quaternion targetRotation;

    void Update()
    {
        // Get mouse position in viewport (0-1)
        Vector3 mouseViewPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);

        // Convert to -1 .. +1 range
        float x = (mouseViewPos.x - 0.5f) * 2f;
        float y = (mouseViewPos.y - 0.5f) * 2f;

        // Calculate desired rotation
        float rotX = -y * maxAngleX; // invert so up = tilt down
        float rotY = x * maxAngleY;

        // Create target rotation
        targetRotation = Quaternion.Euler(rotX, rotY, 0);

        // Smoothly rotate toward the target
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
