using UnityEngine;
using NaughtyAttributes;
using Unity.Netcode;
using System.Collections.Generic;

namespace Perks
{
    /// <summary>
    /// Main Perk system that manages available perks and player selections
    /// </summary>
    public class PerkSystem : NetworkBehaviour
    {
        public static PerkSystem Instance { get; private set; }

        [SerializeField] private List<PerkData> allAvailablePerks = new List<PerkData>();
        private List<PerkData> _activePerksList = new List<PerkData>();
        private HashSet<string> _selectedPerkNames = new HashSet<string>();

        public event System.Action<PerkData> OnPerkSelected;
        public event System.Action<PerkData> OnPerkDeselected;

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
            // Initialize active perks list from available perks
            _activePerksList.AddRange(allAvailablePerks);
        }

        /// <summary>
        /// Returns a random selection of perks for the player to choose from
        /// </summary>
        public List<PerkData> GetRandomPerkSelection(int count = 3)
        {
            List<PerkData> selectedPerks = new List<PerkData>();

            if (allAvailablePerks.Count == 0)
            {
                Debug.LogWarning("[PerkSystem] No perks available!");
                return selectedPerks;
            }

            int selectCount = Mathf.Min(count, allAvailablePerks.Count);
            List<PerkData> tempList = new List<PerkData>(allAvailablePerks);

            for (int i = 0; i < selectCount; i++)
            {
                int randomIndex = Random.Range(0, tempList.Count);
                selectedPerks.Add(tempList[randomIndex]);
                tempList.RemoveAt(randomIndex);
            }

            return selectedPerks;
        }

        /// <summary>
        /// Selects and applies a perk to the player
        /// </summary>
        public bool SelectPerk(PerkData perk, GameObject character)
        {
            if (perk == null || character == null)
            {
                Debug.LogWarning("[PerkSystem] Cannot select perk: invalid perk or character");
                return false;
            }

            // Check if player has enough Soul Orbs
            var soulOrbSystem = SoulOrbCurrency.Instance;
            if (soulOrbSystem == null || !soulOrbSystem.HasEnoughSoulOrbs(perk.soulOrbCost))
            {
                Debug.LogWarning($"[PerkSystem] Not enough Soul Orbs to select {perk.perkName}");
                return false;
            }

            // Deduct Soul Orbs
            if (!soulOrbSystem.RemoveSoulOrbs(perk.soulOrbCost))
            {
                return false;
            }

            // Apply the perk
            perk.ApplyPerk(character);
            _selectedPerkNames.Add(perk.perkName);
            OnPerkSelected?.Invoke(perk);

            Debug.Log($"[PerkSystem] Selected perk: {perk.perkName}");
            return true;
        }

        /// <summary>
        /// Checks if a perk has already been selected
        /// </summary>
        public bool IsPerkSelected(PerkData perk)
        {
            return perk != null && _selectedPerkNames.Contains(perk.perkName);
        }

        /// <summary>
        /// Gets all selected perks
        /// </summary>
        public HashSet<string> GetSelectedPerks()
        {
            return new HashSet<string>(_selectedPerkNames);
        }

        /// <summary>
        /// Clears all selected perks (useful for reset scenarios)
        /// </summary>
        public void ClearSelectedPerks()
        {
            _selectedPerkNames.Clear();
        }
    }
}

