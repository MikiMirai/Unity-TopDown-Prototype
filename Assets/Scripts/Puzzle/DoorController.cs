using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTrigger = "Open";

    private void Awake()
    {
        if (TryGetComponent<Animator>(out Animator foundAnimator))
        {
            doorAnimator = foundAnimator;
        }
    }

    /// <summary>
    /// Activate the specified openTrigger in the Inspector if Animator exists.
    /// </summary>
    public void OpenDoor()
    {
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger(openTrigger);
        }
    }
}
