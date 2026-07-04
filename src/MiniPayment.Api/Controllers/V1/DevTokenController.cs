using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;

namespace MiniPayment.Api.Controllers.V1;

/// <summary>Development-only: mint a test JWT. Not available in Production.</summary>
[ApiController]
[Route("api/v1/dev")]
[ApiExplorerSettings(GroupName = "dev")]
public class DevTokenController(IConfiguration config, IHostEnvironment env) : ControllerBase
{
    /// <summary>
    /// Mint a short-lived JWT for testing. Available in Development only.
    /// </summary>
    [HttpPost("token")]
    [SwaggerOperation(Summary = "Mint dev token (Development only)", Tags = ["Dev"])]
    [SwaggerResponse(200, "JWT access token")]
    [SwaggerResponse(404, "Not available in Production")]
    public IActionResult MintToken()
    {
        if (!env.IsDevelopment())
            return NotFound();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = config.GetValue<int>("Jwt:ExpiryMinutes", 60);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "dev-client"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("client_id", "dev-client")
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return Ok(new
        {
            access_token = new JwtSecurityTokenHandler().WriteToken(token),
            expires_in = expiryMinutes * 60,
            token_type = "Bearer"
        });
    }
}
