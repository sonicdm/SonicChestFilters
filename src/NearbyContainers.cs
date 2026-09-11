using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SonicChestFilters
{
	internal readonly struct ContainerTraits
	{
		internal readonly Container Container;

		internal readonly bool HasPiece;

		internal readonly bool IsMoving;

		internal readonly bool IsIncinerator;

		internal ContainerTraits(Container container, bool hasPiece, bool isMoving, bool isIncinerator)
		{
			Container = container;
			HasPiece = hasPiece;
			IsMoving = isMoving;
			IsIncinerator = isIncinerator;
		}
	}

	internal readonly struct ContainerDistance
	{
		internal readonly Container Container;

		internal readonly float DistanceSq;

		internal ContainerDistance(Container container, float distanceSq)
		{
			Container = container;
			DistanceSq = distanceSq;
		}
	}

	internal static class NearbyContainers
	{
		private static readonly List<Container> CachedContainers = new List<Container>();

		private static readonly List<ContainerDistance> ScanBuffer = new List<ContainerDistance>();

		private static readonly Dictionary<int, ContainerTraits> TraitCache = new Dictionary<int, ContainerTraits>();

		private static Player _cachedPlayer;

		private static Vector3 _cachedPlayerPosition;

		private static float _containerCacheCreatedAt = -1000f;

		private static float _cachedRange = -1f;

		private static bool _cachedRequirePiece;

		private static bool _cachedIgnoreMoving;

		private static bool _cachedIgnoreObliterators;

		private static bool _cachedBalrondCompatibility;

		internal static void InvalidateAll()
		{
			CachedContainers.Clear();
			ScanBuffer.Clear();
			TraitCache.Clear();
			_cachedPlayer = null;
			_containerCacheCreatedAt = -1000f;
			_cachedRange = -1f;
		}

		internal static List<Container> Get(Player player)
		{
			if ((Object)(object)player == (Object)null)
			{
				return CachedContainers;
			}
			float now = Time.realtimeSinceStartup;
			if (ContainerCacheIsValid(player, now))
			{
				return CachedContainers;
			}
			RefreshContainerCache(player, now);
			return CachedContainers;
		}

		private static bool ContainerCacheIsValid(Player player, float now)
		{
			if ((Object)(object)_cachedPlayer != (Object)(object)player)
			{
				return false;
			}
			if (now - _containerCacheCreatedAt >= 0.5f)
			{
				return false;
			}
			Vector3 delta = ((Component)player).transform.position - _cachedPlayerPosition;
			if (delta.sqrMagnitude >= 1f)
			{
				return false;
			}
			if (SonicChestFiltersPlugin.ContainerRange == null)
			{
				return false;
			}
			if (!Mathf.Approximately(_cachedRange, SonicChestFiltersPlugin.ContainerRange.Value))
			{
				return false;
			}
			if (_cachedRequirePiece != SonicChestFiltersPlugin.RequirePlayerPlacedContainer.Value
				|| _cachedIgnoreMoving != SonicChestFiltersPlugin.IgnoreMovingContainers.Value
				|| _cachedIgnoreObliterators != SonicChestFiltersPlugin.IgnoreObliterators.Value
				|| _cachedBalrondCompatibility != SonicChestFiltersPlugin.BalrondCompatibility.Value)
			{
				return false;
			}
			return true;
		}

		private static void RefreshContainerCache(Player player, float now)
		{
			CachedContainers.Clear();
			ScanBuffer.Clear();
			PruneTraitCache();
			float range = Mathf.Max(0f, SonicChestFiltersPlugin.ContainerRange.Value);
			float rangeSq = range * range;
			Vector3 position = ((Component)player).transform.position;
			float started = SonicChestFiltersPlugin.DebugEnabled ? Time.realtimeSinceStartup : 0f;
			Container[] array = Object.FindObjectsByType<Container>((FindObjectsSortMode)0);
			foreach (Container container in array)
			{
				if ((Object)(object)container == (Object)null || !((Behaviour)container).isActiveAndEnabled)
				{
					continue;
				}
				Vector3 offset = ((Component)container).transform.position - position;
				float sqrMagnitude = offset.sqrMagnitude;
				if (sqrMagnitude > rangeSq)
				{
					continue;
				}
				ContainerTraits traits = GetTraits(container);
				if (SonicChestFiltersPlugin.IgnoreObliterators.Value && traits.IsIncinerator)
				{
					continue;
				}
				if (SonicChestFiltersPlugin.RequirePlayerPlacedContainer.Value && !traits.HasPiece)
				{
					continue;
				}
				if (ShouldIgnoreMovingContainer(container, traits))
				{
					continue;
				}
				try
				{
					if (container.GetInventory() == null)
					{
						continue;
					}
				}
				catch (Exception)
				{
					continue;
				}
				ScanBuffer.Add(new ContainerDistance(container, sqrMagnitude));
			}
			ScanBuffer.Sort((ContainerDistance a, ContainerDistance b) => a.DistanceSq.CompareTo(b.DistanceSq));
			for (int i = 0; i < ScanBuffer.Count; i++)
			{
				CachedContainers.Add(ScanBuffer[i].Container);
			}
			_cachedPlayer = player;
			_cachedPlayerPosition = position;
			_containerCacheCreatedAt = now;
			_cachedRange = SonicChestFiltersPlugin.ContainerRange.Value;
			_cachedRequirePiece = SonicChestFiltersPlugin.RequirePlayerPlacedContainer.Value;
			_cachedIgnoreMoving = SonicChestFiltersPlugin.IgnoreMovingContainers.Value;
			_cachedIgnoreObliterators = SonicChestFiltersPlugin.IgnoreObliterators.Value;
			_cachedBalrondCompatibility = SonicChestFiltersPlugin.BalrondCompatibility.Value;
			if (SonicChestFiltersPlugin.DebugEnabled)
			{
				float ms = (Time.realtimeSinceStartup - started) * 1000f;
				SonicChestFiltersPlugin.Debug($"Container cache refreshed: scanned={array.Length}, eligible={CachedContainers.Count}, time={ms:0.00}ms.");
			}
		}

		private static void PruneTraitCache()
		{
			if (TraitCache.Count == 0)
			{
				return;
			}

			List<int> deadKeys = null;
			foreach (KeyValuePair<int, ContainerTraits> pair in TraitCache)
			{
				if ((Object)(object)pair.Value.Container == (Object)null)
				{
					if (deadKeys == null)
					{
						deadKeys = new List<int>();
					}
					deadKeys.Add(pair.Key);
				}
			}

			if (deadKeys == null)
			{
				return;
			}

			for (int i = 0; i < deadKeys.Count; i++)
			{
				TraitCache.Remove(deadKeys[i]);
			}
		}

		private static bool ShouldIgnoreMovingContainer(Container container, ContainerTraits traits)
		{
			if (!traits.IsMoving || SonicChestFiltersPlugin.IgnoreMovingContainers == null || !SonicChestFiltersPlugin.IgnoreMovingContainers.Value)
			{
				return false;
			}

			try
			{
				Vagon cart = ((Component)container).GetComponentInParent<Vagon>(true);
				if ((Object)(object)cart != (Object)null)
				{
					if (cart.InUse() || cart.IsAttached())
					{
						return true;
					}
					return false;
				}
			}
			catch
			{
			}

			return true;
		}

		private static bool HasPlayerPlacedPiece(Container container)
		{
			try
			{
				if ((Object)(object)((Component)container).GetComponentInParent<Piece>() != (Object)null)
				{
					return true;
				}
				if (!SonicChestFiltersPlugin.BalrondCompatibility.Value)
				{
					return false;
				}
				Transform root = ((Component)container).transform.root;
				if ((Object)(object)root == (Object)null)
				{
					return false;
				}
				Piece[] pieces = ((Component)root).GetComponentsInChildren<Piece>(true);
				return pieces != null && pieces.Length != 0;
			}
			catch
			{
				return false;
			}
		}

		private static ContainerTraits GetTraits(Container container)
		{
			int instanceID = ((Object)container).GetInstanceID();
			if (TraitCache.TryGetValue(instanceID, out ContainerTraits value)
				&& value.Container == container
				&& (value.HasPiece || !SonicChestFiltersPlugin.BalrondCompatibility.Value))
			{
				return value;
			}
			bool hasPiece = HasPlayerPlacedPiece(container);
			bool isMoving = false;
			bool isIncinerator = false;
			try
			{
				if ((Object)(object)((Component)container).GetComponentInParent<Incinerator>(true) != (Object)null)
				{
					isIncinerator = true;
				}
			}
			catch
			{
			}
			try
			{
				Component[] parents = ((Component)container).GetComponentsInParent<Component>(true);
				for (int i = 0; i < parents.Length; i++)
				{
					string typeName = ((object)parents[i]).GetType().Name;
					switch (typeName)
					{
						case "Ship":
						case "Vagon":
						case "Wagon":
							isMoving = true;
							break;
						case "Incinerator":
							isIncinerator = true;
							break;
					}
					if (isMoving && isIncinerator)
					{
						break;
					}
				}
			}
			catch
			{
			}
			ContainerTraits traits = new ContainerTraits(container, hasPiece, isMoving, isIncinerator);
			TraitCache[instanceID] = traits;
			return traits;
		}
	}
}
