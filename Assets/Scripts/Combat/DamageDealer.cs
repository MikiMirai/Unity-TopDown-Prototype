using System.Collections.Generic;
using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    List<GameObject> targetsHit;

    [SerializeField] private ComboSystem comboSystem;
    [SerializeField] private float weaponLength;
    [SerializeField] private int weaponDamage = 1;
    [SerializeField] private LayerMask weaponMask;

    private void Start()
    {
        targetsHit = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, weaponLength, weaponMask))
        {
            if (hit.transform.TryGetComponent(out Health health) && !targetsHit.Contains(hit.transform.gameObject))
            {
                Debug.Log("Dealt Damage!");
                health.TakeDamage(weaponDamage);
                targetsHit.Add(hit.transform.gameObject);

                if (comboSystem != null)
                {
                    comboSystem.RegisterLightAttack();
                }
            }
        }
    }

    public void StartDealDamage()
    {
        // Debug.Log("--- Damage Enabled");
        targetsHit.Clear();
    }

    public void EndDealDamage()
    {
        // Debug.Log("--- Damage Disabled");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * weaponLength);
    }
}
