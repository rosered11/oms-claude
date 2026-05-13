using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("token")]
    public IResult Token() =>
        Results.Ok(new { access_token = "dev-token", token_type = "Bearer", expires_in = 3600 });
}
