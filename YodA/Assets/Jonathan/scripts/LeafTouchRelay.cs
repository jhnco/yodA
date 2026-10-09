using UnityEngine;

/// <summary>
/// Added automatically to every leaf by JungleLeafBorder.
/// Forwards trigger enter/exit events to the owner. You don't need to add this yourself,
/// but keep this file in your project next to JungleLeafBorder.cs.
/// </summary>
public class LeafTouchRelay : MonoBehaviour
{
    [HideInInspector] public JungleLeafBorder owner;
    [HideInInspector] public int index;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.NotifyTouch(index, other, true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (owner != null) owner.NotifyTouch(index, other, false);
    }
}