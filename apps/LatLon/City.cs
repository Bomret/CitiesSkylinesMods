namespace Bomret.LatLon;

sealed record City
{
    public required string Name { get; init; }
    public required string CountryCode { get; init; }
    public required string Country { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
}
