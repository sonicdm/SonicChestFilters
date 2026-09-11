using Jotunn.Entities;
using Jotunn.Managers;

namespace SonicChestFilters
{
	internal static class ModLocalization
	{
		internal const string FilterPlaceholder = "$sonic_chestfilters_filter";

		internal const string Clear = "$sonic_chestfilters_clear";

		internal const string Sort = "$sonic_chestfilters_sort";

		internal static void Register()
		{
			if (LocalizationManager.Instance == null)
			{
				return;
			}

			CustomLocalization localization = LocalizationManager.Instance.GetLocalization();
			localization.AddTranslation(FilterPlaceholder, "Filter...");
			localization.AddTranslation(Clear, "Clear");
			localization.AddTranslation(Sort, "Sort");
		}

		internal static string Translate(string token, string fallback)
		{
			if (LocalizationManager.Instance != null)
			{
				string value = LocalizationManager.Instance.TryTranslate(token);
				if (!string.IsNullOrEmpty(value) && value[0] != '[' && value != token)
				{
					return value;
				}
			}

			if (Localization.instance != null)
			{
				string value = Localization.instance.Localize(token);
				if (!string.IsNullOrEmpty(value) && value != token)
				{
					return value;
				}
			}

			return fallback;
		}
	}
}
