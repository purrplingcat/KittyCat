using System.Globalization;
using System.Resources;

namespace PurrplingCore.Toolkit.Localization;

/// <summary>Provides culture selection and resource availability checks independent of a game.</summary>
public static class CultureUtilities
{
    /// <summary>Sets the current thread's formatting and resource lookup cultures.</summary>
    public static void SetCulture(string cultureCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(cultureCode);
        var culture = new CultureInfo(cultureCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>Returns cultures with a resource set in the supplied manager, including invariant culture.</summary>
    public static List<CultureInfo> GetSupportedCultures(ResourceManager resourceManager)
    {
        ArgumentNullException.ThrowIfNull(resourceManager);
        var supportedCultures = new List<CultureInfo>();
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                if (resourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false) != null)
                {
                    supportedCultures.Add(culture);
                }
            }
            catch (MissingManifestResourceException)
            {
                // Resource managers without an invariant set cannot resolve this culture.
            }
        }

        supportedCultures.Add(CultureInfo.InvariantCulture);
        return supportedCultures;
    }
}