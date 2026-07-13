namespace Usm.Shared.Federation.GrpcFederation.Abstractions;

public interface IFederationNodeRegistry
{
    IReadOnlyCollection<Uri> Endpoints { get; }
}
