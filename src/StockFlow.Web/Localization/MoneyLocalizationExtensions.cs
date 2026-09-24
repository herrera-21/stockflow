using Microsoft.Extensions.Localization;

namespace StockFlow.Web.Localization;

/// <summary>
/// Helpers to display monetary amounts with the localized currency symbol. The symbol is only a UI
/// hint; amounts are stored without it.
/// </summary>
public static class MoneyLocalizationExtensions
{
    /// <summary>
    /// Formats an amount with the currency symbol and two decimals (for example "$2.00").
    /// </summary>
    /// <param name="localizer">Localizer used to look up the currency symbol.</param>
    /// <param name="amount">Amount to format.</param>
    /// <returns>The formatted amount.</returns>
    public static string FormatMoney(this IStringLocalizer<SharedResource> localizer, decimal amount) =>
        $"{localizer["CurrencySymbol"].Value}{amount:N2}";
}
