using Azure;
using Azure.Maps.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;

namespace Bomret.LatLon;

public sealed class GetLatLon(IMemoryCache cache)
{
	[Function("GetLatLon")]
	public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, CancellationToken cancellationToken)
	{
		if (req.Query.Count == 0)
		{
			return new BadRequestResult();
		}

		var query = req.Query["q"];
		if (cache.TryGetValue<City>(query, out var location))
		{
			return new OkObjectResult(location);
		}

		var key = Environment.GetEnvironmentVariable("SUBSCRIPTION_KEY")
			?? throw new InvalidOperationException("SUBSCRIPTION_KEY not found");

		var credential = new AzureKeyCredential(key);
		var client = new MapsSearchClient(credential);

		var res = await client.SearchAddressAsync(query, new SearchAddressOptions
		{
			EntityType = GeographicEntity.Municipality,
			Top = 1
		}, cancellationToken).ConfigureAwait(false);

		if (res is null || res.Value.Results.Count == 0)
		{
			return new NotFoundResult();
		}

		var searchAddressResultItem = res.Value.Results[0];

		location = new City
		{
			Name = searchAddressResultItem.Address.Municipality,
			Country = searchAddressResultItem.Address.Country,
			CountryCode = searchAddressResultItem.Address.CountryCode,
			Latitude = searchAddressResultItem.Position.Latitude,
			Longitude = searchAddressResultItem.Position.Longitude
		};

		cache.Set(query, location);

		return new OkObjectResult(location);
	}
}
