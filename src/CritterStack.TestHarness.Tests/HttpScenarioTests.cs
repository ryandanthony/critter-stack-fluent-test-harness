// -----------------------------------------------------------------------
// <copyright file="HttpScenarioTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using Microsoft.Extensions.Hosting;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// <c>WhenHttp</c> against Wolverine.Http endpoints on the host with no async daemon.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class HttpScenarioTests(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Action<HarnessOptions> TenantByHeader =>
        o => o.HttpTenant = (x, tenant) => x.WithRequestHeader(HarnessHostFixture.TenantHeader, tenant);

    [Fact]
    [Trait("business_value", "An HTTP endpoint test reads as Given/When/Then: the request's appended events and status are asserted like a command's.")]
    public async Task Post_appends_events_and_answers_2xx()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new RenameWidget(id, "Gear")).ToUrl("/widgets/rename"))
            .ThenStatusCode(204)
            .ThenAppended<WidgetRenamed>(id, e => Is(e.Name, "Gear"))
            .ThenAppendedCount(id, 1)
            .ThenStreamEventCount(id, 2)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "The JSON body of the response can be asserted with a typed predicate.")]
    public async Task Response_body_is_read_as_json()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new PublishWidget(id)).ToUrl("/widgets/publish"))
            .ThenResponse<WidgetReceipt>(r => r.Id == id && Is(r.Status, "published"))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Messages an endpoint cascades are tracked, so ThenPublished works after WhenHttp as after When.")]
    public async Task Cascaded_messages_are_tracked()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new PublishWidget(id)).ToUrl("/widgets/publish"))
            .ThenPublished<WidgetPublishedNotice>(n => n.Id == id)
            .ThenAppended<WidgetPublished>(id)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A non-2xx status fails the WhenHttp at once with the status and body, unless the test expects it.")]
    public async Task Non_success_status_fails_unless_expected()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .WhenHttp(x => x.Get.Url($"/widgets/{Guid.NewGuid()}")));

        Assert.StartsWith("step 1 WhenHttp(GET /widgets/", failure.Message, StringComparison.Ordinal);
        Assert.Contains("expected a 2xx status, got 404. To expect it, put ThenStatusCode(404) after this step.", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "Error responses are first-class: ThenStatusCode accepts the expected non-2xx status.")]
    public async Task Expected_error_status_passes()
    {
        await TestHarness.Setup(fixture.Host)
            .WhenHttp(x => x.Get.Url($"/widgets/{Guid.NewGuid()}"))
            .ThenStatusCode(404)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A wrong status names the expected and actual status and shows the body.")]
    public async Task Wrong_status_is_reported()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new PublishWidget(id)).ToUrl("/widgets/publish"))
            .ThenStatusCode(201));

        Assert.StartsWith("step 3 ThenStatusCode(201): expected status 201 from step 2 WhenHttp(POST /widgets/publish), got 200.", failure.Message, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"published\"", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "An exception thrown by an endpoint is behaviour under test and can be claimed with ThenThrows.")]
    public async Task Endpoint_exception_is_claimable()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new FailWidget(id)).ToUrl("/widgets/fail"))
            .ThenThrows<WidgetException>(e => e.Message.Contains("over HTTP", StringComparison.Ordinal))
            .ThenNotAppended<WidgetRenamed>(id)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "An unclaimed endpoint exception fails the next step with the real cause attached, not a misleading assertion.")]
    public async Task Unclaimed_endpoint_exception_fails_the_next_step()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new FailWidget(id)).ToUrl("/widgets/fail"))
            .ThenStatusCode(500));

        Assert.StartsWith("step 3 ThenStatusCode(500): step 2 WhenHttp(POST /widgets/fail) threw WidgetException", failure.Message, StringComparison.Ordinal);
        Assert.IsType<WidgetException>(failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "Asserting on the body after the endpoint threw says so, instead of a deserialization error.")]
    public async Task Response_after_claimed_exception_says_there_is_no_response()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new FailWidget(id)).ToUrl("/widgets/fail"))
            .ThenThrows<WidgetException>()
            .ThenResponse<WidgetReceipt>(_ => true));

        Assert.StartsWith("step 4 ThenResponse<WidgetReceipt>: step 2 WhenHttp(POST /widgets/fail) threw WidgetException, so there is no response", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A body that doesn't match the predicate is shown in full in the failure.")]
    public async Task Response_predicate_failure_shows_the_body()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new PublishWidget(id)).ToUrl("/widgets/publish"))
            .ThenResponse<WidgetReceipt>(r => Is(r.Status, "archived")));

        Assert.StartsWith("step 3 ThenResponse<WidgetReceipt>: the body of step 2 WhenHttp(POST /widgets/publish) did not match the predicate.", failure.Message, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"published\"", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "Reading a body that isn't there fails clearly rather than with a serializer exception.")]
    public async Task Response_on_empty_body_fails_clearly()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new RenameWidget(id, "Gear")).ToUrl("/widgets/rename"))
            .ThenResponse<WidgetReceipt>(_ => true));

        Assert.Equal("step 3 ThenResponse<WidgetReceipt>: step 2 WhenHttp(POST /widgets/rename) answered 204 with an empty body, so there is no WidgetReceipt to check", failure.Message);
    }

    [Fact]
    [Trait("business_value", "Async projections are caught up before a WhenHttp, so a GET reads the read model the Givens produced.")]
    public async Task Projections_are_synced_before_the_request()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Get.Url($"/widgets/{id}"))
            .ThenResponse<WidgetSummary>(s => s.Id == id && Is(s.Name, "Sprocket"))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "HTTP, commands and queries mix in one scenario, and each Then refers to the action before it.")]
    public async Task Http_mixes_with_commands_and_queries()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Gear"))
            .ThenAppended<WidgetRenamed>(id)
            .WhenHttp(x => x.Get.Url($"/widgets/{id}"))
            .ThenResponse<WidgetSummary>(s => Is(s.Name, "Gear"))
            .WhenHttp(x => x.Post.Json(new RenameWidget(id, "Cog")).ToUrl("/widgets/rename"))
            .ThenAppended<WidgetRenamed>(id, e => Is(e.Name, "Cog"))
            .ThenAppendedCount(id, 1)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(s => Is(s.Name, "Cog"))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "With HttpTenant set, a ForTenant scenario's request runs as that tenant, end to end.")]
    public async Task For_tenant_request_carries_the_tenant()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host, TenantByHeader)
            .ForTenant("http-a")
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new RecordTenant(id)).ToUrl("/widgets/tenant"))
            .ThenAppended<TenantRecorded>(id, e => Is(e.TenantId, "http-a"))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A ForTenant scenario can't silently send its request as the default tenant: without HttpTenant it fails at once.")]
    public async Task For_tenant_without_http_tenant_fails_at_once()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .ForTenant("http-b")
            .WhenHttp(x => x.Get.Url($"/widgets/{Guid.NewGuid()}"))
            .ThenStatusCode(404));

        Assert.StartsWith("step 1 WhenHttp: the scenario has ForTenant but HarnessOptions.HttpTenant is not set", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "WhenHttp on a host that isn't an Alba host fails with a clear message instead of a cast error.")]
    public async Task Non_alba_host_fails_clearly()
    {
        using var plain = new PlainHost(fixture.Host);

        var failure = await FailsAsync(TestHarness.Setup(plain)
            .WhenHttp(x => x.Get.Url("/widgets")));

        Assert.StartsWith("step 1 WhenHttp: WhenHttp needs an Alba host (IAlbaHost), but Setup was given PlainHost", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A request configuration that throws is reported as a test bug at the WhenHttp, not as an endpoint exception.")]
    public async Task Throwing_request_configuration_is_a_setup_failure()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .WhenHttp(_ => throw new InvalidOperationException("bad request setup")));

        Assert.StartsWith("step 1 WhenHttp: configuring the request threw InvalidOperationException", failure.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    /// <summary>
    /// Wraps a real host so the harness sees an <see cref="IHost"/> that isn't an Alba host.
    /// </summary>
    /// <param name="inner">The host to delegate to.</param>
    private sealed class PlainHost(IHost inner) : IHost
    {
        public IServiceProvider Services => inner.Services;

        public void Dispose()
        {
            // The fixture owns the inner host.
        }

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
