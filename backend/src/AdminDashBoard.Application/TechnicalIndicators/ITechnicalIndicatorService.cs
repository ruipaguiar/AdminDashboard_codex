using AdminDashBoard.Application.MarketData;

namespace AdminDashBoard.Application.TechnicalIndicators;

public interface ITechnicalIndicatorService
{
    TechnicalIndicatorsResponse Calculate(CryptoMarketDataResponse marketData);
}
