namespace AdminDasboard.Application.Assets;

public sealed record AssetSearchResultResponse(
    string Id,
    string Symbol,
    string Name,
    int? MarketCapRank,
    string? Thumb);
