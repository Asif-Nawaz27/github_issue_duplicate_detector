using System.Net;
using System.Text;
using IssueSense.Application.GitHub;
using IssueSense.Application.GitHub.Models;
using IssueSense.Domain.Enums;
using IssueSense.Infrastructure.GitHub;

namespace IssueSense.Infrastructure.Tests.GitHub;

public class GitHubServiceTests
{
    private static (GitHubService Service, FakeHttpMessageHandler Handler) CreateService(params HttpResponseMessage[] responses)
    {
        var fakeHandler = new FakeHttpMessageHandler(responses);
        var rateLimitHandler = new GitHubRateLimitDelegatingHandler { InnerHandler = fakeHandler };
        var httpClient = new HttpClient(rateLimitHandler) { BaseAddress = new Uri("https://api.github.com/") };

        return (new GitHubService(httpClient), fakeHandler);
    }

    private static HttpResponseMessage JsonResponse(
        HttpStatusCode statusCode,
        string json,
        IEnumerable<KeyValuePair<string, string>>? headers = null)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (headers is not null)
        {
            foreach (var header in headers)
                response.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return response;
    }

    [Fact]
    public async Task GetRepositoryAsync_WithSuccessResponse_MapsFields()
    {
        const string json = """
            {
                "id": 123,
                "name": "hello-world",
                "html_url": "https://github.com/octocat/hello-world",
                "owner": { "login": "octocat" }
            }
            """;
        var (service, handler) = CreateService(JsonResponse(HttpStatusCode.OK, json));

        var result = await service.GetRepositoryAsync("octocat", "hello-world");

        Assert.Equal(123, result.Id);
        Assert.Equal("octocat", result.Owner);
        Assert.Equal("hello-world", result.Name);
        Assert.Equal("https://github.com/octocat/hello-world", result.HtmlUrl);
        Assert.Equal("/repos/octocat/hello-world", handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetRepositoryAsync_WithNotFoundResponse_ThrowsGitHubNotFoundException()
    {
        var (service, _) = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<GitHubNotFoundException>(() => service.GetRepositoryAsync("octocat", "missing"));
    }

    [Fact]
    public async Task GetIssueAsync_WithSuccessResponse_MapsFieldsIncludingLabels()
    {
        const string json = """
            {
                "id": 1,
                "number": 42,
                "title": "App crashes on startup",
                "body": "Steps to reproduce...",
                "state": "open",
                "html_url": "https://github.com/octocat/hello-world/issues/42",
                "created_at": "2026-01-01T00:00:00Z",
                "updated_at": "2026-01-02T00:00:00Z",
                "closed_at": null,
                "labels": [ { "name": "bug", "color": "d73a4a" } ]
            }
            """;
        var (service, _) = CreateService(JsonResponse(HttpStatusCode.OK, json));

        var result = await service.GetIssueAsync("octocat", "hello-world", 42);

        Assert.Equal(42, result.Number);
        Assert.Equal("App crashes on startup", result.Title);
        Assert.Equal(IssueState.Open, result.State);
        Assert.Null(result.ClosedAt);
        Assert.Equal(["bug"], result.Labels);
    }

    [Fact]
    public async Task GetIssueAsync_WithClosedState_MapsToClosed()
    {
        const string json = """
            {
                "id": 1, "number": 1, "title": "t", "state": "closed",
                "html_url": "https://github.com/o/r/issues/1",
                "created_at": "2026-01-01T00:00:00Z", "updated_at": "2026-01-01T00:00:00Z",
                "closed_at": "2026-01-03T00:00:00Z", "labels": []
            }
            """;
        var (service, _) = CreateService(JsonResponse(HttpStatusCode.OK, json));

        var result = await service.GetIssueAsync("o", "r", 1);

        Assert.Equal(IssueState.Closed, result.State);
        Assert.NotNull(result.ClosedAt);
    }

    [Fact]
    public async Task GetIssueAsync_WithNotFoundResponse_ThrowsGitHubNotFoundException()
    {
        var (service, _) = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<GitHubNotFoundException>(() => service.GetIssueAsync("o", "r", 999));
    }

    [Fact]
    public async Task GetIssuesAsync_FollowsPaginationAndFiltersPullRequests()
    {
        const string page1 = """
            [
                { "id": 1, "number": 1, "title": "Issue one", "state": "open", "html_url": "u1",
                  "created_at": "2026-01-01T00:00:00Z", "updated_at": "2026-01-01T00:00:00Z", "labels": [] },
                { "id": 2, "number": 2, "title": "A pull request", "state": "open", "html_url": "u2",
                  "created_at": "2026-01-01T00:00:00Z", "updated_at": "2026-01-01T00:00:00Z", "labels": [],
                  "pull_request": { "url": "https://api.github.com/repos/o/r/pulls/2" } }
            ]
            """;
        const string page2 = """
            [
                { "id": 3, "number": 3, "title": "Issue three", "state": "closed", "html_url": "u3",
                  "created_at": "2026-01-01T00:00:00Z", "updated_at": "2026-01-01T00:00:00Z", "labels": [] }
            ]
            """;

        var firstResponse = JsonResponse(HttpStatusCode.OK, page1,
        [
            new("Link", "<https://api.github.com/repos/o/r/issues?state=all&per_page=100&page=2>; rel=\"next\"")
        ]);
        var secondResponse = JsonResponse(HttpStatusCode.OK, page2);

        var (service, handler) = CreateService(firstResponse, secondResponse);

        var results = new List<GitHubIssueInfo>();
        await foreach (var issue in service.GetIssuesAsync("o", "r"))
            results.Add(issue);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, i => i.Number == 2);
        Assert.Contains(results, i => i.Number == 1);
        Assert.Contains(results, i => i.Number == 3);
    }

    [Fact]
    public async Task GetIssueLabelsAsync_WithSuccessResponse_MapsNameAndColor()
    {
        const string json = """
            [
                { "name": "bug", "color": "d73a4a" },
                { "name": "duplicate", "color": "cfd3d7" }
            ]
            """;
        var (service, _) = CreateService(JsonResponse(HttpStatusCode.OK, json));

        var result = await service.GetIssueLabelsAsync("o", "r", 1);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, l => l.Name == "bug" && l.Color == "d73a4a");
        Assert.Contains(result, l => l.Name == "duplicate" && l.Color == "cfd3d7");
    }

