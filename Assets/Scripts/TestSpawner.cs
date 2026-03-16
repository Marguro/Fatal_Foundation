using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestSpawner : NetworkBehaviour
{
    public GameObject prefabToSpawn; // ลาก Prefab ที่มี NetworkObject มาใส่ที่นี่

    void Update()
    {
        // กดปุ่ม T เพื่อ Spawn (ต้องเป็น Server/Host เท่านั้นถึงจะทำงาน)
        if (IsServer && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            // 1. Instantiate GameObject แบบปกติของ Unity ขึ้้นมาก่อน
            GameObject go = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);

            // 2. ดึง Component NetworkObject ออกมา
            NetworkObject netObj = go.GetComponent<NetworkObject>();

            // 3. สั่ง Spawn เพื่อให้มันไปโผล่ที่เครื่อง Client อื่นๆ
            netObj.Spawn();

            Debug.Log("Spawned Object ID: " + netObj.NetworkObjectId);
        }
    }
}