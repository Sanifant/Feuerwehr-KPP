using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using de.openelp.feuerwehr.App.Services;
using de.openelp.feuerwehr.Common.Models.Auth;

namespace de.openelp.feuerwehr.Tests.App;

public sealed class AuthServiceTests
{
    private const string EmailClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
    private const string NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
    private const string RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    [Fact]
    public async Task LoginOverload_UsesDefaultRememberMeAndStoresTokens()
    {
        InMemorySecureStorage storage = new InMemorySecureStorage();
        RecordingAuthHandler handler = new RecordingAuthHandler();
        handler.EnqueueJsonResponse("/api/Auth/login", HttpStatusCode.OK, new LoginResponse
        {
            AccessToken = CreateToken(DateTime.UtcNow.AddMinutes(30), "tester@example.com", "Test User", ["Admin"]),
            RefreshToken = "refresh-token",
        });

        AuthService service = CreateService(storage, handler);

        bool result = await service.LoginAsync("tester@example.com", "secret");

        Assert.True(result);
        Assert.Equal("tester@example.com", service.UserEmail);
        Assert.Equal("Test User", service.UserFullName);
        Assert.Equal(["Admin"], service.UserRoles);
        Assert.True(service.IsAuthenticated);
        Assert.Equal("refresh-token", await storage.GetAsync("refresh_token"));
    }

    [Fact]
    public async Task LoginWithRememberMe_TrueReturnsSuccessWithoutPersistingTokens()
    {
        InMemorySecureStorage storage = new InMemorySecureStorage();
        RecordingAuthHandler handler = new RecordingAuthHandler();
        handler.EnqueueJsonResponse("/api/Auth/login", HttpStatusCode.OK, new LoginResponse
        {
            AccessToken = CreateToken(DateTime.UtcNow.AddMinutes(30), "tester@example.com", "Test User", ["Admin"]),
            RefreshToken = "refresh-token",
        });

        AuthService service = CreateService(storage, handler);

        bool result = await service.LoginAsync("tester@example.com", "secret", rememberMe: true);

        Assert.True(result);
        Assert.False(service.IsAuthenticated);
        Assert.Null(await storage.GetAsync("access_token"));
        Assert.Null(await storage.GetAsync("refresh_token"));
    }

