using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Perks
{
    /// <summary>
    /// Individual perk button UI component
    /// </summary>
    public class PerkButtonUI : MonoBehaviour
    {
        [SerializeField] private Image perkIcon;
        [SerializeField] private TextMeshProUGUI perkNameText;
        [SerializeField] private TextMeshProUGUI perkDescriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image buttonImage;

        private PerkData _perkData;
        private PerkUIManager _uiManager;
        private bool _isHighlighted = false;

        public PerkData PerkData => _perkData;

        public void Initialize(PerkData perk, PerkUIManager uiManager)
        {
            _perkData = perk;
            _uiManager = uiManager;

            // Set up UI elements
            if (perkIcon != null && perk.perkIcon != null)
            {
                perkIcon.sprite = perk.perkIcon;
            }

            if (perkNameText != null)
            {
                perkNameText.text = perk.perkName;
            }

            if (perkDescriptionText != null)
            {
                perkDescriptionText.text = perk.description;
            }

            if (costText != null)
            {
                costText.text = $"Cost: {perk.soulOrbCost} Soul Orbs";
            }

            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnButtonClicked);
            }

            // Set initial color
            if (buttonImage != null)
            {
                buttonImage.color = _uiManager.NormalColor;
            }
        }

        private void OnButtonClicked()
        {
            if (_uiManager != null)
            {
                _uiManager.SelectPerk(_perkData);
            }
        }

        /// <summary>
        /// Sets the highlight state of this button
        /// </summary>
        public void SetHighlight(bool highlighted)
        {
            _isHighlighted = highlighted;

            if (buttonImage != null)
            {
                buttonImage.color = highlighted ? _uiManager.HighlightColor : _uiManager.NormalColor;
            }

            // Optional: Add scale animation
            if (highlighted)
            {
                transform.localScale = Vector3.one * 1.05f;
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnButtonClicked);
            }
        }
    }
}

