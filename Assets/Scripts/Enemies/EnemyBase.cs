using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Enemies
{
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyBase : NetworkBehaviour
    {
        [Header("Base Settings")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float defaultDamage = 10f;
        [SerializeField] private float attackCooldown = 1.0f;

        [Header("Network Sync")]
        [SerializeField] private float syncInterval = 0.05f;
        [SerializeField] private float positionSyncThreshold = 0.01f;
        [SerializeField] private float rotationSyncThreshold = 0.5f;
        [SerializeField] private float clientSmoothing = 15f;

        public NavMeshAgent Agent { get; private set; }
        protected HealthSystem Health;
        protected float LastAttackTime;

        private readonly NetworkVariable<Vector3> syncedPosition = new(
            writePerm: NetworkVariableWritePermission.Server
        );
        private readonly NetworkVariable<Quaternion> syncedRotation = new(
            writePerm: NetworkVariableWritePermission.Server
        );

        private float nextSyncTime;

        public float MoveSpeed
        {
            get => moveSpeed;
            set
            {
                moveSpeed = value;
                if (Agent != null && Agent.isOnNavMesh) Agent.speed = moveSpeed;
            }
        }

        public override void OnNetworkSpawn()
        {
            Agent = GetComponent<NavMeshAgent>();
            Health = GetComponent<HealthSystem>();

            if (IsServer)
            {
                Agent.speed = moveSpeed;
                Health.OnDeath += OnDeath;
                PushTransformState(force: true);
            }
            else
            {
                // Disable NavMeshAgent on clients; server sends replicated transform state.
                Agent.enabled = false;
                transform.SetPositionAndRotation(syncedPosition.Value, syncedRotation.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                Health.OnDeath -= OnDeath;
            }
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsServer)
            {
                if (Time.time >= nextSyncTime)
                {
                    nextSyncTime = Time.time + Mathf.Max(0.01f, syncInterval);
                    PushTransformState();
                }

                return;
            }

            ApplyClientTransform();
        }

        private void PushTransformState(bool force = false)
        {
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;

            bool positionChanged = (position - syncedPosition.Value).sqrMagnitude >= positionSyncThreshold * positionSyncThreshold;
            bool rotationChanged = Quaternion.Angle(rotation, syncedRotation.Value) >= rotationSyncThreshold;

            if (!force && !positionChanged && !rotationChanged)
            {
                return;
            }

            syncedPosition.Value = position;
            syncedRotation.Value = rotation;
        }

        private void ApplyClientTransform()
        {
            float lerpFactor = Mathf.Clamp01(Time.deltaTime * Mathf.Max(0f, clientSmoothing));
            transform.position = Vector3.Lerp(transform.position, syncedPosition.Value, lerpFactor);
            transform.rotation = Quaternion.Slerp(transform.rotation, syncedRotation.Value, lerpFactor);
        }

        public virtual void MoveTo(Vector3 targetPosition)
        {
            if (!IsServer || !Agent.enabled || !Agent.isOnNavMesh) return;
            Agent.SetDestination(targetPosition);
            Agent.isStopped = false;
        }

        public virtual void StopMoving()
        {
            if (!IsServer || !Agent.enabled || !Agent.isOnNavMesh) return;
            Agent.isStopped = true;
        }

        public virtual bool AttackTarget(HealthSystem targetHealth, float damageOverride = -1)
        {
            if (!IsServer) return false;

            if (Time.time >= LastAttackTime + attackCooldown)
            {
                float damage = damageOverride > 0 ? damageOverride : defaultDamage;
                targetHealth.TakeDamage(damage);
                LastAttackTime = Time.time;
                return true;
            }
            return false;
        }

        protected virtual void OnDeath()
        {
            // Optional: shared death logic (e.g., sound, particle)
            if (IsServer)
            {
                // Unsubscribe to prevent errors
                if (Health != null) Health.OnDeath -= OnDeath;

                // Disable Agent to prevent NavMeshAgentInspector errors when destroyed
                if (Agent != null) Agent.enabled = false;

                // Note: We removed Despawn/Destroy here because HealthSystem handles destruction
                // via the 'destroyOnDeath' flag to avoid double-destruction errors.
            }
        }
    }
}