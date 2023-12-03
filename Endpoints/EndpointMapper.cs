using WorkerTankApi.Endpoints.Jobs;
using WorkerTankApi.Endpoints.Workers;

namespace WorkerTankApi.Endpoints;

public static class EndpointMapper
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.AddJobsEndpoints();
        app.AddWorkersEndpoints();
    }
}