using MediatR;

namespace DocumentShare.Application.Documents.Queries;

public record DocumentDto(Guid Id, string FileName, string ContentType, bool IsPublic, DateTimeOffset CreatedAt);

public record GetDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>;

public class GetDocumentsQueryHandler
    : IRequestHandler<GetDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    public Task<IReadOnlyList<DocumentDto>> Handle(
        GetDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<DocumentDto>>(Array.Empty<DocumentDto>());
    }
}
