using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SonicChestFilters
{
	internal static class ChestSort
	{
		private static bool _reverse;

		internal static void ResetSession()
		{
			_reverse = false;
		}

		internal static void SortCurrent(InventoryGui gui)
		{
			if ((Object)(object)gui == (Object)null)
			{
				return;
			}

			if (IsDragging(gui))
			{
				Say("Cannot sort while dragging an item.");
				return;
			}

			Container container = GameAccess.Get<Container>(gui, "m_currentContainer");
			if ((Object)(object)container == (Object)null)
			{
				return;
			}

			if (!container.IsOwner())
			{
				Say("Cannot sort: not the chest owner.");
				return;
			}

			Inventory inventory = container.GetInventory();
			if (inventory == null)
			{
				return;
			}

			MergeStacks(inventory);
			List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
			items.Sort(CompareItems);
			if (_reverse)
			{
				items.Reverse();
			}

			int width = inventory.GetWidth();
			for (int i = 0; i < items.Count; i++)
			{
				items[i].m_gridPos = new Vector2i(i % width, i / width);
			}

			GameAccess.Call(inventory, "Changed", true, false);
			_reverse = !_reverse;
			if (SonicChestFiltersPlugin.DebugEnabled)
			{
				SonicChestFiltersPlugin.Debug($"Sorted {items.Count} item stacks (reverse={_reverse}).");
			}
		}

		private static int CompareItems(ItemDrop.ItemData a, ItemDrop.ItemData b)
		{
			int type = ((int)a.m_shared.m_itemType).CompareTo((int)b.m_shared.m_itemType);
			if (type != 0)
			{
				return type;
			}

			string nameA = ItemMatch.GetLocalizedName(a.m_shared.m_name) ?? string.Empty;
			string nameB = ItemMatch.GetLocalizedName(b.m_shared.m_name) ?? string.Empty;
			int name = string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
			if (name != 0)
			{
				return name;
			}

			int quality = b.m_quality.CompareTo(a.m_quality);
			if (quality != 0)
			{
				return quality;
			}

			return string.Compare(a.m_shared.m_name, b.m_shared.m_name, StringComparison.OrdinalIgnoreCase);
		}

		private static void MergeStacks(Inventory inventory)
		{
			bool merged;
			do
			{
				merged = false;
				List<ItemDrop.ItemData> items = inventory.GetAllItems();
				for (int i = 0; i < items.Count && !merged; i++)
				{
					ItemDrop.ItemData target = items[i];
					int max = target.m_shared.m_maxStackSize;
					if (max <= 1 || target.m_stack >= max)
					{
						continue;
					}
					for (int j = i + 1; j < items.Count; j++)
					{
						ItemDrop.ItemData source = items[j];
						if (!CanStackTogether(target, source))
						{
							continue;
						}
						int space = max - target.m_stack;
						if (space <= 0)
						{
							break;
						}
						int move = Mathf.Min(space, source.m_stack);
						target.m_stack += move;
						source.m_stack -= move;
						if (source.m_stack <= 0)
						{
							inventory.RemoveItem(source);
						}
						merged = true;
						break;
					}
				}
			}
			while (merged);
		}

		private static bool CanStackTogether(ItemDrop.ItemData source, ItemDrop.ItemData target)
		{
			if (source == null || target == null || source.m_shared == null || target.m_shared == null)
			{
				return false;
			}
			if (source.m_shared.m_name != target.m_shared.m_name
				|| source.m_quality != target.m_quality
				|| source.m_variant != target.m_variant
				|| source.m_worldLevel != target.m_worldLevel)
			{
				return false;
			}
			if (source.m_customData.Count != target.m_customData.Count)
			{
				return false;
			}
			foreach (KeyValuePair<string, string> pair in source.m_customData)
			{
				if (!target.m_customData.TryGetValue(pair.Key, out string value) || value != pair.Value)
				{
					return false;
				}
			}
			return true;
		}

		private static bool IsDragging(InventoryGui gui)
		{
			object dragGo = AccessToolsField(gui, "m_dragGo");
			if (dragGo is Object go && (Object)(object)go != (Object)null)
			{
				return true;
			}
			object dragItem = AccessToolsField(gui, "m_dragItem");
			return dragItem != null;
		}

		private static object AccessToolsField(object instance, string name)
		{
			return AccessTools.Field(instance.GetType(), name)?.GetValue(instance);
		}

		private static void Say(string message)
		{
			Player player = Player.m_localPlayer;
			if ((Object)(object)player != (Object)null)
			{
				((Character)player).Message((MessageHud.MessageType)2, message, 0, (Sprite)null, false);
				return;
			}
			if ((Object)(object)Chat.instance != (Object)null)
			{
				Chat.instance.AddString(message);
			}
		}
	}
}
