using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMelee : MonoBehaviour
{
    [SerializeField] private int health = 3;
    [SerializeField] private float deathTime = 0f;
    [SerializeField] private string playerTag = "Player";

    [Header("Combat")]
    [SerializeField] float attackCD = 3f;
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] float aggroRange = 6f;
    [SerializeField] float damageDuration = 0.5f;

    GameObject player;
    NavMeshAgent agent;
    Animator animator;
    DamageDealer damageDealer;

    float timePassed;
    float newDestinationCD = 0.5f;
    private Coroutine attackRoutine;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag(playerTag);
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (damageDealer == null)
        {
            damageDealer = GetComponentInChildren<DamageDealer>();
        }
    }

    private void Update()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude / agent.speed);
        }

        if (timePassed >= attackCD)
        {
            if (Vector3.Distance(player.transform.position, transform.position) <= attackRange)
            {
                animator.SetTrigger("Attack");
                attackRoutine = StartCoroutine(DealDamage());
                timePassed = 0;
            }
        }
        timePassed += Time.deltaTime;

        if (newDestinationCD <= 0 && Vector3.Distance(player.transform.position, transform.position) <= aggroRange)
        {
            newDestinationCD = 0.5f;
            agent.SetDestination(player.transform.position);
        }
        newDestinationCD -= Time.deltaTime;
        transform.LookAt(player.transform);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (animator != null) animator.SetTrigger("Damage");

        if (health <= 0) Invoke(nameof(Die), deathTime);
    }
    private void Die()
    {
        Destroy(this.gameObject);
    }

    IEnumerator DealDamage()
    {
        damageDealer.StartDealDamage();

        yield return new WaitForSeconds(damageDuration);

        damageDealer.EndDealDamage();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}
