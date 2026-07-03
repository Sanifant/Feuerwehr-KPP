using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Feuerwehr.App.Services;

/// <summary>
/// HttpClient wrapper that automatically adds authentication headers.
/// </summary>
public class AuthenticatedHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;

    public AuthenticatedHttpClient(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content };
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        return await SendAsync(request, cancellationToken);
    }
}
