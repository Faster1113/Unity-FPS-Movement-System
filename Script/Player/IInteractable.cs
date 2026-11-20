using UnityEngine;

namespace Game.Player
{
    public interface IInteractable
    {
        void Interact(GameObject interactor);
        string GetInteractPrompt();
    }
}
