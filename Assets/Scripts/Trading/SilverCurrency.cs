using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

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

        private static readonly List<SilverCurrency> ActiveCurrencies = new List<SilverCurrency>();
        private static int _sharedSilver;
        private static bool _sharedSilverInitialized;

        private int _nextRequestId;
        private readonly Dictionary<int, System.Action<bool>> _pendingSpendRequests = new Dictionary<int, System.Action<bool>>();

        public event System.Action<int> OnSilverChanged;

        public int SilverCount => _silverCount.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!ActiveCurrencies.Contains(this))
                ActiveCurrencies.Add(this);

            if (IsServer)
            {
                if (!_sharedSilverInitialized)
                {
                    _sharedSilver = Mathf.Max(0, startingSilver);
                    _sharedSilverInitialized = true;
                }

                _silverCount.Value = _sharedSilver;
            }

            if (IsOwner)
            {
                LocalInstance = this;
            }

            _silverCount.OnValueChanged += HandleSilverChanged;
            OnSilverChanged?.Invoke(_silverCount.Value);
        }

        public override void OnNetworkDespawn()
        {
            _silverCount.OnValueChanged -= HandleSilverChanged;

            ActiveCurrencies.Remove(this);
            _pendingSpendRequests.Clear();

            if (LocalInstance == this)
                LocalInstance = null;

            if (IsServer && ActiveCurrencies.Count == 0)
            {
                _sharedSilverInitialized = false;
                _sharedSilver = 0;
            }

            base.OnNetworkDespawn();
        }

        public void AddSilver(int amount)
        {
            if (amount <= 0) return;

            if (IsServer)
            {
                AddSilverInternal(amount);
                return;
            }

            RequestAddSilverServerRpc(amount);
        }

        public void TrySpendSilver(int amount, System.Action<bool> onCompleted)
        {
            if (amount < 0)
            {
                onCompleted?.Invoke(false);
                return;
            }

            if (IsServer)
            {
                bool success = TrySpendSilverInternal(amount);
                onCompleted?.Invoke(success);
                return;
            }

            int requestId = ++_nextRequestId;
            _pendingSpendRequests[requestId] = onCompleted;
            RequestSpendSilverServerRpc(amount, requestId);
        }

        public bool HasEnoughSilver(int amount)
        {
            return _silverCount.Value >= amount;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestAddSilverServerRpc(int amount)
        {
            AddSilverInternal(amount);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestSpendSilverServerRpc(int amount, int requestId, ServerRpcParams rpcParams = default)
        {
            bool success = TrySpendSilverInternal(amount);

            SendSpendResultClientRpc(
                requestId,
                success,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { rpcParams.Receive.SenderClientId }
                    }
                }
            );
        }

        [ClientRpc]
        private void SendSpendResultClientRpc(int requestId, bool success, ClientRpcParams clientRpcParams = default)
        {
            _ = clientRpcParams;

            if (!_pendingSpendRequests.TryGetValue(requestId, out var callback))
                return;

            _pendingSpendRequests.Remove(requestId);
            callback?.Invoke(success);
        }

        private void AddSilverInternal(int amount)
        {
            if (!IsServer || amount <= 0) return;

            _sharedSilver = Mathf.Max(0, _sharedSilver + amount);
            SyncSharedSilverToAllInstances();
        }

        private bool TrySpendSilverInternal(int amount)
        {
            if (!IsServer || amount < 0)
                return false;

            if (_sharedSilver < amount)
            {
                Debug.LogWarning($"[Silver] Not enough Silver. Required: {amount}, Have: {_sharedSilver}");
                return false;
            }

            _sharedSilver -= amount;
            SyncSharedSilverToAllInstances();
            return true;
        }

        private void SyncSharedSilverToAllInstances()
        {
            if (!IsServer) return;

            for (int i = 0; i < ActiveCurrencies.Count; i++)
            {
                SilverCurrency currency = ActiveCurrencies[i];
                if (currency == null || !currency.IsSpawned)
                    continue;

                currency._silverCount.Value = _sharedSilver;
            }
        }

        private void HandleSilverChanged(int oldValue, int newValue)
        {
            OnSilverChanged?.Invoke(newValue);
        }
    }
}


