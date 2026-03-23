using System.Collections.Generic;
using Inventory;
using NaughtyAttributes;
using UnityEngine;

namespace Trading
{
    public class TradeStationController : MonoBehaviour
    {
        [BoxGroup("References")]
        [SerializeField] private TradeSellZone sellZone;

        [BoxGroup("References")]
        [SerializeField] private TradeCatalogData catalog;

        [BoxGroup("Buy Settings")]
        [SerializeField] private bool allowFallbackToScrapValue = true;

        private readonly Dictionary<ItemData, int> _currentStock = new Dictionary<ItemData, int>();

        private void Awake()
        {
            BuildStockCache();
        }

        public int GetSellPreviewTotal()
        {
            if (sellZone == null) return 0;

            int total = 0;
            var items = sellZone.GetItemsInZone();
            for (int i = 0; i < items.Count; i++)
            {
                var worldItem = items[i];
                if (worldItem == null || worldItem.itemData == null) continue;
                total += ResolveSellPrice(worldItem.itemData);
            }
            return total;
        }

        public bool ConfirmSell(GameObject interactor)
        {
            if (sellZone == null || interactor == null) return false;

            SilverCurrency currency = ResolveCurrency(interactor);
            if (currency == null)
            {
                Debug.LogWarning("[TradeStation] No SilverCurrency found on player.");
                return false;
            }

            var items = sellZone.GetItemsInZone();
            if (items.Count == 0)
            {
                Debug.Log("[TradeStation] Nothing in sell zone.");
                return false;
            }

            int totalPrice = 0;
            for (int i = 0; i < items.Count; i++)
            {
                WorldItem worldItem = items[i];
                if (worldItem == null || worldItem.itemData == null) continue;

                int sellPrice = ResolveSellPrice(worldItem.itemData);
                totalPrice += Mathf.Max(0, sellPrice);
                worldItem.SellAndDespawn();
            }

            if (totalPrice > 0)
            {
                currency.AddSilver(totalPrice);
                Debug.Log($"[TradeStation] Sold items for {totalPrice} Silver.");
            }

            sellZone.ClearMissingReferences();
            return true;
        }

        public bool TryBuyItem(GameObject interactor, ItemData item, int customPrice = -1)
        {
            if (interactor == null || item == null) return false;

            SilverCurrency currency = ResolveCurrency(interactor);
            if (currency == null)
            {
                Debug.LogWarning("[TradeStation] No SilverCurrency found on player.");
                return false;
            }

            int buyPrice = customPrice >= 0 ? customPrice : ResolveBuyPrice(item);
            if (buyPrice < 0)
            {
                Debug.LogWarning($"[TradeStation] Item '{item.itemName}' is not available to buy.");
                return false;
            }

            if (!HasStock(item))
            {
                Debug.LogWarning($"[TradeStation] Item '{item.itemName}' is out of stock.");
                return false;
            }

            PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning("[TradeStation] No PlayerInventory found on interactor.");
                return false;
            }

            // Shared silver spend is server-authoritative; item is granted only to the buyer client.
            currency.TrySpendSilver(buyPrice, success =>
            {
                if (!success)
                    return;

                bool pickedUp = inventory.PickUpItem(item);
                if (!pickedUp)
                {
                    // Refund if inventory is full after server accepted payment.
                    currency.AddSilver(buyPrice);
                    Debug.LogWarning("[TradeStation] Inventory is full. Purchase cancelled and refunded.");
                    return;
                }

                ConsumeStock(item);
                Debug.Log($"[TradeStation] Bought '{item.itemName}' for {buyPrice} Silver.");
            });

            return true;
        }

        public int ResolveBuyPrice(ItemData item)
        {
            if (item == null) return -1;

            TradeCatalogData.TradeEntry entry = catalog != null ? catalog.GetEntry(item) : null;
            if (entry != null)
                return Mathf.Max(0, entry.buyPrice);

            if (allowFallbackToScrapValue)
                return Mathf.Max(0, item.scrapValue);

            return -1;
        }

        public int ResolveSellPrice(ItemData item)
        {
            if (item == null) return 0;

            TradeCatalogData.TradeEntry entry = catalog != null ? catalog.GetEntry(item) : null;
            if (entry != null)
            {
                int fromCatalog = entry.sellPrice;
                if (fromCatalog > 0)
                    return fromCatalog;
            }

            return Mathf.Max(0, item.scrapValue);
        }

        private SilverCurrency ResolveCurrency(GameObject interactor)
        {
            SilverCurrency currency = interactor.GetComponent<SilverCurrency>();
            if (currency != null) return currency;
            return SilverCurrency.LocalInstance;
        }

        private void BuildStockCache()
        {
            _currentStock.Clear();

            if (catalog == null || catalog.Entries == null) return;

            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                var entry = catalog.Entries[i];
                if (entry == null || entry.item == null || entry.unlimitedStock) continue;
                _currentStock[entry.item] = Mathf.Max(0, entry.startingStock);
            }
        }

        private bool HasStock(ItemData item)
        {
            if (catalog == null) return true;

            var entry = catalog.GetEntry(item);
            if (entry == null || entry.unlimitedStock) return true;

            return _currentStock.TryGetValue(item, out int stock) && stock > 0;
        }

        private void ConsumeStock(ItemData item)
        {
            if (catalog == null) return;

            var entry = catalog.GetEntry(item);
            if (entry == null || entry.unlimitedStock) return;

            if (_currentStock.TryGetValue(item, out int stock))
                _currentStock[item] = Mathf.Max(0, stock - 1);
        }
    }
}


