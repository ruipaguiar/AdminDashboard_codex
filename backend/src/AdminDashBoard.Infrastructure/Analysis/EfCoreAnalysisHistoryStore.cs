using System.Text.Json;
using AdminDashBoard.Application.Analysis;
using AdminDashBoard.Domain.Analysis;
using AdminDashBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminDashBoard.Infrastructure.Analysis;

public sealed class EfCoreAnalysisHistoryStore : IAnalysisHistoryStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;

    public EfCoreAnalysisHistoryStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(
        string coinId,
        string currency,
        int days,
        string model,
        AnalysisResponse analysis,
        CancellationToken cancellationToken)
    {
        _dbContext.AnalysisRecords.Add(new AnalysisRecord
        {
            Id = Guid.NewGuid(),
            CoinId = coinId,
            Currency = currency,
            Days = days,
            Model = model,
            RiskLevel = analysis.RiskLevel,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ResponseJson = JsonSerializer.Serialize(analysis, SerializerOptions)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnalysisHistoryItemResponse>> ListAsync(
        string coinId,
        string currency,
        int days,
        int limit,
        CancellationToken cancellationToken)
    {
        var records = await _dbContext.AnalysisRecords
            .AsNoTracking()
            .Where(record =>
                record.CoinId == coinId &&
                record.Currency == currency &&
                record.Days == days)
            .OrderByDescending(record => record.CreatedAtUtc)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        return records
            .Select(record => new AnalysisHistoryItemResponse(
                record.Id,
                record.CoinId,
                record.Currency,
                record.Days,
                record.RiskLevel,
                record.CreatedAtUtc,
                JsonSerializer.Deserialize<AnalysisResponse>(record.ResponseJson, SerializerOptions)!))
            .ToArray();
    }

    public async Task<AnalysisHistoryListResponse> ListAsync(
        string? coinId,
        string? currency,
        int? days,
        string? riskLevel,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AnalysisRecords.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(coinId))
        {
            query = query.Where(record => record.CoinId == coinId);
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            query = query.Where(record => record.Currency == currency);
        }

        if (days is not null)
        {
            query = query.Where(record => record.Days == days);
        }

        if (!string.IsNullOrWhiteSpace(riskLevel))
        {
            query = query.Where(record => record.RiskLevel == riskLevel);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(record => record.CreatedAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        var items = records
            .Select(record => new AnalysisHistoryItemResponse(
                record.Id,
                record.CoinId,
                record.Currency,
                record.Days,
                record.RiskLevel,
                record.CreatedAtUtc,
                JsonSerializer.Deserialize<AnalysisResponse>(record.ResponseJson, SerializerOptions)!))
            .ToArray();

        return new AnalysisHistoryListResponse(items, totalCount, offset, limit);
    }
}
