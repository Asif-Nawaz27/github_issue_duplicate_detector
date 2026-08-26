namespace IssueSense.Infrastructure.Tests.GitHub;

/// <summary>Returns one canned response per call, in order, so tests can script multi-page requests.</summary>
internal sealed class FakeHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
{
    private int _callCount;

    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (_callCount >= responses.Length)
            throw new InvalidOperationException("FakeHttpMessageHandler received more requests than scripted responses.");

        return Task.FromResult(responses[_callCount++]);
    }
}
