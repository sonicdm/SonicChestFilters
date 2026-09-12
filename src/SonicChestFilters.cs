using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SonicChestFilters
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	[BepInDependency(Jotunn.Main.ModGuid)]
	[BepInDependency(NearbyCraftingForkedGuid, BepInDependency.DependencyFlags.SoftDependency)]
	public sealed class SonicChestFiltersPlugin : BaseUnityPlugin
	{
		public const string PluginGuid = "com.sonicdm.valheim.sonicchestfilters";

		public const string PluginName = "Sonic Chest Filters";

		public const string PluginVersion = "1.0.2";

		internal const string NearbyCraftingForkedGuid = "com.sonicdm.valheim.nearbycraftingforked";

		internal static ManualLogSource ModLogger;

		internal static ConfigEntry<bool> Enabled;

		internal static ConfigEntry<bool> AutoReloadConfig;

		internal static ConfigEntry<bool> DebugLogging;

		internal static ConfigEntry<float> ContainerRange;

		internal static ConfigEntry<bool> RequirePlayerPlacedContainer;

		internal static ConfigEntry<bool> IgnoreMovingContainers;

		internal static ConfigEntry<bool> IgnoreObliterators;

		internal static ConfigEntry<bool> BalrondCompatibility;

		internal static ConfigEntry<bool> EnableFilterBox;

		internal static ConfigEntry<bool> EnableSortButton;

		internal static ConfigEntry<bool> ItemLocateEnabled;

		internal static ConfigEntry<int> ItemLocateMaxHighlights;

		internal static ConfigEntry<float> ItemLocateDurationSeconds;

		internal static ConfigEntry<string> ItemLocateGlowColor;

		internal static bool NearbyCraftingForkedPresent { get; private set; }

		internal static bool DebugEnabled => DebugLogging != null && DebugLogging.Value;

		internal static bool LocateIsActive
		{
			get
			{
				if (NearbyCraftingForkedPresent)
				{
					return false;
				}
				if (Enabled == null || !Enabled.Value)
				{
					return false;
				}
				return ItemLocateEnabled != null && ItemLocateEnabled.Value;
			}
		}

		private Harmony _harmony;

		private DateTime _configLastWriteUtc;

		private float _configWatchNextCheck;

		private DateTime _configPendingWriteUtc;

		private float _configPendingSince = -1f;

		private bool _loggedLocateSkip;

		private void Awake()
		{
			ModLogger = Logger;
			Enabled = Config.Bind("General", "Enabled", true, "Enable Sonic Chest Filters.");
			AutoReloadConfig = Config.Bind("General", "AutoReloadConfig", true, "Automatically reload this mod's config when the .cfg file changes on disk.");
			DebugLogging = Config.Bind("General", "DebugLogging", false, "Write diagnostic information to BepInEx LogOutput.log.");
			ContainerRange = Config.Bind("Containers", "ContainerRange", 20f, new ConfigDescription("Maximum distance in metres from the player when locating chests.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(1f, 100f), Array.Empty<object>()));
			RequirePlayerPlacedContainer = Config.Bind("Containers", "RequirePlayerPlacedContainer", true, "Only locate containers attached to a Piece (normally player-built storage).");
			IgnoreMovingContainers = Config.Bind("Containers", "IgnoreMovingContainers", true, "Ignore ships, and ignore carts that are currently attached or in use. Parked carts stay eligible for locate.");
			IgnoreObliterators = Config.Bind("Containers", "IgnoreObliterators", true, "Ignore Obliterators (Incinerator) when locating items.");
			BalrondCompatibility = Config.Bind("Containers", "BalrondConstructions", true, "Recognize storage prefabs where Container and Piece are siblings under the same prefab root.");
			EnableFilterBox = Config.Bind("UI", "EnableFilterBox", true, "Show a live Valheim text box on the open chest to filter visible items.");
			EnableSortButton = Config.Bind("UI", "EnableSortButton", true, "Show a Sort button on the open chest.");
			ItemLocateEnabled = Config.Bind("Item Locate", "Enabled", true, "Enable the 'locate' console command. Ignored when Nearby Crafting Forked is loaded.");
			ItemLocateMaxHighlights = Config.Bind("Item Locate", "MaxHighlights", 10, new ConfigDescription("Maximum chests to glow (nearest first).", (AcceptableValueBase)(object)new AcceptableValueRange<int>(1, 50), Array.Empty<object>()));
			ItemLocateDurationSeconds = Config.Bind("Item Locate", "DurationSeconds", 15f, new ConfigDescription("How long chest glows last before auto-clear.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(3f, 120f), Array.Empty<object>()));
			ItemLocateGlowColor = Config.Bind("Item Locate", "GlowColor", "1,0.85,0.2", "Emission tint for highlighted chests as R,G,B in 0-1 range.");

			ModLocalization.Register();
			DetectNearbyCraftingForked();
			if (LocateIsActive)
			{
				ItemLocate.RegisterCommand();
			}

			ContainerRange.SettingChanged += OnContainerSettingChanged;
			RequirePlayerPlacedContainer.SettingChanged += OnContainerSettingChanged;
			IgnoreMovingContainers.SettingChanged += OnContainerSettingChanged;
			IgnoreObliterators.SettingChanged += OnContainerSettingChanged;
			BalrondCompatibility.SettingChanged += OnContainerSettingChanged;
			Enabled.SettingChanged += OnEnabledChanged;

			_configLastWriteUtc = GetConfigWriteTimeUtc();
			_harmony = new Harmony(PluginGuid);
			try
			{
				_harmony.PatchAll();
				Logger.LogInfo((object)(PluginName + " " + PluginVersion + " loaded."));
			}
			catch (Exception ex)
			{
				try
				{
					_harmony.UnpatchSelf();
				}
				catch
				{
				}
				Enabled.Value = false;
				Logger.LogError((object)(PluginName + " failed to apply its Harmony patches and has been disabled."));
				Logger.LogError((object)ex);
			}
		}

		private void Start()
		{
			DetectNearbyCraftingForked();
			if (LocateIsActive)
			{
				ItemLocate.RegisterCommand();
			}
		}

		private void DetectNearbyCraftingForked()
		{
			NearbyCraftingForkedPresent = Chainloader.PluginInfos != null
				&& Chainloader.PluginInfos.ContainsKey(NearbyCraftingForkedGuid);
			if (NearbyCraftingForkedPresent && !_loggedLocateSkip)
			{
				_loggedLocateSkip = true;
				Logger.LogInfo((object)"Item locate disabled: Nearby Crafting Forked is loaded (use nearby / locate).");
			}
		}

		private void Update()
		{
			WatchConfigFileForChanges();
			if (LocateIsActive)
			{
				ItemLocate.Tick();
			}
			ChestPanelUi.Tick();
		}

		private static void OnContainerSettingChanged(object sender, EventArgs e)
		{
			NearbyContainers.InvalidateAll();
			if (DebugEnabled)
			{
				Debug("Container-related configuration changed; caches invalidated.");
			}
		}

		private static void OnEnabledChanged(object sender, EventArgs e)
		{
			ResetRuntimeState();
		}

		internal static void ResetRuntimeState()
		{
			NearbyContainers.InvalidateAll();
			ItemLocate.ClearHighlights();
			ChestFilter.Clear();
			ChestSort.ResetSession();
			ChestPanelUi.ResetFilterText();
		}

		internal static void Debug(string message)
		{
			if (DebugEnabled && ModLogger != null)
			{
				ModLogger.LogInfo((object)("[DEBUG] " + message));
			}
		}

		private void WatchConfigFileForChanges()
		{
			if (AutoReloadConfig == null || !AutoReloadConfig.Value)
			{
				_configPendingSince = -1f;
				return;
			}
			float now = Time.unscaledTime;
			if (now < _configWatchNextCheck)
			{
				return;
			}
			_configWatchNextCheck = now + 1f;
			DateTime writeUtc = GetConfigWriteTimeUtc();
			if (writeUtc == DateTime.MinValue)
			{
				return;
			}
			if (writeUtc == _configLastWriteUtc)
			{
				_configPendingSince = -1f;
				return;
			}
			if (_configPendingSince < 0f || writeUtc != _configPendingWriteUtc)
			{
				_configPendingWriteUtc = writeUtc;
				_configPendingSince = now;
				return;
			}
			if (now - _configPendingSince < 0.75f)
			{
				return;
			}
			_configLastWriteUtc = writeUtc;
			_configPendingSince = -1f;
			ReloadConfigFromDisk();
		}

		private DateTime GetConfigWriteTimeUtc()
		{
			try
			{
				string path = Config.ConfigFilePath;
				if (string.IsNullOrEmpty(path) || !File.Exists(path))
				{
					return DateTime.MinValue;
				}
				return File.GetLastWriteTimeUtc(path);
			}
			catch
			{
				return DateTime.MinValue;
			}
		}

		private void ReloadConfigFromDisk()
		{
			try
			{
				Config.Reload();
				_configLastWriteUtc = GetConfigWriteTimeUtc();
				_configPendingSince = -1f;
				ResetRuntimeState();
				Logger.LogInfo((object)(PluginName + " config reloaded from disk."));
			}
			catch (Exception ex)
			{
				Logger.LogError((object)(PluginName + " failed to reload config from disk."));
				Logger.LogError((object)ex);
			}
		}

		private void OnDestroy()
		{
			if (ContainerRange != null)
			{
				ContainerRange.SettingChanged -= OnContainerSettingChanged;
			}
			if (RequirePlayerPlacedContainer != null)
			{
				RequirePlayerPlacedContainer.SettingChanged -= OnContainerSettingChanged;
			}
			if (IgnoreMovingContainers != null)
			{
				IgnoreMovingContainers.SettingChanged -= OnContainerSettingChanged;
			}
			if (IgnoreObliterators != null)
			{
				IgnoreObliterators.SettingChanged -= OnContainerSettingChanged;
			}
			if (BalrondCompatibility != null)
			{
				BalrondCompatibility.SettingChanged -= OnContainerSettingChanged;
			}
			if (Enabled != null)
			{
				Enabled.SettingChanged -= OnEnabledChanged;
			}
			ResetRuntimeState();
			ChestPanelUi.DestroyControls();
			_harmony?.UnpatchSelf();
		}
	}
}
