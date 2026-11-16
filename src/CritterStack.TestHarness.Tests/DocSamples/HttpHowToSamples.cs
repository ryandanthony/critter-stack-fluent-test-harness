// -----------------------------------------------------------------------
// <copyright file="HttpHowToSamples.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable MA0006 // Samples use == on strings, as readers' own tests would.

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;

namespace CritterStack.TestHarness.Tests.DocSamples;

/// <summary>
/// Backs every C# block in docs/how-to/test-http-endpoints.md. Each test names the block it backs.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class HttpHowToSamples(NoDaemonHostFixture fixture)
{
    // Sample: test-http-endpoints.md, "Assert what an endpoint did".
    [Fact]
    [Trait("business_value", "The HTTP guide's POST sample works as documented.")]
    public async Task TestHttpEndpoints_post()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new RenameWidget(id, "Gear")).ToUrl("/widgets/rename"))
            .ThenStatusCode(204)
            .ThenAppended<WidgetRenamed>(id, e => e.Name == "Gear");
    }

    // Sample: test-http-endpoints.md, "Assert on the response body".
    [Fact]
    [Trait("business_value", "The HTTP guide's response-body sample works as documented.")]
    public async Task TestHttpEndpoints_response()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new PublishWidget(id)).ToUrl("/widgets/publish"))
            .ThenResponse<WidgetReceipt>(r => r.Status == "published")
            .ThenPublished<WidgetPublishedNotice>(n => n.Id == id);
    }

    // Sample: test-http-endpoints.md, "Expect an error status".
    [Fact]
    [Trait("business_value", "The HTTP guide's error-status sample works as documented.")]
    public async Task TestHttpEndpoints_error_status()
    {
        await TestHarness
            .Setup(fixture.Host)
            .WhenHttp(x => x.Get.Url($"/widgets/{Guid.NewGuid()}"))
            .ThenStatusCode(404);
    }

    // Sample: test-http-endpoints.md, "Endpoints that throw".
    [Fact]
    [Trait("business_value", "The HTTP guide's throwing-endpoint sample works as documented.")]
    public async Task TestHttpEndpoints_throws()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new FailWidget(id)).ToUrl("/widgets/fail"))
            .ThenThrows<WidgetException>()
            .ThenNotAppended<WidgetRenamed>(id);
    }

    // Sample: test-http-endpoints.md, "Send the tenant". The test host reads the tenant from the "tenant" header.
    [Fact]
    [Trait("business_value", "The HTTP guide's tenant sample works as documented.")]
    public async Task TestHttpEndpoints_tenant()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host, o => o.HttpTenant = (x, tenant) => x.WithRequestHeader("tenant", tenant))
            .ForTenant("tenant-a")
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new RecordTenant(id)).ToUrl("/widgets/tenant"))
            .ThenAppended<TenantRecorded>(id, e => e.TenantId == "tenant-a");
    }
}
