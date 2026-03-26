using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Hazards
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class DeadlyPitZone : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private float damageAmount = 99999f;
        [SerializeField] private bool applyDamageOverTime;
        [SerializeField] private float damageTickInterval = 0.25f;

        [Header("Filter")]
        [SerializeField] private bool onlyAffectPlayerObjects = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogs;

        private readonly Dictionary<HealthSystem, float> nextTickAt = new Dictionary<HealthSystem, float>();

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void OnValidate()
        {
            if (damageTickInterval < 0.01f)
            {
                damageTickInterval = 0.01f;
            }

            EnsureTriggerCollider();
        }

        private void EnsureTriggerCollider()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                box.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!HasServerAuthority()) return;

            HealthSystem target = ResolveTarget(other);
            if (target == null) return;

            ApplyDamage(target);

            if (applyDamageOverTime)
            {
                nextTickAt[target] = Time.time + damageTickInterval;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!applyDamageOverTime || !HasServerAuthority()) return;

            HealthSystem target = ResolveTarget(other);
            if (target == null) return;

            float nextTime;
            if (!nextTickAt.TryGetValue(target, out nextTime))
            {
                nextTime = Time.time;
            }

            if (Time.time < nextTime) return;

            ApplyDamage(target);
            nextTickAt[target] = Time.time + damageTickInterval;
        }

        private void OnTriggerExit(Collider other)
        {
            HealthSystem target = ResolveTarget(other);
            if (target == null) return;

            nextTickAt.Remove(target);
        }

        private bool HasServerAuthority()
        {
            NetworkManager manager = NetworkManager.Singleton;
            return manager == null || manager.IsServer;
        }

        private HealthSystem ResolveTarget(Collider other)
        {
            HealthSystem target = other.GetComponentInParent<HealthSystem>();
            if (target == null)
            {
                return null;
            }

            if (!onlyAffectPlayerObjects)
            {
                return target;
            }

            NetworkObject netObject = target.GetComponent<NetworkObject>();
            if (netObject == null || !netObject.IsPlayerObject)
            {
                return null;
            }

            return target;
        }

        private void ApplyDamage(HealthSystem target)
        {
            target.TakeDamage(damageAmount);

            if (debugLogs)
            {
                Debug.Log($"DeadlyPitZone dealt {damageAmount} damage to {target.name}", this);
            }
        }
    }
}