    [Fact]
    public async Task GetRepositoryAsync_WithRateLimitExceededResponse_ThrowsGitHubRateLimitExceededException()
    {
        var resetAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.TryAddWithoutValidation("X-RateLimit-Remaining", "0");
        response.Headers.TryAddWithoutValidation(
            "X-RateLimit-Reset",
            resetAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));

        var (service, _) = CreateService(response);

        var exception = await Assert.ThrowsAsync<GitHubRateLimitExceededException>(() => service.GetRepositoryAsync("o", "r"));

        Assert.Equal(resetAt.ToUnixTimeSeconds(), exception.ResetAt.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetRepositoryAsync_WithSecondaryRateLimit_UsesRetryAfterHeader()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.TryAddWithoutValidation("Retry-After", "60");

        var (service, _) = CreateService(response);

        var exception = await Assert.ThrowsAsync<GitHubRateLimitExceededException>(() => service.GetRepositoryAsync("o", "r"));

        Assert.True(exception.ResetAt > DateTimeOffset.UtcNow.AddSeconds(50));
    }

    [Fact]
    public async Task GetRepositoryAsync_WithForbiddenButNoRateLimitHeaders_ThrowsGitHubForbiddenException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);

        var (service, _) = CreateService(response);

        await Assert.ThrowsAsync<GitHubForbiddenException>(() => service.GetRepositoryAsync("o", "r"));
    }

    [Fact]
    public async Task GetIssueCommentsAsync_WithSuccessResponse_MapsFields()
    {
        const string json = """
            [
                { "id": 1, "body": "First comment", "user": { "login": "octocat" } },
                { "id": 2, "body": "Second comment", "user": { "login": "issuesense-bot" } }
            ]
            """;
        var (service, _) = CreateService(JsonResponse(HttpStatusCode.OK, json));

        var result = await service.GetIssueCommentsAsync("o", "r", 1);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Id == 1 && c.Body == "First comment" && c.AuthorLogin == "octocat");
        Assert.Contains(result, c => c.Id == 2 && c.AuthorLogin == "issuesense-bot");
    }

    [Fact]
    public async Task GetIssueCommentsAsync_WithNotFoundResponse_ThrowsGitHubNotFoundException()
    {
        var (service, _) = CreateService(new HttpResponseMessage(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<GitHubNotFoundException>(() => service.GetIssueCommentsAsync("o", "r", 1));
    }

    [Fact]
    public async Task PostIssueCommentAsync_WithSuccessResponse_ReturnsCreatedComment()
    {
        const string json = """{ "id": 42, "body": "posted body", "user": { "login": "issuesense-bot" } }""";
        var (service, handler) = CreateService(JsonResponse(HttpStatusCode.Created, json));

        var result = await service.PostIssueCommentAsync("o", "r", 1, "posted body");

        Assert.Equal(42, result.Id);
        Assert.Equal("posted body", result.Body);
        Assert.Equal("POST", handler.Requests[0].Method.Method);
        Assert.Equal("/repos/o/r/issues/1/comments", handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task PostIssueCommentAsync_WithForbiddenResponse_ThrowsGitHubForbiddenException()
    {
        var (service, _) = CreateService(new HttpResponseMessage(HttpStatusCode.Forbidden));

        await Assert.ThrowsAsync<GitHubForbiddenException>(() => service.PostIssueCommentAsync("o", "r", 1, "body"));
    }

    [Fact]
    public async Task PostIssueCommentAsync_WithServerError_ThrowsHttpRequestException()
    {
        var (service, _) = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.PostIssueCommentAsync("o", "r", 1, "body"));
    }
}
