using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SonicChestFilters
{
	internal static class ItemLocate
	{
		private const string EmissionColorProperty = "_EmissionColor";

		private static readonly List<HighlightedChest> Active = new List<HighlightedChest>();

		private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

		private static bool _commandRegistered;

		private static float _activeUntil = -1f;

		private static Color _baseGlow = new Color(1f, 0.85f, 0.2f, 1f);

		internal static bool IsActive => SonicChestFiltersPlugin.LocateIsActive;

		internal static void RegisterCommand()
		{
			if (_commandRegistered || !IsActive || CommandManager.Instance == null)
			{
				return;
			}
			_commandRegistered = true;
			CommandManager.Instance.AddConsoleCommand(new FindCommand());
			CommandManager.Instance.AddConsoleCommand(new FindItemCommand());
		}

		internal static List<string> CommandOptions()
		{
			return new List<string> { "clear" };
		}

		internal static void ClearHighlights()
		{
			for (int i = 0; i < Active.Count; i++)
			{
				Active[i].Restore();
			}
			Active.Clear();
			_activeUntil = -1f;
		}

		internal static void Tick()
		{
			if (!IsActive || Active.Count == 0)
			{
				return;
			}

			float now = Time.realtimeSinceStartup;
			if (now >= _activeUntil)
			{
				ClearHighlights();
				return;
			}

			float pulse = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(now * 5f));
			Color glow = _baseGlow * pulse;
			glow.a = 1f;
			for (int i = 0; i < Active.Count; i++)
			{
				Active[i].ApplyGlow(glow);
			}
		}

		internal static void OnCommand(string[] args, Terminal context)
		{
			if (!IsActive)
			{
				Say("Item locate is disabled.", context);
				return;
			}

			if (args != null && args.Length >= 1 && string.Equals(args[0], "clear", StringComparison.OrdinalIgnoreCase))
			{
				ClearHighlights();
				Say("Cleared nearby chest highlights.", context);
				return;
			}

			Player player = Player.m_localPlayer;
			if ((Object)(object)player == (Object)null)
			{
				Say("No local player.", context);
				return;
			}

			string pattern;
			string displayName;
			if (args == null || args.Length == 0)
			{
				if (!TryGetHeldItemPattern(player, out pattern, out displayName))
				{
					Say("Hold an item or use: find <item name>", context);
					return;
				}
			}
			else
			{
				pattern = string.Join(" ", args).Trim();
				if (pattern.Length == 0)
				{
					Say("Usage: find [item|*pattern*|clear]", context);
					return;
				}
				displayName = pattern;
			}

			SearchAndHighlight(player, pattern, displayName, context);
		}

		private static bool TryGetHeldItemPattern(Player player, out string pattern, out string displayName)
		{
			pattern = string.Empty;
			displayName = string.Empty;
			Humanoid humanoid = (Humanoid)(object)player;
			ItemDrop.ItemData item = humanoid.RightItem ?? humanoid.LeftItem;
			if (item?.m_shared == null)
			{
				return false;
			}

			string prefabName = null;
			if ((Object)(object)item.m_dropPrefab != (Object)null)
			{
				prefabName = ((Object)item.m_dropPrefab).name;
			}

			if (!string.IsNullOrEmpty(prefabName))
			{
				pattern = prefabName;
				displayName = prefabName;
				return true;
			}

			pattern = item.m_shared.m_name;
			displayName = ItemMatch.FormatDisplayName(item.m_shared.m_name);
			return !string.IsNullOrEmpty(pattern);
		}

		private static void SearchAndHighlight(Player player, string pattern, string displayName, Terminal context)
		{
			ClearHighlights();
			_baseGlow = ParseGlowColor(SonicChestFiltersPlugin.ItemLocateGlowColor?.Value);
			int maxHighlights = SonicChestFiltersPlugin.ItemLocateMaxHighlights != null
				? Mathf.Clamp(SonicChestFiltersPlugin.ItemLocateMaxHighlights.Value, 1, 50)
				: 10;
			float duration = SonicChestFiltersPlugin.ItemLocateDurationSeconds != null
				? Mathf.Clamp(SonicChestFiltersPlugin.ItemLocateDurationSeconds.Value, 3f, 120f)
				: 15f;

			List<Container> containers = NearbyContainers.Get(player);
			int matchCount = 0;
			List<string> matchedLabels = new List<string>();

			for (int i = 0; i < containers.Count; i++)
			{
				Container container = containers[i];
				if ((Object)(object)container == (Object)null)
				{
					continue;
				}

				Inventory inventory;
				try
				{
					inventory = container.GetInventory();
				}
				catch
				{
					continue;
				}

				if (inventory == null || !InventoryContains(inventory, pattern, matchedLabels))
				{
					continue;
				}

				matchCount++;
				if (Active.Count < maxHighlights)
				{
					HighlightedChest highlight = HighlightedChest.TryCreate(container);
					if (highlight != null)
					{
						Active.Add(highlight);
					}
				}
			}

			string label = matchedLabels.Count > 0
				? string.Join(", ", matchedLabels)
				: ItemMatch.FormatDisplayName(displayName);
			if (matchCount == 0)
			{
				Say($"No nearby chests contain {ItemMatch.FormatDisplayName(displayName)}.", context);
				return;
			}

			_activeUntil = Time.realtimeSinceStartup + duration;
			Tick();

			if (matchCount > Active.Count)
			{
				Say($"Found {label} in {matchCount} chests (showing nearest {Active.Count}).", context);
			}
			else
			{
				Say($"Found {label} in {matchCount} chests.", context);
			}

			if (SonicChestFiltersPlugin.DebugEnabled)
			{
				SonicChestFiltersPlugin.Debug($"Item locate '{pattern}': matches={matchCount}, glowing={Active.Count}, duration={duration:0.#}s.");
			}
		}

		private static bool InventoryContains(Inventory inventory, string pattern, List<string> matchedLabels)
		{
			List<ItemDrop.ItemData> items = inventory.GetAllItems();
			if (items == null || items.Count == 0)
			{
				return false;
			}

			bool found = false;
			for (int i = 0; i < items.Count; i++)
			{
				ItemDrop.ItemData item = items[i];
				if (!ItemMatch.ItemMatchesPattern(item, pattern, out string matchedLabel))
				{
					continue;
				}

				found = true;
				AddUniqueLabel(matchedLabels, matchedLabel);
			}

			return found;
		}

		private static void AddUniqueLabel(List<string> labels, string label)
		{
			if (string.IsNullOrEmpty(label))
			{
				return;
			}

			for (int i = 0; i < labels.Count; i++)
			{
				if (string.Equals(labels[i], label, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}

			labels.Add(label);
		}

		private static Color ParseGlowColor(string raw)
		{
			Color fallback = new Color(1f, 0.85f, 0.2f, 1f);
			if (string.IsNullOrWhiteSpace(raw))
			{
				return fallback;
			}

			string[] parts = raw.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 3)
			{
				return fallback;
			}

			if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r)
				|| !float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float g)
				|| !float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float b))
			{
				return fallback;
			}

			return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f);
		}

		private static void Say(string message, Terminal context)
		{
			if ((Object)(object)context != (Object)null)
			{
				context.AddString(message);
				return;
			}
			if ((Object)(object)Chat.instance != (Object)null)
			{
				Chat.instance.AddString(message);
			}
			else if (SonicChestFiltersPlugin.ModLogger != null)
			{
				SonicChestFiltersPlugin.ModLogger.LogInfo((object)message);
			}
		}

		private sealed class HighlightedChest
		{
			private readonly List<RendererGlow> _renderers = new List<RendererGlow>();

			internal static HighlightedChest TryCreate(Container container)
			{
				Renderer[] renderers = ((Component)container).GetComponentsInChildren<Renderer>(true);
				if (renderers == null || renderers.Length == 0)
				{
					return null;
				}

				HighlightedChest highlight = new HighlightedChest();
				for (int i = 0; i < renderers.Length; i++)
				{
					Renderer renderer = renderers[i];
					if ((Object)(object)renderer == (Object)null || renderer is ParticleSystemRenderer)
					{
						continue;
					}

					RendererGlow glow = RendererGlow.TryCreate(renderer);
					if (glow != null)
					{
						highlight._renderers.Add(glow);
					}
				}

				if (highlight._renderers.Count == 0)
				{
					return null;
				}

				return highlight;
			}

			internal void ApplyGlow(Color color)
			{
				for (int i = 0; i < _renderers.Count; i++)
				{
					_renderers[i].Apply(color);
				}
			}

			internal void Restore()
			{
				for (int i = 0; i < _renderers.Count; i++)
				{
					_renderers[i].Restore();
				}
				_renderers.Clear();
			}
		}

		private sealed class RendererGlow
		{
			private readonly Renderer _renderer;

			private readonly bool _usedMaterialInstances;

			private readonly Material[] _originalSharedMaterials;

			private readonly Material[] _instancedMaterials;

			private readonly Color[] _originalEmission;

			private readonly bool[] _hadEmissionKeyword;

			private RendererGlow(Renderer renderer, bool usedMaterialInstances, Material[] originalSharedMaterials, Material[] instancedMaterials, Color[] originalEmission, bool[] hadEmissionKeyword)
			{
				_renderer = renderer;
				_usedMaterialInstances = usedMaterialInstances;
				_originalSharedMaterials = originalSharedMaterials;
				_instancedMaterials = instancedMaterials;
				_originalEmission = originalEmission;
				_hadEmissionKeyword = hadEmissionKeyword;
			}

			internal static RendererGlow TryCreate(Renderer renderer)
			{
				Material[] shared = renderer.sharedMaterials;
				if (shared == null || shared.Length == 0)
				{
					return null;
				}

				renderer.GetPropertyBlock(PropertyBlock);
				PropertyBlock.SetColor(EmissionColorProperty, Color.black);
				renderer.SetPropertyBlock(PropertyBlock);
				if (SupportsEmissionViaPropertyBlock(renderer))
				{
					return new RendererGlow(renderer, false, null, null, null, null);
				}

				Material[] originals = new Material[shared.Length];
				Material[] instances = new Material[shared.Length];
				Color[] originalEmission = new Color[shared.Length];
				bool[] hadKeyword = new bool[shared.Length];
				bool any = false;
				for (int i = 0; i < shared.Length; i++)
				{
					Material mat = shared[i];
					originals[i] = mat;
					if ((Object)(object)mat == (Object)null)
					{
						instances[i] = mat;
						continue;
					}

					Material instance = new Material(mat);
					instances[i] = instance;
					hadKeyword[i] = instance.IsKeywordEnabled("_EMISSION");
					if (instance.HasProperty(EmissionColorProperty))
					{
						originalEmission[i] = instance.GetColor(EmissionColorProperty);
						instance.EnableKeyword("_EMISSION");
						any = true;
					}
					else
					{
						originalEmission[i] = Color.black;
					}
				}

				if (!any)
				{
					for (int i = 0; i < instances.Length; i++)
					{
						if ((Object)(object)instances[i] != (Object)null && (Object)(object)instances[i] != (Object)(object)originals[i])
						{
							Object.Destroy(instances[i]);
						}
					}
					return null;
				}

				renderer.materials = instances;
				return new RendererGlow(renderer, true, originals, instances, originalEmission, hadKeyword);
			}

			private static bool SupportsEmissionViaPropertyBlock(Renderer renderer)
			{
				Material[] shared = renderer.sharedMaterials;
				for (int i = 0; i < shared.Length; i++)
				{
					Material mat = shared[i];
					if ((Object)(object)mat != (Object)null && mat.HasProperty(EmissionColorProperty))
					{
						return true;
					}
				}
				return false;
			}

			internal void Apply(Color color)
			{
				if ((Object)(object)_renderer == (Object)null)
				{
					return;
				}

				if (!_usedMaterialInstances)
				{
					_renderer.GetPropertyBlock(PropertyBlock);
					PropertyBlock.SetColor(EmissionColorProperty, color);
					_renderer.SetPropertyBlock(PropertyBlock);
					return;
				}

				if (_instancedMaterials == null)
				{
					return;
				}

				for (int i = 0; i < _instancedMaterials.Length; i++)
				{
					Material mat = _instancedMaterials[i];
					if ((Object)(object)mat != (Object)null && mat.HasProperty(EmissionColorProperty))
					{
						mat.SetColor(EmissionColorProperty, color);
					}
				}
			}

			internal void Restore()
			{
				if ((Object)(object)_renderer == (Object)null)
				{
					return;
				}

				if (!_usedMaterialInstances)
				{
					_renderer.GetPropertyBlock(PropertyBlock);
					PropertyBlock.SetColor(EmissionColorProperty, Color.black);
					_renderer.SetPropertyBlock(PropertyBlock);
					return;
				}

				if (_originalSharedMaterials != null)
				{
					_renderer.sharedMaterials = _originalSharedMaterials;
				}

				if (_instancedMaterials != null)
				{
					for (int i = 0; i < _instancedMaterials.Length; i++)
					{
						Material mat = _instancedMaterials[i];
						if ((Object)(object)mat == (Object)null)
						{
							continue;
						}
						if (_originalEmission != null && mat.HasProperty(EmissionColorProperty))
						{
							mat.SetColor(EmissionColorProperty, _originalEmission[i]);
						}
						if (_hadEmissionKeyword != null && !_hadEmissionKeyword[i])
						{
							mat.DisableKeyword("_EMISSION");
						}
						Object.Destroy(mat);
					}
				}
			}
		}
	}

	internal sealed class FindCommand : ConsoleCommand
	{
		public override string Name => "find";

		public override string Help =>
			"[item|clear] Highlight nearby eligible chests containing an item (or clear). No args uses held item.";

		public override void Run(string[] args, Terminal context)
		{
			ItemLocate.OnCommand(args, context);
		}

		public override List<string> CommandOptionList()
		{
			return ItemLocate.CommandOptions();
		}
	}

	internal sealed class FindItemCommand : ConsoleCommand
	{
		public override string Name => "finditem";

		public override string Help => "[item|clear] Alias for find — highlight chests containing an item.";

		public override void Run(string[] args, Terminal context)
		{
			ItemLocate.OnCommand(args, context);
		}

		public override List<string> CommandOptionList()
		{
			return ItemLocate.CommandOptions();
		}
	}
}
