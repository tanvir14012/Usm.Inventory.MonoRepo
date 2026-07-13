using Usm.Shared.Federation.GrpcFederation.Abstractions;

namespace Usm.Shared.Federation.GrpcFederation.Client;

public sealed class StaticFederationNodeRegistry : IFederationNodeRegistry
{
    public StaticFederationNodeRegistry(params Uri[] endpoints)
    {
        Endpoints = endpoints.Distinct().ToArray();
    }

    public IReadOnlyCollection<Uri> Endpoints { get; }
}
