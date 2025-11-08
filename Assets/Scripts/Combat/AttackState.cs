using System.Collections;
using UnityEngine;

public class AttackState : MonoBehaviour
{
    private Animator animator;
    private Coroutine attackRoutine;
    private DamageDealer playerDamageDealer;

    [Header("Settings")]
    [Tooltip("Layer index of your attack animations (usually 0 or 1)")]
    [SerializeField] private int attackLayerIndex = 0;
    [SerializeField] private string attackTag = "Attack"; // Tag on both attack clips
    [SerializeField] private float attackSpamCooldown = 0.1f; // Prevent button mashing

    [Header("Debug")]
    public float timePassed;
    public float clipLength;
    public bool isAttacking = false;
    private float lastAttackTime = 0f;

    void Start()
    {
        if (TryGetComponent(out Animator foundAnimator))
        {
            animator = foundAnimator;
        }
        if (playerDamageDealer == null)
        {
            playerDamageDealer = gameObject.GetComponentInChildren<DamageDealer>();
        }
    }

    public void TryMove()
    {
        isAttacking = false;

        animator.SetTrigger("Move");
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    public void TryAttack()
    {
        // Tiny spam prevention
        if (Time.time - lastAttackTime < attackSpamCooldown) return;

        isAttacking = true;

        lastAttackTime = Time.time;

        // Always allow re-triggering Attack during attack
        animator.SetTrigger("Attack");

        // Restart the routine (cancels old one automatically)
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }
        attackRoutine = StartCoroutine(AttackSequence());
    }

    public void ForceIdle()
    {
        TryMove();
    }

    private IEnumerator AttackSequence()
    {
        // Wait one frame for new state
        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(attackLayerIndex);

        if (!stateInfo.IsTag(attackTag))
        {
            attackRoutine = null;
            yield break;
        }

        playerDamageDealer.StartDealDamage();

        // Wait for THIS specific animation to finish
        float clipLength = stateInfo.length;
        yield return new WaitForSeconds(clipLength);

        isAttacking = false;
        attackRoutine = null;
        playerDamageDealer.EndDealDamage();
        TryMove();
    }

    // Animation event at end of clips (optional)
    public void OnAttackEnd()
    {
        attackRoutine = null;
    }
}
