using UnityEngine;

namespace FatalFoundation
{
    /// <summary>
    /// ScriptableObject สำหรับเก็บข้อมูลของ Item ทุกชนิด
    /// สร้างได้จาก Right-click > Create > FatalFoundation/ItemData
    /// </summary>
    [CreateAssetMenu(fileName = "New ItemData", menuName = "FatalFoundation/ItemData")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("ชื่อของไอเทม")]
        public string itemName = "Unknown Item";

        [Tooltip("ไอคอนที่แสดงใน UI Inventory")]
        public Sprite itemIcon;

        [Header("Prefabs")]
        [Tooltip("Prefab ที่ Spawn ในโลกเมื่อทิ้งไอเทม")]
        public GameObject worldPrefab;

        [Tooltip("Prefab ที่แสดงในมือ Player")]
        public GameObject handPrefab;

        [Header("Item Properties")]
        [Tooltip("น้ำหนักของไอเทม — ส่งผลต่อความเร็วของ Player")]
        public float weight = 1f;

        [Tooltip("มูลค่าเศษซาก (scrap value)")]
        public int scrapValue = 0;

        [Tooltip("ถ้าเป็น True จะใช้สองมือ — ไม่สามารถสลับช่องได้จนกว่าจะทิ้ง")]
        public bool isTwoHanded = false;
    }
}

