using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// HttpClient wrapper that automatically adds authentication headers.
    /// </summary>
    public class AuthenticatedHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;

        public AuthenticatedHttpClient(HttpClient httpClient, IAuthService authService)
        {
            this._httpClient = httpClient;
            this._authService = authService;
        }

        /// <summary>
        /// Sends the operation.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            string? token = await this._authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await this._httpClient.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <param name="requestUri">The request Uri.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new (HttpMethod.Get, requestUri);
            return await this.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Executes post.
        /// </summary>
        /// <param name="requestUri">The request Uri.</param>
        /// <param name="content">The content.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new (HttpMethod.Post, requestUri) { Content = content };
            return await this.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Executes put.
        /// </summary>
        /// <param name="requestUri">The request Uri.</param>
        /// <param name="content">The content.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new (HttpMethod.Put, requestUri) { Content = content };
            return await this.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Deletes the operation.
        /// </summary>
        /// <param name="requestUri">The request Uri.</param>
        /// <param name="cancellationToken">The cancellation Token.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new (HttpMethod.Delete, requestUri);
            return await this.SendAsync(request, cancellationToken);
        }
    }
}
