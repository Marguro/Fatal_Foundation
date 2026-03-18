using Unity.Netcode;
using UnityEngine;

namespace Trading
{
    /// <summary>
    /// Dedicated currency for trading systems.
    /// </summary>
    public class SilverCurrency : NetworkBehaviour
    {
        public static SilverCurrency LocalInstance { get; private set; }

        [SerializeField] private int startingSilver;

        private readonly NetworkVariable<int> _silverCount = new NetworkVariable<int>();

        public event System.Action<int> OnSilverChanged;

        public int SilverCount => _silverCount.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                LocalInstance = this;
                _silverCount.Value = Mathf.Max(0, startingSilver);
            }

            _silverCount.OnValueChanged += HandleSilverChanged;
            OnSilverChanged?.Invoke(_silverCount.Value);
        }

        public override void OnNetworkDespawn()
        {
            _silverCount.OnValueChanged -= HandleSilverChanged;

            if (LocalInstance == this)
                LocalInstance = null;

            base.OnNetworkDespawn();
        }

        public void AddSilver(int amount)
        {
            if (!IsOwner) return;
            if (amount <= 0) return;

            _silverCount.Value = Mathf.Max(0, _silverCount.Value + amount);
        }

        public bool RemoveSilver(int amount)
        {
            if (!IsOwner) return false;
            if (amount < 0) return false;

            if (_silverCount.Value < amount)
            {
                Debug.LogWarning($"[Silver] Not enough Silver. Required: {amount}, Have: {_silverCount.Value}");
                return false;
            }

            _silverCount.Value -= amount;
            return true;
        }

        public bool HasEnoughSilver(int amount)
        {
            return _silverCount.Value >= amount;
        }

        private void HandleSilverChanged(int oldValue, int newValue)
        {
            OnSilverChanged?.Invoke(newValue);
        }
    }
}


