using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SonicChestFilters
{
	internal static class ChestFilter
	{
		internal static string Query { get; private set; } = string.Empty;

		internal static bool HasQuery => !string.IsNullOrWhiteSpace(Query);

		internal static void SetQuery(string query)
		{
			Query = query ?? string.Empty;
		}

		internal static void Clear()
		{
			Query = string.Empty;
		}

		internal static bool ItemIsVisible(ItemDrop.ItemData item)
		{
			if (!HasQuery)
			{
				return true;
			}
			if (item?.m_shared == null)
			{
				return true;
			}
			return ItemMatch.ItemMatchesPattern(item, Query.Trim(), out _);
		}

		internal static void ApplyToGrid(InventoryGrid grid, Inventory inventory)
		{
			if ((Object)(object)grid == (Object)null || inventory == null)
			{
				return;
			}

			List<InventoryElement> elements = AccessTools.Field(typeof(InventoryGrid), "m_elements")?.GetValue(grid) as List<InventoryElement>;
			if (elements == null)
			{
				return;
			}

			for (int i = 0; i < elements.Count; i++)
			{
				InventoryElement element = elements[i];
				if ((Object)(object)element == (Object)null)
				{
					continue;
				}

				if (!HasQuery || !element.m_used)
				{
					RestoreClickable(element);
					continue;
				}

				ItemDrop.ItemData item = inventory.GetItemAt(element.Position.x, element.Position.y);
				if (ItemIsVisible(item))
				{
					RestoreClickable(element);
					continue;
				}

				HideOccupied(element);
			}
		}

		private static void RestoreClickable(InventoryElement element)
		{
			if ((Object)(object)element.m_button != (Object)null)
			{
				element.m_button.interactable = true;
			}
		}

		private static void HideOccupied(InventoryElement element)
		{
			SetGraphicEnabled(element.m_icon, false);
			SetGraphicEnabled(element.m_amount, false);
			SetGraphicEnabled(element.m_quality, false);
			SetGraphicEnabled(element.m_equiped, false);
			SetGraphicEnabled(element.m_queued, false);
			SetGraphicEnabled(element.m_noteleport, false);
			SetGraphicEnabled(element.m_food, false);
			if ((Object)(object)element.m_durability != (Object)null)
			{
				element.m_durability.gameObject.SetActive(false);
			}
			if ((Object)(object)element.m_tooltip != (Object)null)
			{
				element.m_tooltip.enabled = false;
			}
			if ((Object)(object)element.m_button != (Object)null)
			{
				element.m_button.interactable = false;
			}
			element.m_canBeDroppedOn = false;
		}

		private static void SetGraphicEnabled(Graphic graphic, bool enabled)
		{
			if ((Object)(object)graphic != (Object)null)
			{
				graphic.enabled = enabled;
			}
		}
	}
}
