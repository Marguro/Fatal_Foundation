using TMPro;
using UnityEngine;
using Inventory;

namespace Trading
{
    public class SilverCurrencyTextUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI silverText;
        [SerializeField] private string textFormat = "Silver {0}/400";
        [SerializeField] private string soulOrbFormat = "SoulOrb {0}/1";
        [SerializeField] private SilverCurrency targetCurrency;

        private SilverCurrency _boundCurrency;

        private void Reset()
        {
            if (silverText == null)
                silverText = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            TryBindCurrency();
            RefreshText();
        }

        private void Update()
        {
            if (_boundCurrency == null)
            {
                TryBindCurrency();
            }

            if (_boundCurrency != null && _boundCurrency.SilverCount >= 400)
            {
                RefreshText(_boundCurrency.SilverCount);
            }
        }

        private void OnDisable()
        {
            UnbindCurrency();
        }

        public void SetTargetCurrency(SilverCurrency currency)
        {
            if (targetCurrency == currency) return;

            targetCurrency = currency;
            TryBindCurrency();
            RefreshText();
        }

        private void TryBindCurrency()
        {
            SilverCurrency candidate = ResolveCurrencyCandidate();
            if (candidate == _boundCurrency) return;

            UnbindCurrency();
            _boundCurrency = candidate;

            if (_boundCurrency != null)
            {
                _boundCurrency.OnSilverChanged += HandleSilverChanged;
                RefreshText(_boundCurrency.SilverCount);
            }
            else
            {
                RefreshText(0);
            }
        }

        private SilverCurrency ResolveCurrencyCandidate()
        {
            if (targetCurrency != null)
                return targetCurrency;

            if (SilverCurrency.LocalInstance != null)
                return SilverCurrency.LocalInstance;

            SilverCurrency[] allCurrencies = Object.FindObjectsByType<SilverCurrency>(FindObjectsSortMode.None);
            for (int i = 0; i < allCurrencies.Length; i++)
            {
                if (allCurrencies[i] != null && allCurrencies[i].IsOwner)
                    return allCurrencies[i];
            }

            return null;
        }

        private void UnbindCurrency()
        {
            if (_boundCurrency == null) return;
            _boundCurrency.OnSilverChanged -= HandleSilverChanged;
            _boundCurrency = null;
        }

        private void HandleSilverChanged(int currentSilver)
        {
            RefreshText(currentSilver);
        }

        private void RefreshText()
        {
            int amount = _boundCurrency != null ? _boundCurrency.SilverCount : 0;
            RefreshText(amount);
        }

        private void RefreshText(int amount)
        {
            if (silverText == null) return;
            
            if (amount >= 400)
            {
                int totalSoulOrbs = 0;
                PlayerInventory[] allInventories = Object.FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None);
                foreach (var inv in allInventories)
                {
                    if (inv != null)
                    {
                        totalSoulOrbs += inv.NetSoulOrbCount.Value;
                    }
                }
                silverText.text = string.Format(soulOrbFormat, totalSoulOrbs > 0 ? 1 : 0);
            }
            else
            {
                silverText.text = string.Format(textFormat, amount);
            }
        }
    }
}
