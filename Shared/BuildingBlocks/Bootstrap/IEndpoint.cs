using Microsoft.AspNetCore.Routing;

namespace Usm.Shared.BuildingBlocks.Bootstrap;

public interface IEndpoint
{
	void MapEndpoint(IEndpointRouteBuilder app);
}