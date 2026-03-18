using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Assigns player spawn positions for NGO using scene-based PlayerSpawnPoint markers.
/// Add this component to the same GameObject as NetworkManager.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager))]
public class NetworkPlayerSpawnService : MonoBehaviour
{
    public enum SpawnSelectionMode
    {
        RoundRobin,
        ClientIdHash,
        Random
    }

    [Header("Spawn Points")]
    [SerializeField] private bool autoCollectSpawnPoints = true;
    [SerializeField] private List<PlayerSpawnPoint> spawnPoints = new List<PlayerSpawnPoint>();
    [SerializeField] private SpawnSelectionMode selectionMode = SpawnSelectionMode.RoundRobin;

    [Header("NGO Integration")]
    [Tooltip("When enabled, this script assigns response.Position/response.Rotation during connection approval.")]
    [SerializeField] private bool useConnectionApprovalForSpawn = true;

    [Tooltip("If true, this script can still spawn player objects on server when a client has no PlayerObject.")]
    [SerializeField] private bool manualSpawnFallback;

    [Tooltip("Optional override. If null, NetworkConfig.PlayerPrefab is used.")]
    [SerializeField] private GameObject playerPrefabOverride;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private NetworkManager mNetworkManager;
    private Action<NetworkManager.ConnectionApprovalRequest, NetworkManager.ConnectionApprovalResponse> mPreviousApprovalCallback;
    private int mRoundRobinIndex;

    private void Awake()
    {
        mNetworkManager = GetComponent<NetworkManager>();
        RefreshSpawnPoints();
    }

    private void OnEnable()
    {
        if (mNetworkManager == null)
        {
            return;
        }

        if (useConnectionApprovalForSpawn)
        {
            if (!mNetworkManager.NetworkConfig.ConnectionApproval)
            {
                Debug.LogWarning("[NetworkPlayerSpawnService] NetworkConfig.ConnectionApproval is disabled. Enable it to apply spawn points during approval.");
            }

            mPreviousApprovalCallback = mNetworkManager.ConnectionApprovalCallback;
            mNetworkManager.ConnectionApprovalCallback = HandleConnectionApproval;
        }

        mNetworkManager.OnClientConnectedCallback += HandleClientConnected;
    }

    private void OnDisable()
    {
        if (mNetworkManager == null)
        {
            return;
        }

        mNetworkManager.OnClientConnectedCallback -= HandleClientConnected;

        if (useConnectionApprovalForSpawn && mNetworkManager.ConnectionApprovalCallback == HandleConnectionApproval)
        {
            mNetworkManager.ConnectionApprovalCallback = mPreviousApprovalCallback;
        }
    }

    [ContextMenu("Refresh Spawn Points")]
    public void RefreshSpawnPoints()
    {
        if (!autoCollectSpawnPoints)
        {
            return;
        }

        spawnPoints.Clear();
        var found = FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
            {
                spawnPoints.Add(found[i]);
            }
        }
    }

    private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        mPreviousApprovalCallback?.Invoke(request, response);

        if (!response.Approved || !response.CreatePlayerObject)
        {
            return;
        }

        if (!TryGetSpawn(request.ClientNetworkId, out var spawnPoint))
        {
            return;
        }

        response.Position = spawnPoint.transform.position;
        response.Rotation = spawnPoint.transform.rotation;

        if (verboseLog)
        {
            Debug.Log($"[NetworkPlayerSpawnService] Approval spawn set for client {request.ClientNetworkId} at {spawnPoint.transform.position}");
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!manualSpawnFallback || !mNetworkManager.IsServer)
        {
            return;
        }

        if (!mNetworkManager.ConnectedClients.TryGetValue(clientId, out var client))
        {
            return;
        }

        if (client.PlayerObject != null)
        {
            return;
        }

        var prefab = playerPrefabOverride != null ? playerPrefabOverride : mNetworkManager.NetworkConfig.PlayerPrefab;
        if (prefab == null)
        {
            Debug.LogError("[NetworkPlayerSpawnService] No player prefab configured for manual spawn fallback.");
            return;
        }

        if (!TryGetSpawn(clientId, out var spawnPoint))
        {
            return;
        }

        var instance = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
        var networkObject = instance.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("[NetworkPlayerSpawnService] Player prefab must have a NetworkObject component.");
            Destroy(instance);
            return;
        }

        networkObject.SpawnAsPlayerObject(clientId);

        if (verboseLog)
        {
            Debug.Log($"[NetworkPlayerSpawnService] Manual fallback spawned player for client {clientId} at {spawnPoint.transform.position}");
        }
    }

    private bool TryGetSpawn(ulong clientId, out PlayerSpawnPoint spawnPoint)
    {
        if (autoCollectSpawnPoints && spawnPoints.Count == 0)
        {
            RefreshSpawnPoints();
        }

        var validSpawns = new List<PlayerSpawnPoint>();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            var point = spawnPoints[i];
            if (point != null && point.SpawnEnabled)
            {
                validSpawns.Add(point);
            }
        }

        if (validSpawns.Count == 0)
        {
            spawnPoint = null;
            Debug.LogWarning("[NetworkPlayerSpawnService] No enabled PlayerSpawnPoint found. NGO will use prefab default position.");
            return false;
        }

        var index = 0;
        switch (selectionMode)
        {
            case SpawnSelectionMode.RoundRobin:
                index = mRoundRobinIndex % validSpawns.Count;
                mRoundRobinIndex++;
                break;
            case SpawnSelectionMode.ClientIdHash:
                index = (int)(clientId % (ulong)validSpawns.Count);
                break;
            case SpawnSelectionMode.Random:
                index = UnityEngine.Random.Range(0, validSpawns.Count);
                break;
        }

        spawnPoint = validSpawns[index];
        return true;
    }
}


