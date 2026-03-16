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

        public NavMeshAgent Agent { get; private set; }
        protected HealthSystem Health;
        protected float LastAttackTime;

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
            }
            else
            {
                // Disable NavMeshAgent on clients to let NetworkTransform handle position
                Agent.enabled = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                Health.OnDeath -= OnDeath;
            }
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
        }
    }
}