using UnityEngine;

public class CameraMouseRotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;
    public float maxAngleX = 30f;
    public float maxAngleY = 60f;

    private Quaternion originalRotation;  
    private Quaternion targetRotation;

    void Start()
    {
        originalRotation = transform.localRotation;
    }

    void Update()
    {
        Vector3 mouseViewPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);

        float x = (mouseViewPos.x - 0.5f) * 2f;
        float y = (mouseViewPos.y - 0.5f) * 2f;

        float rotX = -y * maxAngleX;
        float rotY = x * maxAngleY;

        Quaternion mouseOffset = Quaternion.Euler(rotX, rotY, 0);

        targetRotation = originalRotation * mouseOffset;

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
