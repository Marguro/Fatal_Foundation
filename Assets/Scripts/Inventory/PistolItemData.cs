using UnityEngine;
using NaughtyAttributes;
using Unity.Netcode;

namespace Inventory
{
    [CreateAssetMenu(fileName = "New Pistol", menuName = "FatalFoundation/Items/Pistol")]
    public class PistolItemData : ItemData
    {
        [BoxGroup("Weapon Stats")]
        [SerializeField] private float damage = 20f;
        [BoxGroup("Weapon Stats")]
        [SerializeField] private float fireRange = 50f;
        [BoxGroup("Weapon Stats")]
        [SerializeField] private LayerMask hitMask = ~0; // ค่าเริ่มต้นเป็น Everything
        [BoxGroup("Weapon Stats")]
        [SerializeField] private int maxAmmo = 12;
        [BoxGroup("Weapon Stats")]
        [SerializeField] private int currentAmmo = 12;
        
        [BoxGroup("Effects")]
        [SerializeField] private AudioClip shootSound;
        [BoxGroup("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;

        public override bool Use(GameObject character)
        {
            base.Use(character);

            if (character == null) return false;

            // รีเซ็ตกระสุนถ้าหมด สำหรับทดสอบ (เพราะ ScriptableObject จะจำค่ากระสุนไว้ตลอดใน Editor)
            if (currentAmmo <= 0)
            {
                Debug.Log("Out of ammo! Reloading automatically for testing...");
                currentAmmo = maxAmmo;
            }

            var netObj = character.GetComponent<NetworkObject>();
            if (netObj == null || !netObj.IsOwner) return false;

            Camera playerCamera = character.GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (playerCamera != null)
            {
                currentAmmo--;
                Debug.Log($"Ammo left: {currentAmmo}/{maxAmmo}");
                Shoot(playerCamera.transform, netObj);
                return false; // Return false so the item is NOT consumed from the inventory
            }

            return false;
        }

        private void Shoot(Transform origin, NetworkObject ownerNetObj)
        {
            if (shootSound != null)
            {
                AudioSource.PlayClipAtPoint(shootSound, origin.position);
            }

            Vector3 endPoint = origin.position + origin.forward * fireRange;
            Ray ray = new Ray(origin.position, origin.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, fireRange, hitMask))
            {
                endPoint = hit.point;

                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }

                // If target has health, tell server to damage
                var hitNetObj = hit.collider.GetComponentInParent<NetworkObject>();
                if (hitNetObj != null)
                {
                    var health = hitNetObj.GetComponent<HealthSystem>();
                    if (health != null)
                    {
                        var playerInventory = ownerNetObj.GetComponent<PlayerInventory>();
                        if (playerInventory != null)
                        {
                            playerInventory.DealDamageServerRpc(hitNetObj.NetworkObjectId, damage);
                            Debug.Log($"Hit {hit.collider.name} for {damage} damage.");
                        }
                    }
                }
            }
            
            // สร้างเส้นกราฟิกเพื่อให้ผู้เล่นมองเห็นวิถีกระสุนได้ในหน้า Game
            DrawVisualTracer(origin.position - (origin.up * 0.1f), endPoint);
        }

        private void DrawVisualTracer(Vector3 start, Vector3 end)
        {
            GameObject tracer = new GameObject("BulletTracer");
            LineRenderer lr = tracer.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPositions(new Vector3[] { start, end });
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            
            // หา Material แบบเบสิกมาวาดเส้น
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = Color.yellow;
            lr.material = mat;
            
            // ลบเส้นทิ้งหลังจากผ่านไป 0.1 วินาที
            Destroy(tracer, 0.1f);
        }
    }
}
