namespace OmsApi;

public class GetNearbyBranchesHandler
{
    private static readonly List<BranchDto> _branches =
    [
        new() { BranchId = "store-central-dc", Name = "Central DC",
            Address = "123 Main St, Bangkok", Lat = 13.7563, Lng = 100.5018 },
        new() { BranchId = "store-a", Name = "Store A",
            Address = "45 Sukhumvit Rd, Bangkok", Lat = 13.7381, Lng = 100.5600 },
        new() { BranchId = "store-b", Name = "Store B",
            Address = "88 Rama IV, Bangkok", Lat = 13.7231, Lng = 100.5230 }
    ];

    public IResult Handle(double? lat, double? lng, double radius = 10, int limit = 20)
    {
        if (lat is null || lng is null)
            return Results.BadRequest(new { error = "missing_coordinates", detail = "lat and lng are required." });

        var branches = _branches
            .Select(b => new
            {
                b.BranchId,
                b.Name,
                b.Address,
                b.Lat,
                b.Lng,
                distanceKm = HaversineKm(lat.Value, lng.Value, b.Lat, b.Lng),
                availableSlots = true
            })
            .Where(b => b.distanceKm <= radius)
            .OrderBy(b => b.distanceKm)
            .Take(limit)
            .ToList();

        return Results.Ok(new { branches });
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}

public class BranchDto
{
    public string BranchId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
}
