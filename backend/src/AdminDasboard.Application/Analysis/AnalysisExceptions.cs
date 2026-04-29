namespace AdminDasboard.Application.Analysis;

public sealed class AnalysisConfigurationException : Exception
{
    public AnalysisConfigurationException(string message)
        : base(message)
    {
    }
}

public sealed class AnalysisProviderException : Exception
{
    public AnalysisProviderException(string message)
        : base(message)
    {
    }
}
