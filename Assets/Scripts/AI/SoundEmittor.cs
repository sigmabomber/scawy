using Doody.AI.Events;
using Doody.GameEvents;
using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    [SerializeField] private float soundRadius = 15f;

    public void EmitSound()
    {
        EmitSound(transform.position, soundRadius, 1f);
    }

    public void EmitSound(float intensity)
    {
        EmitSound(transform.position, soundRadius, intensity);
    }

    public void EmitSound(Vector3 position, float radius, float intensity = 1f)
    {
        // Publish sound event - all nearby AI will hear it
        Events.Publish(new SoundHeardEvent
        {
            AI = gameObject,
            SoundPosition = position,
            SoundIntensity = intensity
        });

        Debug.Log($"Sound emitted at {position} (radius: {radius}, intensity: {intensity})");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, soundRadius);
    }
}

