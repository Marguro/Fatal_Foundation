using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;

namespace Inventory
{
    [RequireComponent(typeof(NetworkObject))]
    public class WorldItem : NetworkBehaviour
    {
        [BoxGroup("Item Data")]
        public ItemData itemData;

        [BoxGroup("Visual Feedback")]
        [SerializeField] private GameObject highlightEffect;

        [BoxGroup("Bob & Rotate Animation")]
        [SerializeField] private bool enableBobAnimation = true;

        [BoxGroup("Bob & Rotate Animation")]
        [SerializeField] private float bobHeight = 0.15f;

        [BoxGroup("Bob & Rotate Animation")]
        [SerializeField] private float bobSpeed = 1.5f;

        [BoxGroup("Bob & Rotate Animation")]
        [SerializeField] private float rotateSpeed = 90f;

        private Vector3 _startPosition;
        private float _bobTimer;

        public override void OnNetworkSpawn()
        {
            if (highlightEffect != null)
                highlightEffect.SetActive(false);
        }

        private void Start()
        {
            _startPosition = transform.position;
            // Highlight disabled in OnNetworkSpawn or here is fine
        }

        private void Update()
        {
            if (!enableBobAnimation) return;

            _bobTimer += Time.deltaTime * bobSpeed;
            float newY = _startPosition.y + Mathf.Sin(_bobTimer) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        public void Interact()
        {
            if (itemData == null) return;
            
            // Client-side prediction / Local pickup logic
            if (PlayerInventory.Instance != null)
            {
               bool pickedUp = PlayerInventory.Instance.PickUpItem(itemData);
               if (pickedUp)
               {
                   // Request server to despawn this object
                   RequestDespawnServerRpc();
               }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestDespawnServerRpc()
        {
            // Server validates and despawns
            GetComponent<NetworkObject>().Despawn();
        }
        
        public void SetHighlight(bool active)
        {
            if (highlightEffect != null)
                highlightEffect.SetActive(active);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

#if UNITY_EDITOR
            if (itemData != null)
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.5f,
                    $"{itemData.itemName}\n{itemData.weight}kg | ${itemData.scrapValue}"
                );
#endif
        }
    }
}
