using UnityEngine;
using UnityEngine.InputSystem;

public class HoverDetector : MonoBehaviour
{
    [SerializeField] private Camera cam;
    GameObject lastHit;

    private void Awake()
    {
        // Auto-find camera if not assigned
        if (cam == null) cam = Camera.main;
    }

    private void FixedUpdate()
    {
        CheckHover();
    }

    private void CheckHover()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject != lastHit)
            {
                if (lastHit != null)
                    lastHit.GetComponent<HoverableObject>()?.OnMouseExit();

                hit.collider.GetComponent<HoverableObject>()?.OnMouseEnter();
                lastHit = hit.collider.gameObject;
            }
        }
        else
        {
            if (lastHit != null)
            {
                lastHit.GetComponent<HoverableObject>()?.OnMouseExit();
                lastHit = null;
            }
        }
    }
}
