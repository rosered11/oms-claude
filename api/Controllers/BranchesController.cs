using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/branches")]
public class BranchesController(GetNearbyBranchesHandler getNearbyBranches) : ControllerBase
{
    [HttpGet("nearby")]
    public IResult GetNearby(
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] double radius = 10,
        [FromQuery] int limit = 20)
        => getNearbyBranches.Handle(lat, lng, radius, limit);
}
