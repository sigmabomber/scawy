using UnityEngine;

public class FleshyWallAnimator : MonoBehaviour
{
    [Header("Blendshape Settings")]
    [SerializeField] private SkinnedMeshRenderer skinnedMesh;
    [SerializeField] private int blendshape1Index = 0;
    [SerializeField] private int blendshape2Index = 1;

    [Header("Player Detection")]
    public Transform player;
    [SerializeField] private float maxDetectionDistance = 10f;
    [SerializeField] private float minDetectionDistance = 2f;
    [SerializeField] private float playerSpeedMultiplier = 3f;

    [Header("Speed Settings")]
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 2.5f;
    [SerializeField] private float speedChangeRate = 0.3f;

    [Header("Amplitude Settings")]
    [SerializeField] private float minAmplitude = 30f;
    [SerializeField] private float maxAmplitude = 100f;
    [SerializeField] private float amplitudeChangeRate = 0.2f;

    [Header("Randomness")]
    [SerializeField] private float noiseScale1 = 0.5f;
    [SerializeField] private float noiseScale2 = 0.7f;
    [SerializeField] private float noiseInfluence = 0.15f;

    [Header("Heartbeat")]
    [SerializeField] private bool enableHeartbeat = true;
    [SerializeField] private float heartbeatRate = 1.2f;
    [SerializeField] private float heartbeatStrength = 0.3f;
    [SerializeField] private float heartbeatSharpness = 8f;

    private float time1;
    private float time2;
    private float noiseOffset1;
    private float noiseOffset2;
    private float speedOffset1;
    private float speedOffset2;
    private float ampOffset1;
    private float ampOffset2;

    private float currentSpeed1;
    private float currentSpeed2;
    private float currentAmp1;
    private float currentAmp2;

    void Start()
    {
        if (skinnedMesh == null)
        {
            skinnedMesh = GetComponent<SkinnedMeshRenderer>();
        }

        noiseOffset1 = Random.Range(0f, 100f);
        noiseOffset2 = Random.Range(0f, 100f);
        speedOffset1 = Random.Range(0f, 100f);
        speedOffset2 = Random.Range(0f, 100f);
        ampOffset1 = Random.Range(0f, 100f);
        ampOffset2 = Random.Range(0f, 100f);

        currentSpeed1 = Random.Range(minSpeed, maxSpeed);
        currentSpeed2 = Random.Range(minSpeed, maxSpeed);
        currentAmp1 = Random.Range(minAmplitude, maxAmplitude);
        currentAmp2 = Random.Range(minAmplitude, maxAmplitude);
    }

    void Update()
    {
        float proximityFactor = 0f;
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            proximityFactor = Mathf.InverseLerp(maxDetectionDistance, minDetectionDistance, distance);
            proximityFactor = Mathf.Clamp01(proximityFactor);
        }

        float speedBoost = 1f + (proximityFactor * (playerSpeedMultiplier - 1f));

        float speedNoise1 = Mathf.PerlinNoise(Time.time * speedChangeRate + speedOffset1, 0f);
        float speedNoise2 = Mathf.PerlinNoise(Time.time * speedChangeRate + speedOffset2, 0f);
        currentSpeed1 = Mathf.Lerp(minSpeed, maxSpeed, speedNoise1) * speedBoost;
        currentSpeed2 = Mathf.Lerp(minSpeed, maxSpeed, speedNoise2) * speedBoost;

        float ampNoise1 = Mathf.PerlinNoise(Time.time * amplitudeChangeRate + ampOffset1, 0f);
        float ampNoise2 = Mathf.PerlinNoise(Time.time * amplitudeChangeRate + ampOffset2, 0f);
        currentAmp1 = Mathf.Lerp(minAmplitude, maxAmplitude, ampNoise1);
        currentAmp2 = Mathf.Lerp(minAmplitude, maxAmplitude, ampNoise2);

        time1 += Time.deltaTime * currentSpeed1;
        time2 += Time.deltaTime * currentSpeed2;

        float sine1 = Mathf.Sin(time1);
        float sine2 = Mathf.Sin(time2);

        float noise1 = Mathf.PerlinNoise(time1 * noiseScale1 + noiseOffset1, 0f) * 2f - 1f;
        float noise2 = Mathf.PerlinNoise(time2 * noiseScale2 + noiseOffset2, 0f) * 2f - 1f;

        float blend1 = Mathf.Lerp(sine1, noise1, noiseInfluence);
        float blend2 = Mathf.Lerp(sine2, noise2, noiseInfluence);

        if (enableHeartbeat)
        {
            float heartbeatTime = Time.time * heartbeatRate * speedBoost * Mathf.PI * 2f;

            float pulse1 = Mathf.Pow(Mathf.Max(0, Mathf.Sin(heartbeatTime)), heartbeatSharpness);
            float pulse2 = Mathf.Pow(Mathf.Max(0, Mathf.Sin(heartbeatTime + 0.4f)), heartbeatSharpness) * 0.7f;
            float heartbeat = (pulse1 + pulse2) * heartbeatStrength;

            blend1 += heartbeat;
            blend2 += heartbeat;

            blend1 = Mathf.Clamp(blend1, -1f, 1f);
            blend2 = Mathf.Clamp(blend2, -1f, 1f);
        }

        float value1 = (blend1 * 0.5f + 0.5f) * currentAmp1;
        float value2 = (blend2 * 0.5f + 0.5f) * currentAmp2;

        skinnedMesh.SetBlendShapeWeight(blendshape1Index, value1);
        skinnedMesh.SetBlendShapeWeight(blendshape2Index, value2);
    }
}