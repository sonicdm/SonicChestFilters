using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SonicChestFilters
{
	internal static class ChestPanelUi
	{
		private const string FilterRootName = "SonicChestFilters_Root";

		private const float ToolbarHeight = 44f;

		private static GameObject _root;

		private static Button _clearButton;

		private static InputField _filterInput;

		private static string _lastFilterText = string.Empty;

		private static Container _boundContainer;

		private static bool _expanded;

		internal static bool FilterFieldFocused =>
			(Object)(object)_filterInput != (Object)null && _filterInput.isFocused;

		internal static void EnsureControls(InventoryGui gui)
		{
			if ((Object)(object)gui == (Object)null || (Object)(object)gui.m_container == (Object)null)
			{
				return;
			}

			if (SonicChestFiltersPlugin.Enabled == null || !SonicChestFiltersPlugin.Enabled.Value)
			{
				DestroyControls(gui);
				return;
			}

			Container current = GameAccess.Get<Container>(gui, "m_currentContainer");
			if ((Object)(object)current == (Object)null)
			{
				return;
			}

			if ((Object)(object)_boundContainer != (Object)(object)current)
			{
				RestoreFrame(gui);
				DestroyWidgets();
			}

			if (GUIManager.Instance == null)
			{
				return;
			}

			if ((Object)(object)_root == (Object)null)
			{
				CreateToolbar(gui);
				_boundContainer = current;
			}

			ApplyFrameLayout(gui);
			RefreshClearButton();
		}

		internal static void Tick()
		{
			SyncFilterField();
		}

		internal static void ResetFilterText()
		{
			_lastFilterText = string.Empty;
			ChestFilter.Clear();
			if ((Object)(object)_filterInput != (Object)null)
			{
				_filterInput.SetTextWithoutNotify(string.Empty);
			}
			RefreshClearButton();
			RefreshContainerGrid();
		}

		internal static void DestroyControls()
		{
			DestroyControls(InventoryGui.instance);
		}

		internal static void DestroyControls(InventoryGui gui)
		{
			RestoreFrame(gui);
			DestroyWidgets();
			_boundContainer = null;
		}

		private static void CreateToolbar(InventoryGui gui)
		{
			RectTransform container = gui.m_container;
			_root = new GameObject(FilterRootName, typeof(RectTransform));
			_root.transform.SetParent(container, false);
			RectTransform rootRt = _root.GetComponent<RectTransform>();
			rootRt.anchorMin = new Vector2(0f, 1f);
			rootRt.anchorMax = new Vector2(1f, 1f);
			rootRt.pivot = new Vector2(0.5f, 1f);
			rootRt.sizeDelta = new Vector2(0f, ToolbarHeight);

			const float filterWidth = 170f;
			const float buttonWidth = 80f;
			const float gap = 8f;
			float x = 16f + filterWidth * 0.5f;
			Vector2 childAnchor = new Vector2(0f, 0.5f);

			if (SonicChestFiltersPlugin.EnableFilterBox == null || SonicChestFiltersPlugin.EnableFilterBox.Value)
			{
				GameObject inputGo = GUIManager.Instance.CreateInputField(
					parent: rootRt,
					anchorMin: childAnchor,
					anchorMax: childAnchor,
					position: new Vector2(x, 0f),
					contentType: InputField.ContentType.Standard,
					placeholderText: ModLocalization.Translate(ModLocalization.FilterPlaceholder, "Filter..."),
					fontSize: 16,
					width: filterWidth,
					height: 28f);
				if ((Object)(object)inputGo != (Object)null)
				{
					inputGo.name = "SonicChestFilters_FilterField";
					_filterInput = inputGo.GetComponent<InputField>();
					if ((Object)(object)_filterInput != (Object)null)
					{
						_filterInput.characterLimit = 64;
						_filterInput.SetTextWithoutNotify(string.Empty);
						_filterInput.onValueChanged.AddListener(OnFilterChanged);
						_filterInput.onEndEdit.AddListener(OnFilterChanged);
					}
					x += filterWidth * 0.5f + gap + buttonWidth * 0.5f;
				}

				GameObject clearGo = GUIManager.Instance.CreateButton(
					text: ModLocalization.Translate(ModLocalization.Clear, "Clear"),
					parent: rootRt,
					anchorMin: childAnchor,
					anchorMax: childAnchor,
					position: new Vector2(x, 0f),
					width: buttonWidth,
					height: 28f);
				if ((Object)(object)clearGo != (Object)null)
				{
					clearGo.name = "SonicChestFilters_Clear";
					_clearButton = clearGo.GetComponent<Button>();
					if ((Object)(object)_clearButton != (Object)null)
					{
						_clearButton.onClick.AddListener(OnClearClicked);
					}
					x += buttonWidth + gap;
				}
			}

			if (SonicChestFiltersPlugin.EnableSortButton == null || SonicChestFiltersPlugin.EnableSortButton.Value)
			{
				GameObject sortGo = GUIManager.Instance.CreateButton(
					text: ModLocalization.Translate(ModLocalization.Sort, "Sort"),
					parent: rootRt,
					anchorMin: childAnchor,
					anchorMax: childAnchor,
					position: new Vector2(x, 0f),
					width: buttonWidth,
					height: 28f);
				if ((Object)(object)sortGo != (Object)null)
				{
					sortGo.name = "SonicChestFilters_Sort";
					Button sortButton = sortGo.GetComponent<Button>();
					if ((Object)(object)sortButton != (Object)null)
					{
						sortButton.onClick.AddListener(OnSortClicked);
					}
				}
			}

			_lastFilterText = string.Empty;
			ChestFilter.Clear();
			ChestSort.ResetSession();
		}

		private static void ApplyFrameLayout(InventoryGui gui)
		{
			if (!_expanded)
			{
				GrowDown(gui.m_container, ToolbarHeight);
				GrowDown(FindBackground(gui.m_container), ToolbarHeight);
				ShiftDown(GetGridShiftTarget(gui), ToolbarHeight);
				_expanded = true;
			}

			PositionToolbar(gui);
		}

		private static void RestoreFrame(InventoryGui gui)
		{
			if (!_expanded || (Object)(object)gui == (Object)null || (Object)(object)gui.m_container == (Object)null)
			{
				_expanded = false;
				return;
			}

			GrowDown(gui.m_container, -ToolbarHeight);
			GrowDown(FindBackground(gui.m_container), -ToolbarHeight);
			ShiftDown(GetGridShiftTarget(gui), -ToolbarHeight);
			_expanded = false;
		}

		private static void PositionToolbar(InventoryGui gui)
		{
			if ((Object)(object)_root == (Object)null)
			{
				return;
			}

			RectTransform container = gui.m_container;
			RectTransform rootRt = _root.GetComponent<RectTransform>();
			rootRt.anchorMin = new Vector2(0f, 1f);
			rootRt.anchorMax = new Vector2(1f, 1f);
			rootRt.pivot = new Vector2(0.5f, 1f);
			rootRt.offsetMin = new Vector2(10f, rootRt.offsetMin.y);
			rootRt.offsetMax = new Vector2(-10f, rootRt.offsetMax.y);

			float headerBottom = GetHeaderBottomLocalY(gui, container);
			Vector2 pos = rootRt.anchoredPosition;
			pos.y = LocalYToTopOffset(container, headerBottom) - 2f;
			rootRt.anchoredPosition = pos;

			Vector2 size = rootRt.sizeDelta;
			size.y = ToolbarHeight;
			rootRt.sizeDelta = size;
		}

		private static float LocalYToTopOffset(RectTransform container, float localY)
		{
			float height = container.rect.height;
			float pivotY = container.pivot.y;
			float topLocal = (1f - pivotY) * height;
			return localY - topLocal;
		}

		private static float GetHeaderBottomLocalY(InventoryGui gui, RectTransform container)
		{
			float lowest = float.PositiveInfinity;
			ConsiderHeader(gui.m_stackAllButton, container, ref lowest);
			ConsiderHeader(gui.m_takeAllButton, container, ref lowest);
			ConsiderHeader(gui.m_containerName, container, ref lowest);
			if (float.IsPositiveInfinity(lowest))
			{
				float height = container.rect.height;
				return (1f - container.pivot.y) * height - 36f;
			}
			return lowest;
		}

		private static void ConsiderHeader(Component component, RectTransform container, ref float lowest)
		{
			if ((Object)(object)component == (Object)null)
			{
				return;
			}
			RectTransform rt = component.GetComponent<RectTransform>();
			if ((Object)(object)rt == (Object)null || !rt.gameObject.activeInHierarchy)
			{
				return;
			}
			Vector3[] corners = new Vector3[4];
			rt.GetWorldCorners(corners);
			Vector3 bottomLeft = container.InverseTransformPoint(corners[0]);
			if (bottomLeft.y < lowest)
			{
				lowest = bottomLeft.y;
			}
		}

		private static RectTransform GetGridShiftTarget(InventoryGui gui)
		{
			InventoryGrid grid = GameAccess.Get<InventoryGrid>(gui, "m_containerGrid");
			if ((Object)(object)grid == (Object)null)
			{
				return null;
			}

			RectTransform gridRt = grid.GetComponent<RectTransform>();
			if ((Object)(object)gridRt != (Object)null && (Object)(object)gridRt != (Object)(object)gui.m_container)
			{
				return gridRt;
			}

			if ((Object)(object)grid.m_gridRoot != (Object)null && (Object)(object)grid.m_gridRoot != (Object)(object)gui.m_container)
			{
				return grid.m_gridRoot;
			}

			return null;
		}

		private static RectTransform FindBackground(RectTransform container)
		{
			if ((Object)(object)container == (Object)null)
			{
				return null;
			}

			Transform bkg = container.Find("Bkg");
			if ((Object)(object)bkg == (Object)null)
			{
				return null;
			}

			RectTransform bkgRt = bkg as RectTransform;
			if ((Object)(object)bkgRt == (Object)null || (Object)(object)bkgRt == (Object)(object)container)
			{
				return null;
			}

			return bkgRt;
		}

		private static void GrowDown(RectTransform rt, float extra)
		{
			if ((Object)(object)rt == (Object)null)
			{
				return;
			}

			if (!Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y))
			{
				Vector2 min = rt.offsetMin;
				min.y -= extra;
				rt.offsetMin = min;
				return;
			}

			float height = rt.rect.height;
			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + extra);
			rt.anchoredPosition += new Vector2(0f, -extra * (1f - rt.pivot.y));
		}

		private static void ShiftDown(RectTransform rt, float extra)
		{
			if ((Object)(object)rt == (Object)null)
			{
				return;
			}
			rt.anchoredPosition += new Vector2(0f, -extra);
		}

		private static void DestroyWidgets()
		{
			_filterInput = null;
			_clearButton = null;
			_lastFilterText = string.Empty;
			if ((Object)(object)_root != (Object)null)
			{
				Object.Destroy(_root);
				_root = null;
			}
		}

		private static void SyncFilterField()
		{
			if ((Object)(object)_filterInput == (Object)null)
			{
				return;
			}
			string text = _filterInput.text ?? string.Empty;
			if (text == _lastFilterText)
			{
				return;
			}
			OnFilterChanged(text);
		}

		private static void OnFilterChanged(string text)
		{
			string value = text ?? string.Empty;
			if (value == _lastFilterText)
			{
				return;
			}
			_lastFilterText = value;
			ChestFilter.SetQuery(value);
			RefreshClearButton();
			RefreshContainerGrid();
		}

		private static void OnClearClicked()
		{
			ResetFilterText();
		}

		private static void OnSortClicked()
		{
			ChestSort.SortCurrent(InventoryGui.instance);
			RefreshContainerGrid();
		}

		private static void RefreshClearButton()
		{
			if ((Object)(object)_clearButton != (Object)null)
			{
				_clearButton.interactable = ChestFilter.HasQuery;
			}
		}

		private static void RefreshContainerGrid()
		{
			InventoryGui gui = InventoryGui.instance;
			InventoryGrid grid = GameAccess.Get<InventoryGrid>(gui, "m_containerGrid");
			if ((Object)(object)gui == (Object)null || (Object)(object)grid == (Object)null)
			{
				return;
			}
			Container container = GameAccess.Get<Container>(gui, "m_currentContainer");
			if ((Object)(object)container == (Object)null)
			{
				return;
			}
			Inventory inventory = container.GetInventory();
			if (inventory == null)
			{
				return;
			}

			ItemDrop.ItemData dragItem = GameAccess.Get<ItemDrop.ItemData>(gui, "m_dragItem");
			grid.UpdateInventory(inventory, Player.m_localPlayer, dragItem);
		}
	}

	[HarmonyPatch(typeof(InventoryGui), "UpdateContainer")]
	internal static class InventoryGui_UpdateContainer_Patch
	{
		private static void Postfix(InventoryGui __instance)
		{
			if ((Object)(object)__instance == (Object)null)
			{
				return;
			}
			if ((Object)(object)GameAccess.Get<Container>(__instance, "m_currentContainer") == (Object)null)
			{
				return;
			}
			ChestPanelUi.EnsureControls(__instance);
		}
	}

	[HarmonyPatch(typeof(InventoryGui), "CloseContainer")]
	internal static class InventoryGui_CloseContainer_Patch
	{
		private static void Postfix(InventoryGui __instance)
		{
			ChestFilter.Clear();
			ChestSort.ResetSession();
			ChestPanelUi.DestroyControls(__instance);
		}
	}

	[HarmonyPatch(typeof(InventoryGrid), "UpdateInventory")]
	internal static class InventoryGrid_UpdateInventory_Patch
	{
		private static void Postfix(InventoryGrid __instance, Inventory inventory)
		{
			InventoryGui gui = InventoryGui.instance;
			InventoryGrid containerGrid = GameAccess.Get<InventoryGrid>(gui, "m_containerGrid");
			if ((Object)(object)gui == (Object)null || (Object)(object)__instance != (Object)(object)containerGrid)
			{
				return;
			}
			ChestFilter.ApplyToGrid(__instance, inventory);
		}
	}

	[HarmonyPatch(typeof(Player), "TakeInput")]
	internal static class Player_TakeInput_Patch
	{
		private static bool Prefix(ref bool __result)
		{
			if (!ChestPanelUi.FilterFieldFocused)
			{
				return true;
			}
			__result = false;
			return false;
		}
	}
}
