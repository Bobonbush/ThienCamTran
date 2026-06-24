public interface IInteractable
{
    bool CanInteract { get; }
    void Interact(PlayerController player);
}
