using Administration.Application.Common;
using Administration.Application.Departments.Commands;
using Administration.Application.Departments.Dtos;
using Administration.Application.Departments.Queries;
using MediatR;
using Usm.Shared.BuildingBlocks.Bootstrap;

namespace Administration.Api.Endpoints.Departments;

public sealed class DepartmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/departments", GetAll)
            .WithName("GetDepartments")
            .WithTags("Departments")
            .RequireAuthorization();

        app.MapPost("/departments", Create)
            .WithName("CreateDepartment")
            .WithTags("Departments")
            .RequireAuthorization();

        app.MapPut("/departments/{id:guid}", Update)
            .WithName("UpdateDepartment")
            .WithTags("Departments")
            .RequireAuthorization();

        app.MapDelete("/departments/{id:guid}", Delete)
            .WithName("DeleteDepartment")
            .WithTags("Departments")
            .RequireAuthorization();
    }

    private static async Task<IResult> GetAll(ISender sender, CancellationToken cancellationToken)
        => Results.Ok(await sender.Send(new GetDepartmentsQuery(), cancellationToken));

    private static async Task<IResult> Create(DepartmentInput input, ISender sender, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(new CreateDepartmentCommand(input), cancellationToken));
        }
        catch (ApplicationValidationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> Update(Guid id, DepartmentInput input, ISender sender, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(new UpdateDepartmentCommand(id, input), cancellationToken));
        }
        catch (ApplicationValidationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> Delete(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new DeleteDepartmentCommand(id), cancellationToken);
            return Results.NoContent();
        }
        catch (ApplicationValidationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}
