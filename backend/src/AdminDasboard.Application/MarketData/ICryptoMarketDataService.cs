namespace AdminDasboard.Application.MarketData;

public interface ICryptoMarketDataService
{
    Task<CryptoMarketDataResponse> GetAsync(
        string coinId,
        string currency,
        int days,
        CancellationToken cancellationToken);
}
