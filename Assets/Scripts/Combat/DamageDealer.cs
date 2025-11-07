using System.Collections.Generic;
using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    bool canDealDamage;
    List<GameObject> hasDealthDamage;

    [SerializeField] private float weaponLength;
    [SerializeField] private int weaponDamage;
    [SerializeField] private LayerMask weaponMask;

    private void Start()
    {
        canDealDamage = false;
        hasDealthDamage = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, weaponLength, weaponMask))
        {
            if (hit.transform.TryGetComponent(out Health health) && !hasDealthDamage.Contains(hit.transform.gameObject))
            {
                Debug.Log("Dealt Damage!");
                health.TakeDamage(weaponDamage);
                hasDealthDamage.Add(hit.transform.gameObject);
            }
        }
    }

    public void StartDealDamage()
    {
        Debug.Log("--- Damage Enabled");
        canDealDamage = true;
        hasDealthDamage.Clear();
    }

    public void EndDealDamage()
    {
        Debug.Log("--- Damage Disabled");
        canDealDamage = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * weaponLength);
    }
}
