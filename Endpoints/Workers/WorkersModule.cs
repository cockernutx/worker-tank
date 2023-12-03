using Microsoft.AspNetCore.Http.HttpResults;
using WorkerTankApi.Services;

namespace WorkerTankApi.Endpoints.Workers;

public static class WorkersModule
{
    public static void AddWorkersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/workers");

        group.MapPost("/register_worker", RegisterWorker);
    }

    public static Results<Ok<Guid>, Conflict<string>> RegisterWorker(string name, ManagerService managerService)
    {
        try
        {
            return TypedResults.Ok(managerService.RegisterWorker(name));
        }
        catch (ManagerService.WorkerAlreadyRegisteredException)
        {
            return TypedResults.Conflict("Worker already created");
        }
    }
}