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

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticatedHttpClient" /> class with its required dependencies.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to call the backend API.</param>
        /// <param name="authService">The authentication service supplying session state and user roles.</param>
        public AuthenticatedHttpClient(HttpClient httpClient, IAuthService authService)
        {
            this._httpClient = httpClient;
            this._authService = authService;
        }

        /// <summary>
        /// Adds the current bearer token and sends the HTTP request to the backend API.
        /// </summary>
        /// <param name="request">The HTTP request message input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the HTTP response returned by the backend; the caller must dispose it.</returns>
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
        /// Sends an authenticated HTTP GET request to the supplied backend URI.
        /// </summary>
        /// <param name="requestUri">The backend API path or URI to request.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the HTTP response returned by the backend; the caller must dispose it.</returns>
        public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new (HttpMethod.Get, requestUri);
            return await this.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Sends an authenticated HTTP POST request to the supplied backend URI.
        /// </summary>
        /// <param name="requestUri">The backend API path or URI to request.</param>
        /// <param name="content">The content to validate or return in the generated response.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the HTTP response returned by the backend; the caller must dispose it.</returns>
        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new (HttpMethod.Post, requestUri) { Content = content };
            return await this.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Sends an authenticated HTTP PUT request to the supplied backend URI.
        /// </summary>
        /// <param name="requestUri">The backend API path or URI to request.</param>
        /// <param name="content">The content to validate or return in the generated response.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the HTTP response returned by the backend; the caller must dispose it.</returns>
        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent? content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new (HttpMethod.Put, requestUri) { Content = content };
            return await this.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Sends an authenticated HTTP DELETE request to the supplied backend URI.
        /// </summary>
        /// <param name="requestUri">The backend API path or URI to request.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the HTTP response returned by the backend; the caller must dispose it.</returns>
        public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new (HttpMethod.Delete, requestUri);
            return await this.SendAsync(request, cancellationToken);
        }
    }
}
