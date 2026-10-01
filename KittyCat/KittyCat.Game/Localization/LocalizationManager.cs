using System.Collections.Generic;
using System.Globalization;
using KittyCat.Core.Localization;
using PurrplingCore.Toolkit.Localization;

namespace KittyCat.Localization;

/// <summary>
/// Selects KittyCat's default culture and resource catalog.
/// </summary>
internal static class LocalizationManager
{
    public const string DEFAULT_CULTURE_CODE = "en-EN";

    public static List<CultureInfo> GetSupportedCultures() =>
        CultureUtilities.GetSupportedCultures(Resources.ResourceManager);

    public static void SetCulture(string cultureCode) => CultureUtilities.SetCulture(cultureCode);
}