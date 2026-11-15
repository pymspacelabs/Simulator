using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Simulator.Localization;

public interface ILocalizer<out T>
{
    LocalizedString this[string name] { get; }
    LocalizedString this[string name, params object[] arguments] { get; }
}

public sealed class Localizer<T>(IStringLocalizerFactory factory) : ILocalizer<T>
{
    private readonly IStringLocalizer _localizer =
        factory.Create("Simulator.Resources.Localization.Strings", typeof(T).Assembly.FullName!);
    public LocalizedString this[string name] => _localizer[name];
    public LocalizedString this[string name, params object[] arguments] => _localizer[name, arguments];
}

public sealed class LocalizationState
{
    public event Action? CultureChanged;
    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("en-US");

    public LocalizationState()
    {
        CultureInfo.CurrentCulture = CurrentCulture;
        CultureInfo.CurrentUICulture = CurrentCulture;
    }

    public void SetCulture(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        if (Equals(CurrentCulture, culture)) return;
        CurrentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureChanged?.Invoke();
    }
}
