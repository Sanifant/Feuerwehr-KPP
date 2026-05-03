using de.openelp.authentification.Models;

namespace de.openelp.authentification.Services;

public interface IJwtService
{
    (string AccessToken, string RefreshToken) GenerateTokens(User user);
}