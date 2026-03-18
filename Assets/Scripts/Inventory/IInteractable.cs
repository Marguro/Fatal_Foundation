namespace Inventory
{
    public interface IInteractable
    {
        string PromptText { get; }
        void Interact(UnityEngine.GameObject interactor);
        void SetHighlight(bool active);
    }
}

