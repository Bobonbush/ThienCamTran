public interface IInteractable
{
    public enum Type
    {
        Item,
        Non_needPickup,
        Object,
        Door,
        Purifying,
        Save,
        Dialog
        
    };

    bool CanInteract { get; }
    void Interact(PlayerController player);
    Type GetType();

}
