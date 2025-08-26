using System.Collections;
using UnityEngine;

public class SelfDestroy : MonoBehaviour
{
    [Tooltip("Time in seconds before the object disappears")]
    [SerializeField] private float LifeTime = 3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, LifeTime);
    }
}
