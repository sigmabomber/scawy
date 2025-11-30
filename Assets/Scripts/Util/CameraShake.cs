using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    // INTERNAL STATE
    private Vector3 currentRecoilRotation;
    private Vector3 recoilRotVelocity;

    private Vector3 currentRecoilPosition;
    private Vector3 recoilPosVelocity;

    private Vector3 targetRecoilRotation;
    private Vector3 targetRecoilPosition;

    private Vector3 currentSway;
    private float swayTimer;

    [Header("Recoil Settings")]
    public float springFrequency = 14f;
    public float springDamping = 0.25f;
    public float returnDelay = 0.05f;

    private float returnTimer;

    [Header("Sway Settings")]
    public float swayAmount = 0.5f;
    public float swaySpeed = 3f;
    public float swayDuration = 0.5f;

    public Vector3 CurrentRecoilRotation => currentRecoilRotation + currentSway;
    public Vector3 CurrentRecoilPosition => currentRecoilPosition;

    private void Update()
    {
        float dt = Time.deltaTime;

        if (returnTimer > 0)
        {
            returnTimer -= dt;
        }
        else
        {
            currentRecoilRotation = Spring(currentRecoilRotation, ref recoilRotVelocity, springFrequency, springDamping, dt);
            currentRecoilPosition = Spring(currentRecoilPosition, ref recoilPosVelocity, springFrequency * 0.7f, springDamping, dt);
        }

        if (swayTimer > 0)
        {
            swayTimer -= dt;

            float t = swayTimer / swayDuration;

            currentSway.x = Mathf.Sin(Time.time * swaySpeed * 2.5f) * swayAmount * t;
            currentSway.y = Mathf.Cos(Time.time * swaySpeed * 3.2f) * swayAmount * 0.6f * t;
        }
        else
        {
            currentSway = Vector3.Lerp(currentSway, Vector3.zero, dt * 10f);
        }
    }
    private Vector3 Spring(Vector3 x, ref Vector3 v, float freq, float damping, float dt)
    {
        float k = freq * freq;
        float d = damping * 2f * freq;

        v += (-k * x - d * v) * dt;
        x += v * dt;

        return x;
    }

    public void ApplyRecoil(Vector3 rotationRecoil, Vector3 positionRecoil)
    {
        targetRecoilRotation = rotationRecoil;
        targetRecoilPosition = positionRecoil;

        currentRecoilRotation += rotationRecoil;
        currentRecoilPosition += positionRecoil;

        recoilRotVelocity -= rotationRecoil * 2f;
        recoilPosVelocity -= positionRecoil * 1.4f;

        returnTimer = returnDelay;

        swayTimer = swayDuration;
    }
}
