using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SonicChestFilters
{
	internal static class ItemMatch
	{
		internal static bool NameMatches(string value, string pattern)
		{
			if (string.IsNullOrEmpty(pattern))
			{
				return false;
			}
			if (pattern.IndexOf('*') < 0)
			{
				return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);
			}

			string[] parts = pattern.Split('*');
			int index = 0;
			for (int i = 0; i < parts.Length; i++)
			{
				string part = parts[i];
				if (part.Length == 0)
				{
					continue;
				}
				if (i == 0)
				{
					if (!value.StartsWith(part, StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
					index = part.Length;
					continue;
				}
				bool isLast = i == parts.Length - 1;
				if (isLast && pattern[pattern.Length - 1] != '*')
				{
					if (!value.EndsWith(part, StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}
					return value.Length - part.Length >= index;
				}
				int found = value.IndexOf(part, index, StringComparison.OrdinalIgnoreCase);
				if (found < 0)
				{
					return false;
				}
				index = found + part.Length;
			}
			return true;
		}

		internal static bool LocateNameMatches(string value, string pattern)
		{
			if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
			{
				return false;
			}

			if (NameMatches(value, pattern))
			{
				return true;
			}

			string needle = pattern.Replace("*", string.Empty).Trim();
			if (needle.Length == 0)
			{
				return false;
			}

			return value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		internal static bool ItemMatchesPattern(ItemDrop.ItemData item, string pattern, out string matchedLabel)
		{
			matchedLabel = string.Empty;
			if (item?.m_shared == null || string.IsNullOrWhiteSpace(pattern))
			{
				return false;
			}

			string sharedName = item.m_shared.m_name;
			string localized = GetLocalizedName(sharedName);

			if (!string.IsNullOrEmpty(localized) && LocateNameMatches(localized, pattern))
			{
				matchedLabel = localized;
				return true;
			}

			if (!string.IsNullOrEmpty(sharedName) && LocateNameMatches(sharedName, pattern))
			{
				matchedLabel = !string.IsNullOrEmpty(localized) ? localized : FormatDisplayName(sharedName);
				return true;
			}

			if (!string.IsNullOrEmpty(sharedName) && sharedName[0] == '$')
			{
				string token = sharedName.Substring(1);
				if (LocateNameMatches(token, pattern))
				{
					matchedLabel = !string.IsNullOrEmpty(localized) ? localized : FormatDisplayName(sharedName);
					return true;
				}
			}

			if ((Object)(object)item.m_dropPrefab != (Object)null)
			{
				string prefabName = ((Object)item.m_dropPrefab).name;
				if (!string.IsNullOrEmpty(prefabName) && LocateNameMatches(prefabName, pattern))
				{
					matchedLabel = !string.IsNullOrEmpty(localized) ? localized : prefabName;
					return true;
				}
			}

			return false;
		}

		internal static string GetLocalizedName(string sharedName)
		{
			if (string.IsNullOrEmpty(sharedName))
			{
				return sharedName;
			}

			EnsureLocalizeResolver();
			if (_localizeName == null)
			{
				return FormatDisplayName(sharedName);
			}

			try
			{
				string localized = _localizeName(sharedName);
				if (!string.IsNullOrEmpty(localized) && localized[0] != '$')
				{
					return localized;
				}
			}
			catch
			{
			}

			return FormatDisplayName(sharedName);
		}

		internal static string FormatDisplayName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return name;
			}
			if (name.StartsWith("$item_", StringComparison.OrdinalIgnoreCase) && name.Length > 6)
			{
				string token = name.Substring(6);
				if (token.Length == 0)
				{
					return name;
				}
				return char.ToUpperInvariant(token[0]) + token.Substring(1);
			}
			if (name.StartsWith("$", StringComparison.Ordinal) && name.Length > 1)
			{
				return name.Substring(1);
			}
			return name;
		}

		private static Func<string, string> _localizeName;

		private static bool _localizeResolved;

		private static void EnsureLocalizeResolver()
		{
			if (_localizeResolved)
			{
				return;
			}

			_localizeResolved = true;
			try
			{
				Type locType = AccessTools.TypeByName("Localization");
				if (locType == null)
				{
					return;
				}

				MethodInfo getInstance = AccessTools.PropertyGetter(locType, "instance");
				MethodInfo localize = AccessTools.Method(locType, "Localize", new Type[] { typeof(string) });
				if (getInstance == null || localize == null)
				{
					localize = AccessTools.Method(locType, "Translate", new Type[] { typeof(string) });
				}

				if (getInstance == null || localize == null)
				{
					return;
				}

				_localizeName = (string token) =>
				{
					object instance = getInstance.Invoke(null, null);
					if (instance == null)
					{
						return token;
					}

					object result = localize.Invoke(instance, new object[] { token });
					return result as string ?? token;
				};
			}
			catch (Exception ex)
			{
				SonicChestFiltersPlugin.Debug("Localization resolver failed: " + ex.Message);
			}
		}
	}
}
