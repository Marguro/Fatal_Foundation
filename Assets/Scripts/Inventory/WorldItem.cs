using NaughtyAttributes;
using UnityEngine;

namespace Inventory
{
    public class WorldItem : MonoBehaviour
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

        private void Start()
        {
            _startPosition = transform.position;

            if (highlightEffect != null)
                highlightEffect.SetActive(false);
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
            if (itemData == null)
            {
                Debug.LogWarning($"[WorldItem] {gameObject.name} ไม่มี ItemData — กรุณา Assign ใน Inspector");
                return;
            }

            if (PlayerInventory.Instance == null)
            {
                Debug.LogWarning("[WorldItem] ไม่พบ PlayerInventory.Instance ใน Scene");
                return;
            }

            bool pickedUp = PlayerInventory.Instance.PickUpItem(itemData);

            if (pickedUp)
            {
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"[WorldItem] กระเป๋าเต็ม ไม่สามารถเก็บ '{itemData.itemName}' ได้");
            }
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
