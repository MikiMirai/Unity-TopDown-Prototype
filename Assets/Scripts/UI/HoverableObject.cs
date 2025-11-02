using UnityEngine;
using DG.Tweening;

public class HoverableObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float hoverOffset = 0.2f;
    [SerializeField] private float tweenDuration = 0.2f;
    [SerializeField] private bool moveToRight = false;

    private Vector3 originalPosition;
    private Tween moveTween;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    public void OnMouseEnter()
    {
        // Kill any existing tween to avoid stacking
        moveTween?.Kill();

        Vector3 targetPosition;
        if (moveToRight)
        {
            targetPosition = originalPosition + Vector3.right * hoverOffset;
        } 
        else
        {
            targetPosition = originalPosition + Vector3.forward * hoverOffset;
        }
        moveTween = transform.DOLocalMove(targetPosition, tweenDuration).SetEase(Ease.OutQuad);
    }

    public void OnMouseExit()
    {
        moveTween?.Kill();
        moveTween = transform.DOLocalMove(originalPosition, tweenDuration).SetEase(Ease.OutQuad);
    }
}
