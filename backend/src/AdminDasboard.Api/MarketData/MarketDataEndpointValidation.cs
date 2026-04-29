using System.Text.RegularExpressions;

namespace AdminDasboard.Api.MarketData;

public static partial class MarketDataEndpointValidation
{
    private static readonly string[] AllowedCurrencies = ["eur", "usd"];
    private static readonly int[] AllowedRanges = [1, 7, 30, 90, 365];

    public static bool TryValidate(
        string coinId,
        string? currency,
        int days,
        out string normalizedCurrency,
        out Dictionary<string, string[]> errors)
    {
        normalizedCurrency = (currency ?? "eur").Trim().ToLowerInvariant();
        errors = [];

        if (string.IsNullOrWhiteSpace(coinId) || !IsValidCoinId(coinId))
        {
            errors["coinId"] = ["Use a CoinGecko coin id with lowercase letters, numbers, and hyphens."];
        }

        if (!IsAllowedCurrency(normalizedCurrency))
        {
            errors["currency"] = [$"Allowed values: {string.Join(", ", AllowedCurrencies)}."];
        }

        if (!IsAllowedRange(days))
        {
            errors["days"] = [$"Allowed values: {string.Join(", ", AllowedRanges)}."];
        }

        return errors.Count == 0;
    }

    public static bool IsValidCoinId(string coinId)
    {
        return CoinIdRegex().IsMatch(coinId);
    }

    public static bool IsAllowedCurrency(string currency)
    {
        return AllowedCurrencies.Contains(currency, StringComparer.Ordinal);
    }

    public static bool IsAllowedRange(int days)
    {
        return AllowedRanges.Contains(days);
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,99}$")]
    private static partial Regex CoinIdRegex();
}
