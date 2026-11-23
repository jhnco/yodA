using UnityEngine;

public class ControllerSwitcher : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private RuntimeAnimatorController startController;   // cactus mouth closed
    [SerializeField] private RuntimeAnimatorController triggeredController; // cactus mouth opened

    [Header("Settings")]
    [SerializeField] private string triggerTag = "Player";
    [SerializeField] private bool switchBackOnExit = false;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.runtimeAnimatorController = startController;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag)) return;
        animator.runtimeAnimatorController = triggeredController;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!switchBackOnExit || !other.CompareTag(triggerTag)) return;
        animator.runtimeAnimatorController = startController;
    }
}