    [Fact]
    public async Task LoginAsync_ReturnsFalseWhenApiRequestFails()
    {
        InMemorySecureStorage storage = new InMemorySecureStorage();
        RecordingAuthHandler handler = new RecordingAuthHandler();
        handler.EnqueueJsonResponse("/api/Auth/login", HttpStatusCode.Unauthorized, new { message = "invalid" });

        AuthService service = CreateService(storage, handler);

        bool result = await service.LoginAsync("tester@example.com", "wrong");

        Assert.False(result);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task LogoutAsync_NotifiesServerAndClearsTokens()
    {
        InMemorySecureStorage storage = new InMemorySecureStorage();
        RecordingAuthHandler handler = new RecordingAuthHandler();
        handler.EnqueueJsonResponse("/api/Auth/login", HttpStatusCode.OK, new LoginResponse
        {
            AccessToken = CreateToken(DateTime.UtcNow.AddMinutes(30), "tester@example.com", "Test User", ["Admin"]),
            RefreshToken = "refresh-token",
        });
        handler.EnqueueJsonResponse("/api/Auth/logout", HttpStatusCode.OK, new { });

        AuthService service = CreateService(storage, handler);
        await service.LoginAsync("tester@example.com", "secret");

        await service.LogoutAsync();

        Assert.False(service.IsAuthenticated);
        Assert.Null(await storage.GetAsync("access_token"));
        Assert.Null(await storage.GetAsync("refresh_token"));
        Assert.Equal("/api/Auth/logout", handler.Requests.Last().RequestUri!.AbsolutePath);
        Assert.NotNull(handler.Requests.Last().Headers.Authorization);
        Assert.Equal("Bearer", handler.Requests.Last().Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsFalseWhenNoRefreshTokenIsAvailable()
    {
        AuthService service = CreateService(new InMemorySecureStorage(), new RecordingAuthHandler());

        bool result = await service.RefreshTokenAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_StoresNewTokensWhenRefreshSucceeds()
    {
        InMemorySecureStorage storage = new InMemorySecureStorage(new Dictionary<string, string?>
        {
            ["refresh_token"] = "old-refresh",
        });
        RecordingAuthHandler handler = new RecordingAuthHandler();
        string newAccessToken = CreateToken(DateTime.UtcNow.AddMinutes(40), "new@example.com", "New User", ["InventoryViewer"]);
        handler.EnqueueJsonResponse("/api/Auth/refresh", HttpStatusCode.OK, new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = "new-refresh",
        });

        AuthService service = CreateService(storage, handler);

        bool result = await service.RefreshTokenAsync();

        Assert.True(result);
        Assert.Equal(newAccessToken, await service.GetAccessTokenAsync());
        Assert.Equal("new@example.com", service.UserEmail);
        Assert.Equal(["InventoryViewer"], service.UserRoles);
        Assert.Equal("new-refresh", await storage.GetAsync("refresh_token"));
    }

    [Fact]
    public async Task GetAccessTokenAsync_RefreshesExpiredToken()
    {
        string expiredToken = CreateToken(DateTime.UtcNow.AddMinutes(-5), "old@example.com", "Old User", ["Admin"]);
        string refreshedToken = CreateToken(DateTime.UtcNow.AddMinutes(50), "fresh@example.com", "Fresh User", ["Admin", "EquipmentOfficer"]);
        InMemorySecureStorage storage = new InMemorySecureStorage(new Dictionary<string, string?>
        {
            ["access_token"] = expiredToken,
            ["refresh_token"] = "refresh-token",
        });
        RecordingAuthHandler handler = new RecordingAuthHandler();
        handler.EnqueueJsonResponse("/api/Auth/refresh", HttpStatusCode.OK, new LoginResponse
        {
            AccessToken = refreshedToken,
            RefreshToken = "refresh-new",
        });

        AuthService service = CreateService(storage, handler);

        string? token = await service.GetAccessTokenAsync();

        Assert.Equal(refreshedToken, token);
        Assert.Equal("fresh@example.com", service.UserEmail);
        Assert.Equal(["Admin", "EquipmentOfficer"], service.UserRoles);
    }

    [Fact]
    public async Task Constructor_LoadsStoredTokensAndExposesClaims()
    {
        string validToken = CreateToken(DateTime.UtcNow.AddMinutes(25), "stored@example.com", "Stored User", ["Admin", "InventoryViewer"]);
        InMemorySecureStorage storage = new InMemorySecureStorage(new Dictionary<string, string?>
        {
            ["access_token"] = validToken,
            ["refresh_token"] = "stored-refresh",
        });

        AuthService service = CreateService(storage, new RecordingAuthHandler());

        Assert.True(service.IsAuthenticated);
        Assert.Equal("stored@example.com", service.UserEmail);
        Assert.Equal("Stored User", service.UserFullName);
        Assert.Equal(["Admin", "InventoryViewer"], service.UserRoles);
        Assert.Equal(validToken, await service.GetAccessTokenAsync());
    }

    [Fact]
    public async Task AuthStateChanged_IsRaisedForTokenStoreAndClear()
    {
        InMemorySecureStorage storage = new InMemorySecureStorage();
        RecordingAuthHandler handler = new RecordingAuthHandler();
        handler.EnqueueJsonResponse("/api/Auth/login", HttpStatusCode.OK, new LoginResponse
        {
            AccessToken = CreateToken(DateTime.UtcNow.AddMinutes(30), "tester@example.com", "Test User", ["Admin"]),
            RefreshToken = "refresh-token",
        });
        handler.EnqueueJsonResponse("/api/Auth/logout", HttpStatusCode.OK, new { });
        AuthService service = CreateService(storage, handler);
        int raised = 0;
        service.AuthStateChanged += (_, _) => raised++;

        await service.LoginAsync("tester@example.com", "secret");
        await service.LogoutAsync();

        Assert.Equal(3, raised);
    }

    private static AuthService CreateService(InMemorySecureStorage storage, RecordingAuthHandler handler)
    {
        HttpClient client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test"),
        };
        return new AuthService(client, storage);
    }

    private static string CreateToken(DateTime expiresUtc, string email, string fullName, IReadOnlyList<string> roles)
    {
        List<Claim> claims =
        [
            new Claim(EmailClaimType, email),
            new Claim(NameClaimType, fullName),
        ];
        claims.AddRange(roles.Select(role => new Claim(RoleClaimType, role)));
        JwtSecurityToken token = new JwtSecurityToken(claims: claims, expires: expiresUtc, notBefore: expiresUtc.AddMinutes(-5));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class RecordingAuthHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, Queue<HttpResponseMessage>> _responses = [];

        public List<HttpRequestMessage> Requests { get; } = [];

        public void EnqueueJsonResponse<T>(string path, HttpStatusCode statusCode, T payload)
        {
            if (!this._responses.TryGetValue(path, out Queue<HttpResponseMessage>? queue))
            {
                queue = [];
                this._responses[path] = queue;
            }

            queue.Enqueue(new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(payload),
            });
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Requests.Add(request);
            string path = request.RequestUri!.AbsolutePath;
            if (this._responses.TryGetValue(path, out Queue<HttpResponseMessage>? queue) && queue.Count > 0)
            {
                return Task.FromResult(queue.Dequeue());
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class InMemorySecureStorage : ISecureStorage
    {
        private readonly Dictionary<string, string?> _values;

        public InMemorySecureStorage()
            : this(new Dictionary<string, string?>())
        {
        }

        public InMemorySecureStorage(Dictionary<string, string?> values)
        {
            this._values = values;
        }

        public Task SetAsync(string key, string value)
        {
            this._values[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
        {
            this._values.TryGetValue(key, out string? value);
            return Task.FromResult(value);
        }

        public Task RemoveAsync(string key)
        {
            this._values.Remove(key);
            return Task.CompletedTask;
        }
    }
}
