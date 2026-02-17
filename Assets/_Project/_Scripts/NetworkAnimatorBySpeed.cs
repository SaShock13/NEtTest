using UnityEngine;

public class NetworkAnimatorBySpeed : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ServerMovementWithAnim movement;

    private float _smoothedSpeed;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<ServerMovementWithAnim>();
    }

    private void Update()
    {
        if (animator == null || movement == null) return;

        float target = movement.IsWalking.Value ? 1f : 0f;
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, target, 10f * Time.deltaTime);
        animator.SetFloat("Speed", _smoothedSpeed);
    }
}
