using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    public Transform target;         
    public float turnSpeed = 8f;

    [Header("Yaw (Left / Right)")]
    public float maxYaw = 60f;

    [Header("Pitch (Up / Down)")]
    public float maxUp = 25f;
    public float maxDown = 20f;

    Quaternion initialLocalRotation;

    void Start()
    {
        initialLocalRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 dir = target.position - transform.position;
        dir = transform.parent.InverseTransformDirection(dir);

        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float pitch = 0;//-Mathf.Atan2(dir.y, dir.z) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, -maxDown, maxUp);

        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0);
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            initialLocalRotation * targetRot,
            Time.deltaTime * turnSpeed
        );
    }
}
