using UnityEngine;
using NaughtyAttributes;
using Unity.Netcode;

namespace Perks
{
    /// <summary>
    /// Manages the player's Soul Orb currency
    /// </summary>
    public class SoulOrbCurrency : NetworkBehaviour
    {
        public static SoulOrbCurrency Instance { get; private set; }

        [SerializeField] private int startingSoulOrbs = 0;
        private NetworkVariable<int> _soulOrbCount = new NetworkVariable<int>(0);

        public int SoulOrbCount
        {
            get => _soulOrbCount.Value;
            set
            {
                if (IsOwner)
                {
                    _soulOrbCount.Value = Mathf.Max(0, value);
                    OnSoulOrbsChanged?.Invoke(_soulOrbCount.Value);
                }
            }
        }

        public event System.Action<int> OnSoulOrbsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                _soulOrbCount.Value = startingSoulOrbs;
            }

            _soulOrbCount.OnValueChanged += (old, newVal) =>
            {
                OnSoulOrbsChanged?.Invoke(newVal);
            };
        }

        /// <summary>
        /// Adds Soul Orbs to the player
        /// </summary>
        public void AddSoulOrbs(int amount)
        {
            if (!IsOwner) return;
            SoulOrbCount += amount;
            Debug.Log($"[SoulOrb] Added {amount} Soul Orbs. Total: {SoulOrbCount}");
        }

        /// <summary>
        /// Removes Soul Orbs from the player (used when buying perks)
        /// </summary>
        public bool RemoveSoulOrbs(int amount)
        {
            if (!IsOwner) return false;

            if (_soulOrbCount.Value < amount)
            {
                Debug.LogWarning($"[SoulOrb] Not enough Soul Orbs! Required: {amount}, Have: {_soulOrbCount.Value}");
                return false;
            }

            SoulOrbCount -= amount;
            Debug.Log($"[SoulOrb] Removed {amount} Soul Orbs. Total: {SoulOrbCount}");
            return true;
        }

        /// <summary>
        /// Checks if player has enough Soul Orbs
        /// </summary>
        public bool HasEnoughSoulOrbs(int amount)
        {
            return _soulOrbCount.Value >= amount;
        }
    }
}

