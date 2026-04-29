namespace AdminDasboard.Application.Assets;

public interface IAssetSearchService
{
    Task<IReadOnlyList<AssetSearchResultResponse>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);
}
