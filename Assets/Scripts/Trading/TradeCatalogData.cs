using System;
using System.Collections.Generic;
using Inventory;
using NaughtyAttributes;
using UnityEngine;

namespace Trading
{
	[CreateAssetMenu(fileName = "TradeCatalog", menuName = "FatalFoundation/Trading/Trade Catalog")]
	public class TradeCatalogData : ScriptableObject
	{
		[Serializable]
		public class TradeEntry
		{
			[Required("Assign ItemData")]
			public ItemData item;

			[Min(0)] public int buyPrice = 10;
			[Min(0)] public int sellPrice;
			public bool unlimitedStock = true;
			[Min(0)] public int startingStock;
		}

		[SerializeField] private List<TradeEntry> entries = new List<TradeEntry>();

		public IReadOnlyList<TradeEntry> Entries => entries;

		public TradeEntry GetEntry(ItemData item)
		{
			if (item == null) return null;
			return entries.Find(e => e != null && e.item == item);
		}
	}
}


