using Unity.Netcode;
using UnityEngine;

public class NetworkAnimatorBySpeed : NetworkBehaviour
{
    [SerializeField] private Animator animator;

    private Vector3 lastPos;
    private float smoothedSpeed;

    private void Start()
    {
        lastPos = transform.position;
    }

    private void Update()
    {
        // Скорость считаем на каждом клиенте локально
        Vector3 delta = transform.position - lastPos;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        lastPos = transform.position;

        // Сглаживание, чтобы не дёргалось из-за сетевых шагов
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 12f * Time.deltaTime);

        // Нормализуем под твою скорость
        float normalized = Mathf.InverseLerp(0f, 4f, smoothedSpeed);

        animator.SetFloat("Speed", normalized);
    }
}
