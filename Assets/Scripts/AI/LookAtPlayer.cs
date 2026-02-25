using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    private Transform target;
    public float turnSpeed = 8f;
    public float returnSpeed = 4f;

    [Header("Yaw (Left / Right)")]
    public float maxYaw = 60f;


    private bool isQuitting = false;
    private Quaternion initialLocalRotation;
    private bool isReturning = false;
    private Coroutine returnCoroutine;

    void Start()
    {
        initialLocalRotation = transform.localRotation;

        target = FindAnyObjectByType<PlayerController>().transform.Find("Main Camera").transform;
    }

    void LateUpdate()
    {
        if (isReturning) return;

        if (!target)
        {
            ReturnToInitialRotation();
            return;
        }

        Vector3 dir = target.position - transform.position;
        dir = transform.parent.InverseTransformDirection(dir);

        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
       

        yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);

        Quaternion targetRot = Quaternion.Euler(0, yaw, 0);
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            initialLocalRotation * targetRot,
            Time.deltaTime * turnSpeed
        );
    }
    void OnApplicationQuit()
    {
        isQuitting = true;
    }
    void OnDisable()
    {
        if(isQuitting) return;

        ReturnToInitialRotation();
    }

    void OnEnable()
    {
        if (isReturning && returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            isReturning = false;
        }
    }

    private void ReturnToInitialRotation()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
        }
   
            returnCoroutine = StartCoroutine(ReturnToInitialRotationCoroutine());
    }

    private IEnumerator ReturnToInitialRotationCoroutine()
    {
        isReturning = true;
        Quaternion startRotation = transform.localRotation;
        float elapsedTime = 0f;
        float duration = Quaternion.Angle(startRotation, initialLocalRotation) / returnSpeed * 0.02f;

        while (elapsedTime < duration)
        {
            if (!gameObject.activeSelf) yield break;
              elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            transform.localRotation = Quaternion.Slerp(startRotation, initialLocalRotation, t);
            yield return null;
        }

        transform.localRotation = initialLocalRotation;
        isReturning = false;
        returnCoroutine = null;
    }
}