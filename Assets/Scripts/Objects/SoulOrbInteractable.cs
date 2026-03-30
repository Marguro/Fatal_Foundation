using UnityEngine;
using Unity.Netcode;
using Perks;
using Inventory;

namespace Objects
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider))]
    public class SoulOrbInteractable : NetworkBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] private int soulOrbAmount = 1;
        [SerializeField] private string promptText = "Press E to collect Soul Orb";

        public string PromptText => promptText;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                // Most interactive objects shouldn't block the player, but rather act as triggers or specific collision layers 
                // Adjust this depending on your game's collision needs
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!IsServer)
            {
                // When a client interacts, send RPC to server to handle pickup
                RequestPickupServerRpc();
                return;
            }

            HandlePickup(interactor);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestPickupServerRpc(ServerRpcParams rpcParams = default)
        {
            // The sender client ID
            ulong clientId = rpcParams.Receive.SenderClientId;

            // Find the player object of the sender
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(clientId, out NetworkObject playerObj))
            {
                HandlePickup(playerObj.gameObject);
            }
        }

        private void HandlePickup(GameObject interactor)
        {
            // Give Soul Orb
            // Since SoulOrbCurrency is likely owned by the local player who picks it up,
            // we'll find the specific SoulOrbCurrency component on the interactor or the locally managed instance.
            
            // To be safe, we send a ClientRpc to the picking up player to update their currency
            NetworkObject interactorNetObj = interactor.GetComponent<NetworkObject>();
            if (interactorNetObj != null)
            {
                GiveSoulOrbClientRpc(interactorNetObj.OwnerClientId);
            }

            // Destroy the orb object on the network
            NetworkObject.Despawn(true);
        }

        [ClientRpc]
        private void GiveSoulOrbClientRpc(ulong targetClientId)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                if (SoulOrbCurrency.Instance != null)
                {
                    SoulOrbCurrency.Instance.AddSoulOrbs(soulOrbAmount);
                }
            }
        }

        public void SetHighlight(bool active)
        {
            // Optional: Handle glowing or outlines here
        }
    }
}

