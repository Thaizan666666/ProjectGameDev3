using UnityEngine;

public interface IInteractable
{
    void Interact();
    void SetHighlighted(bool isHighlighted);
    Transform GetTransform();
    bool CanInteract();
}
