using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminDashBoard.Application.Analysis;
using AdminDashBoard.Application.MarketData;
using AdminDashBoard.Application.TechnicalIndicators;
using Microsoft.Extensions.Options;

namespace AdminDashBoard.Infrastructure.Analysis;

public sealed class OpenAiAnalysisService : IAnalysisService
{
    private const string Disclaimer =
        "Isto \u00e9 uma an\u00e1lise automatizada com base em dados hist\u00f3ricos e indicadores t\u00e9cnicos. N\u00e3o constitui aconselhamento financeiro.";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ICryptoMarketDataService _marketDataService;
    private readonly ITechnicalIndicatorService _technicalIndicatorService;
    private readonly IAnalysisHistoryStore _analysisHistoryStore;

    public OpenAiAnalysisService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ICryptoMarketDataService marketDataService,
        ITechnicalIndicatorService technicalIndicatorService,
        IAnalysisHistoryStore analysisHistoryStore)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _marketDataService = marketDataService;
        _technicalIndicatorService = technicalIndicatorService;
        _analysisHistoryStore = analysisHistoryStore;
    }

    public async Task<AnalysisResponse> AnalyzeAsync(
        string coinId,
        string currency,
        int days,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new AnalysisConfigurationException("OpenAI API key is not configured.");
        }

        var marketData = await _marketDataService.GetAsync(coinId, currency, days, cancellationToken);
        var indicators = _technicalIndicatorService.Calculate(marketData);
        var promptPayload = BuildPromptPayload(marketData, indicators);

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new OpenAiResponsesRequest(
            _options.Model,
            [
                new OpenAiInputMessage("system", [new OpenAiInputContent("input_text", BuildSystemPrompt())]),
                new OpenAiInputMessage("user", [new OpenAiInputContent(
                    "input_text",
                    JsonSerializer.Serialize(promptPayload, SerializerOptions))])
            ],
            _options.MaxOutputTokens,
            new OpenAiTextOptions(new OpenAiTextFormat(
                "json_schema",
                "crypto_analysis",
                true,
                BuildAnalysisSchema()))));

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new AnalysisConfigurationException("OpenAI API key was rejected.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new AnalysisProviderException("AI analysis provider is temporarily unavailable.");
        }

        var content = await response.Content.ReadFromJsonAsync<OpenAiResponsesResponse>(
            SerializerOptions,
            cancellationToken);

        var text = ExtractText(content);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new AnalysisProviderException("AI analysis provider returned an empty response.");
        }

        var analysis = JsonSerializer.Deserialize<AnalysisResponse>(text, SerializerOptions)
            ?? throw new AnalysisProviderException("AI analysis provider returned invalid JSON.");

        var finalAnalysis = analysis with { Disclaimer = Disclaimer };

        await _analysisHistoryStore.SaveAsync(
            coinId,
            currency,
            days,
            _options.Model,
            finalAnalysis,
            cancellationToken);

        return finalAnalysis;
    }

    private static object BuildPromptPayload(
        CryptoMarketDataResponse marketData,
        TechnicalIndicatorsResponse indicators)
    {
        return new
        {
            coin = new
            {
                marketData.CoinId,
                marketData.Symbol,
                marketData.Name,
                marketData.Currency,
                marketData.Days
            },
            currentMarketData = marketData.Current,
            historicalPrices = marketData.History
                .Select(point => new
                {
                    point.Timestamp,
                    point.Price,
                    point.MarketCap,
                    point.TotalVolume
                })
                .ToArray(),
            calculatedIndicators = new
            {
                indicators.Summary,
                Sma20 = indicators.Sma20.TakeLast(20),
                Ema20 = indicators.Ema20.TakeLast(20),
                Rsi14 = indicators.Rsi14.TakeLast(20)
            },
            summarizedChartContext = new
            {
                FirstPrice = marketData.History.FirstOrDefault()?.Price,
                LastPrice = marketData.History.LastOrDefault()?.Price,
                HighestPrice = marketData.History.Count == 0
                    ? (decimal?)null
                    : marketData.History.Max(point => point.Price),
                LowestPrice = marketData.History.Count == 0
                    ? (decimal?)null
                    : marketData.History.Min(point => point.Price)
            }
        };
    }

    private static string BuildSystemPrompt()
    {
        return """
You analyze cryptocurrency market data for an admin dashboard.
Use only the structured data supplied by the user.
Do not invent prices. Do not make guarantees. Do not provide financial advice.
Return concise Portuguese text fields suitable for an operational dashboard.
Support, resistance, and target arrays must contain numeric price levels only.
Risk level must be one of: low, medium, high.
""";
    }

    private static object BuildAnalysisSchema()
    {
        return new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                summary = new { type = "string" },
                trend = new { type = "string" },
                rsiComment = new { type = "string" },
                supportLevels = new
                {
                    type = "array",
                    items = new { type = "number" }
                },
                resistanceLevels = new
                {
                    type = "array",
                    items = new { type = "number" }
                },
                possibleEntryZone = new { type = "string" },
                stopLoss = new { type = "string" },
                takeProfitTargets = new
                {
                    type = "array",
                    items = new { type = "number" }
                },
                riskLevel = new
                {
                    type = "string",
                    @enum = new[] { "low", "medium", "high" }
                },
                disclaimer = new { type = "string" }
            },
            required = new[]
            {
                "summary",
                "trend",
                "rsiComment",
                "supportLevels",
                "resistanceLevels",
                "possibleEntryZone",
                "stopLoss",
                "takeProfitTargets",
                "riskLevel",
                "disclaimer"
            }
        };
    }

    private static string? ExtractText(OpenAiResponsesResponse? response)
    {
        return response?.Output
            .SelectMany(item => item.Content)
            .FirstOrDefault(content => content.Type == "output_text")?
            .Text;
    }

    private sealed record OpenAiResponsesRequest(
        string Model,
        IReadOnlyList<OpenAiInputMessage> Input,
        [property: JsonPropertyName("max_output_tokens")] int MaxOutputTokens,
        OpenAiTextOptions Text);

    private sealed record OpenAiInputMessage(
        string Role,
        IReadOnlyList<OpenAiInputContent> Content);

    private sealed record OpenAiInputContent(string Type, string Text);

    private sealed record OpenAiTextOptions(OpenAiTextFormat Format);

    private sealed record OpenAiTextFormat(
        string Type,
        string Name,
        bool Strict,
        object Schema);

    private sealed record OpenAiResponsesResponse(
        IReadOnlyList<OpenAiOutputItem> Output);

    private sealed record OpenAiOutputItem(
        IReadOnlyList<OpenAiOutputContent> Content);

    private sealed record OpenAiOutputContent(string Type, string? Text);
}
