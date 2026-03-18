using Inventory;
using NaughtyAttributes;
using UnityEngine;

namespace Trading
{
    public enum TradeButtonAction
    {
        ConfirmSell,
        BuySpecificItem
    }

    public class TradeButtonInteractable : MonoBehaviour, IInteractable
    {
        [BoxGroup("References")]
        [SerializeField] private TradeStationController tradeStation;

        [BoxGroup("Button")]
        [SerializeField] private TradeButtonAction action = TradeButtonAction.ConfirmSell;

        [ShowIf(nameof(IsBuyAction))]
        [BoxGroup("Button")]
        [SerializeField] private ItemData buyItem;

        [ShowIf(nameof(IsBuyAction))]
        [BoxGroup("Button")]
        [SerializeField] private int overrideBuyPrice = -1;

        [BoxGroup("Prompt")]
        [SerializeField] private string sellPrompt = "Press E to confirm sell";

        [BoxGroup("Prompt")]
        [SerializeField] private string buyPromptPrefix = "Press E to buy";

        [BoxGroup("Prompt")]
        [SerializeField] private string currencyLabel = "Silver";

        [BoxGroup("Visual")]
        [SerializeField] private GameObject highlightObject;

        public string PromptText
        {
            get
            {
                if (action == TradeButtonAction.ConfirmSell)
                    return sellPrompt;

                if (buyItem == null)
                    return "Press E to buy";

                int price = overrideBuyPrice >= 0
                    ? overrideBuyPrice
                    : (tradeStation != null ? tradeStation.ResolveBuyPrice(buyItem) : buyItem.scrapValue);

                return $"{buyPromptPrefix} {buyItem.itemName} ({Mathf.Max(0, price)} {currencyLabel})";
            }
        }

        public void Interact(GameObject interactor)
        {
            if (tradeStation == null)
            {
                Debug.LogWarning("[TradeButton] TradeStationController is missing.");
                return;
            }

            switch (action)
            {
                case TradeButtonAction.ConfirmSell:
                    tradeStation.ConfirmSell(interactor);
                    break;

                case TradeButtonAction.BuySpecificItem:
                    if (buyItem == null)
                    {
                        Debug.LogWarning("[TradeButton] No ItemData assigned for buy action.");
                        return;
                    }
                    tradeStation.TryBuyItem(interactor, buyItem, overrideBuyPrice);
                    break;
            }
        }

        public void SetHighlight(bool active)
        {
            if (highlightObject != null)
                highlightObject.SetActive(active);
        }

        private bool IsBuyAction()
        {
            return action == TradeButtonAction.BuySpecificItem;
        }
    }
}


