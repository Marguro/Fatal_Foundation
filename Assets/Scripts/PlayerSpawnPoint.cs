using UnityEngine;

/// <summary>
/// Scene marker for player spawn locations.
/// Attach this to empty GameObjects and place them where players should spawn.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "";
    [SerializeField] private bool spawnEnabled = true;

    public string SpawnId => spawnId;
    public bool SpawnEnabled => spawnEnabled;

    private void OnDrawGizmos()
    {
        Gizmos.color = spawnEnabled ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        var forward = transform.forward * 1.2f;
        Gizmos.DrawLine(transform.position, transform.position + forward);
    }
}

