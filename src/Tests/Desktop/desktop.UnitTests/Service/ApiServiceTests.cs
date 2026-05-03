using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using de.openelp.feuerwehr.desktop.Service;
using de.openelp.feuerwehr.desktop.ViewModels;
using de.openelp.feuerwehr.domain;
using Microsoft.Extensions.Options;
using Xunit;

namespace de.openelp.feuerwehr.desktop.UnitTests.Service;

public class ApiServiceTests
{
    [Fact]
    public async Task GetAll_AddsBearerTokenToRequest()
    {
        var handler = new CaptureHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<InventoryItem> { new() { Name = "Helm" } })
            }
            ));

        var sut = CreateSut(handler, "jwt-token");

        var result = await sut.GetAll();

        Assert.Single(result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("jwt-token", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Create_AddsBearerTokenToRequest()
    {
        var handler = new CaptureHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var sut = CreateSut(handler, "jwt-token");

        await sut.Create(new InventoryItem { Name = "Leiter" });

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("jwt-token", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetAll_WithoutToken_Throws()
    {
        var handler = new CaptureHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var sut = CreateSut(handler, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(sut.GetAll);
    }

    private static ApiService CreateSut(HttpMessageHandler handler, string token)
    {
        var user = new ApplicationUser { AccessToken = token };
        var httpClient = new HttpClient(handler);
        var tokenStore = new AuthTokenStore { Token = user };
        var options = Options.Create(new ApiSettings { BaseUrl = "https://inventory.local/" });
        return new ApiService(httpClient, tokenStore, options);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responseFactory;
        public HttpRequestMessage? LastRequest { get; private set; }

        public CaptureHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return await _responseFactory(request);
        }
    }
}

