using System.Collections.Generic;
using Inventory;
using NaughtyAttributes;
using UnityEngine;

namespace Trading
{
    [RequireComponent(typeof(BoxCollider))]
    public class TradeSellZone : MonoBehaviour
    {
        [BoxGroup("Zone")]
        [SerializeField] private BoxCollider zoneCollider;

        private readonly HashSet<WorldItem> _trackedItems = new HashSet<WorldItem>();

        private void Reset()
        {
            zoneCollider = GetComponent<BoxCollider>();
            if (zoneCollider != null)
                zoneCollider.isTrigger = true;
        }

        private void Awake()
        {
            if (zoneCollider == null)
                zoneCollider = GetComponent<BoxCollider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            WorldItem item = other.GetComponentInParent<WorldItem>();
            if (item != null)
                _trackedItems.Add(item);
        }

        private void OnTriggerExit(Collider other)
        {
            WorldItem item = other.GetComponentInParent<WorldItem>();
            if (item != null)
                _trackedItems.Remove(item);
        }

        public IReadOnlyList<WorldItem> GetItemsInZone()
        {
            RefreshTrackedItemsFromOverlap();
            _trackedItems.RemoveWhere(item => item == null || !item.gameObject.activeInHierarchy);
            return new List<WorldItem>(_trackedItems);
        }

        public void ClearMissingReferences()
        {
            _trackedItems.RemoveWhere(item => item == null || !item.gameObject.activeInHierarchy);
        }

        private void RefreshTrackedItemsFromOverlap()
        {
            if (zoneCollider == null) return;

            Vector3 worldCenter = transform.TransformPoint(zoneCollider.center);
            Vector3 worldHalfExtents = Vector3.Scale(zoneCollider.size, transform.lossyScale) * 0.5f;

            Collider[] overlaps = Physics.OverlapBox(
                worldCenter,
                worldHalfExtents,
                transform.rotation,
                ~0,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < overlaps.Length; i++)
            {
                WorldItem item = overlaps[i].GetComponentInParent<WorldItem>();
                if (item != null)
                    _trackedItems.Add(item);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (zoneCollider == null)
                zoneCollider = GetComponent<BoxCollider>();

            if (zoneCollider == null) return;

            Gizmos.color = new Color(0f, 1f, 0.1f, 0.25f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(zoneCollider.center, zoneCollider.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(zoneCollider.center, zoneCollider.size);
            Gizmos.matrix = oldMatrix;
        }
    }
}

