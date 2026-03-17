using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NaughtyAttributes;
using System.Collections.Generic;

namespace Perks
{
    /// <summary>
    /// Manages the Perk UI Canvas and user interactions
    /// </summary>
    public class PerkUIManager : MonoBehaviour
    {
        public static PerkUIManager Instance { get; private set; }

        [BoxGroup("Canvas")]
        [SerializeField] private Canvas perkCanvas;
        [BoxGroup("Canvas")]
        [SerializeField] private Button closeButton;

        [BoxGroup("Soul Orb Display")]
        [SerializeField] private TextMeshProUGUI soulOrbDisplayText;

        [BoxGroup("Perk Button Prefab")]
        [SerializeField] private GameObject perkButtonPrefab;
        [BoxGroup("Perk Button Prefab")]
        [SerializeField] private Transform perkButtonContainer;

        [BoxGroup("Selection Settings")]
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color normalColor = Color.white;

        private List<PerkButtonUI> _perkButtons = new List<PerkButtonUI>();
        private PerkData _selectedPerk;
        private GameObject _playerCharacter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Initialize
            if (perkCanvas != null)
            {
                perkCanvas.gameObject.SetActive(false);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePerkMenu);
            }

            // Subscribe to Soul Orb changes
            if (SoulOrbCurrency.Instance != null)
            {
                SoulOrbCurrency.Instance.OnSoulOrbsChanged += UpdateSoulOrbDisplay;
                UpdateSoulOrbDisplay(SoulOrbCurrency.Instance.SoulOrbCount);
            }

            _playerCharacter = GameObject.FindGameObjectWithTag("Player");
        }

        private void OnDestroy()
        {
            if (SoulOrbCurrency.Instance != null)
            {
                SoulOrbCurrency.Instance.OnSoulOrbsChanged -= UpdateSoulOrbDisplay;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePerkMenu);
            }
        }

        /// <summary>
        /// Opens the Perk selection menu with random perks
        /// </summary>
        public void OpenPerkMenu()
        {
            if (perkCanvas == null || PerkSystem.Instance == null) return;

            // Get random perks
            List<PerkData> perks = PerkSystem.Instance.GetRandomPerkSelection(3);

            // Clear previous buttons
            ClearPerkButtons();

            // Create buttons for each perk
            foreach (var perk in perks)
            {
                CreatePerkButton(perk);
            }

            // Show canvas
            perkCanvas.gameObject.SetActive(true);
            Debug.Log("[PerkUIManager] Opened Perk menu with " + perks.Count + " perks");
        }

        /// <summary>
        /// Closes the Perk selection menu
        /// </summary>
        public void ClosePerkMenu()
        {
            if (perkCanvas != null)
            {
                perkCanvas.gameObject.SetActive(false);
                Debug.Log("[PerkUIManager] Closed Perk menu");
            }
        }

        /// <summary>
        /// Creates a UI button for a perk
        /// </summary>
        private void CreatePerkButton(PerkData perk)
        {
            if (perkButtonPrefab == null || perkButtonContainer == null) return;

            GameObject buttonObj = Instantiate(perkButtonPrefab, perkButtonContainer);
            PerkButtonUI buttonUI = buttonObj.GetComponent<PerkButtonUI>();

            if (buttonUI != null)
            {
                buttonUI.Initialize(perk, this);
                _perkButtons.Add(buttonUI);
            }
        }

        /// <summary>
        /// Clears all perk buttons
        /// </summary>
        private void ClearPerkButtons()
        {
            foreach (var button in _perkButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _perkButtons.Clear();
            _selectedPerk = null;
        }

        /// <summary>
        /// Handles perk selection
        /// </summary>
        public void SelectPerk(PerkData perk)
        {
            if (perk == null || _playerCharacter == null) return;

            // Deselect previous perk
            if (_selectedPerk != null)
            {
                DeselectedPerk(_selectedPerk);
            }

            _selectedPerk = perk;

            // Highlight the selected perk
            foreach (var button in _perkButtons)
            {
                if (button.PerkData == perk)
                {
                    button.SetHighlight(true);
                }
                else
                {
                    button.SetHighlight(false);
                }
            }

            Debug.Log($"[PerkUIManager] Selected perk: {perk.perkName}");
        }

        /// <summary>
        /// Confirms and applies the selected perk
        /// </summary>
        public void ConfirmPerkSelection()
        {
            if (_selectedPerk == null)
            {
                Debug.LogWarning("[PerkUIManager] No perk selected!");
                return;
            }

            if (_playerCharacter == null)
            {
                Debug.LogWarning("[PerkUIManager] Player character not found!");
                return;
            }

            // Apply the perk through PerkSystem
            if (PerkSystem.Instance.SelectPerk(_selectedPerk, _playerCharacter))
            {
                // Success! Close the menu
                ClosePerkMenu();
            }
            else
            {
                Debug.LogWarning("[PerkUIManager] Failed to apply perk!");
            }
        }

        /// <summary>
        /// Deselects a perk
        /// </summary>
        private void DeselectedPerk(PerkData perk)
        {
            foreach (var button in _perkButtons)
            {
                if (button.PerkData == perk)
                {
                    button.SetHighlight(false);
                }
            }
        }

        /// <summary>
        /// Updates the Soul Orb display text
        /// </summary>
        private void UpdateSoulOrbDisplay(int amount)
        {
            if (soulOrbDisplayText != null)
            {
                soulOrbDisplayText.text = $"x{amount}";
            }
        }

        public Color HighlightColor => highlightColor;
        public Color NormalColor => normalColor;
    }
}

