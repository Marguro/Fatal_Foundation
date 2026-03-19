using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private bool autoCollectSpawnPoints = true;
    [SerializeField] private bool randomSpawnPoint;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    private int _roundRobinIndex;

    private void Awake()
    {
        RefreshSpawnPoints();
    }

    [ContextMenu("Refresh Spawn Points")]
    public void RefreshSpawnPoints()
    {
        if (!autoCollectSpawnPoints)
        {
            return;
        }

        spawnPoints.Clear();
        var points = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null && points[i] != transform)
            {
                spawnPoints.Add(points[i]);
            }
        }
    }

    void Update()
    {
        // กดปุ่ม T เพื่อ Spawn (ต้องเป็น Server/Host เท่านั้นถึงจะทำงาน)
        if (IsServer && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("[TestSpawner] Missing prefabToSpawn reference.");
            return;
        }

        if (!TryGetSpawnPose(out var spawnPosition, out var spawnRotation))
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        // 1. Instantiate GameObject แบบปกติของ Unity ขึ้้นมาก่อน
        GameObject go = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

        // 2. ดึง Component NetworkObject ออกมา
        NetworkObject netObj = go.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError("[TestSpawner] Spawned prefab has no NetworkObject component.");
            Destroy(go);
            return;
        }

        // 3. สั่ง Spawn เพื่อให้มันไปโผล่ที่เครื่อง Client อื่นๆ
        netObj.Spawn();

        Debug.Log($"[TestSpawner] Spawned Object ID: {netObj.NetworkObjectId} at {spawnPosition}");
    }

    private bool TryGetSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        if (autoCollectSpawnPoints && spawnPoints.Count == 0)
        {
            RefreshSpawnPoints();
        }

        var validPoints = new List<Transform>();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
                validPoints.Add(spawnPoints[i]);
            }
        }

        if (validPoints.Count == 0)
        {
            position = default;
            rotation = default;
            return false;
        }

        var index = randomSpawnPoint
            ? Random.Range(0, validPoints.Count)
            : _roundRobinIndex++ % validPoints.Count;

        var selected = validPoints[index];
        position = selected.position;
        rotation = selected.rotation;
        return true;
    }
}