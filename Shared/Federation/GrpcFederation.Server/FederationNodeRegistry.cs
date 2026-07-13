using Usm.Shared.Federation.GrpcFederation.Abstractions;

namespace Usm.Shared.Federation.GrpcFederation.Server;

public sealed class FederationNodeRegistry : IFederationNodeRegistry
{
    private readonly IReadOnlyCollection<Uri> _endpoints;

    public FederationNodeRegistry(IEnumerable<Uri> endpoints)
    {
        _endpoints = endpoints.Distinct().ToArray();
    }

    public IReadOnlyCollection<Uri> Endpoints => _endpoints;
}
