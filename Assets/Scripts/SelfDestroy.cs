using UnityEngine;

public class SelfDestroy : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Time in seconds before the object disappears")]
    [SerializeField] private float LifeTime = 3f;
    [SerializeField] private string ignoreMask;

    void Start()
    {
        Destroy(gameObject, LifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ignoreMask))
        {
            Destroy(gameObject);
        }
    }
}
