using UnityEngine;

public class CameraLookSway : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public float swayAmount = 1.5f;
    public float smoothSpeed = 8f;

    private Vector2 mouseInput;
    private Vector2 currentRotation;

    void Update()
    {
        mouseInput.x = Input.GetAxis("Mouse X");
        mouseInput.y = Input.GetAxis("Mouse Y");

        Vector2 targetRotation = new Vector2(
            -mouseInput.y * swayAmount,
            mouseInput.x * swayAmount
        );

        currentRotation = Vector2.Lerp(
            currentRotation,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );

        transform.localRotation = Quaternion.Euler(
            currentRotation.x,
            currentRotation.y,
            0f
        );
    }


}