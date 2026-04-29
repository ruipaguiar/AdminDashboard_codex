using AdminDasboard.Application.MarketData;

namespace AdminDasboard.Application.TechnicalIndicators;

public interface ITechnicalIndicatorService
{
    TechnicalIndicatorsResponse Calculate(CryptoMarketDataResponse marketData);
}
