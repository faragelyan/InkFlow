using System.Security.Claims;
using InkFlow.Domain;

namespace InkFlow.Application.Interfaces.ExternalServices;

public interface IJwtProvider
{
    string GenerateAccessToken(AppUser user, IEnumerable<string> roles);
    RefreshToken GenerateRefreshToken(Guid userId);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
