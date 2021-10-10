using System.Collections.Generic;
using System.Security.Claims;

namespace kroniiapi.Helper
{
    public interface IJwtGenerator
    {
        string GenerateAccessToken(IEnumerable<Claim> claims);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}