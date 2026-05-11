using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("token")]
    public IResult Token() =>
        Results.Ok(new { accessToken = "dev-token", tokenType = "Bearer", expiresIn = 3600 });
}
