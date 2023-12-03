using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WorkerTankApi.Database;
using System.Security.Claims;
using System.Text.Json;
namespace WorkerTankApi.Endpoints.Jobs;

public static class JobsModule
{
    public static void AddJobsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/jobs");
        group.MapPost("/request_jobs/", RequestJobs);
        group.MapGet("/get_job/{jobId}", GetJob);

        group.AllowAnonymous();

        var workersOnlyGroup = group.MapGroup("/workers_only");
        workersOnlyGroup.MapGet("/fetch_jobs", FetchJobs);
        workersOnlyGroup.MapPatch("/processing/{uuid}", ProcessingJob);
        workersOnlyGroup.MapPatch("/finish/{uuid}", FinishJob);
        workersOnlyGroup.RequireAuthorization();
    }

    public record JobRequest(string WorkerName, JsonDocument JobData);
    public static Results<Created, NotFound<string>> RequestJobs(JobRequest jobRequest, WorkerTankContext workerTankContext)
    {
        var worker = workerTankContext.Workers.Single(w => w.Name == jobRequest.WorkerName);
        if (worker == null) return TypedResults.NotFound("Worker not found!");

        var job = new Job()
        {
            Worker = worker,
            Status = Database.JobStatus.Requested,
            JobData = jobRequest.JobData.ToString()!
        };
        worker.Jobs.Add(job);

        workerTankContext.SaveChanges();

        return TypedResults.Created($"/jobs/get_job/{job.Id}");
    }
    public record JobInfo(Guid Id, string WorkerName, JobStatus Status, JsonDocument? JobData, JsonDocument? JobResult);
    public static Results<Ok<JobInfo>, NotFound> GetJob(Guid jobId, WorkerTankContext workerTankContext) =>
        workerTankContext.Jobs.Single(job => job.Id == jobId) is Job job ? TypedResults.Ok(new JobInfo(job.Id, job.Worker.Name, job.Status, JsonSerializer.Deserialize<JsonDocument>(job.JobData), JsonSerializer.Deserialize<JsonDocument>(job.JobResult == null ? "{}" : job.JobResult))) : TypedResults.NotFound();

    public static Ok<List<JobInfo>> FetchJobs(ClaimsPrincipal claims, WorkerTankContext workerTankContext)
    {
        var worker = workerTankContext.Workers.Single(w => w.Name == claims.Claims.First(x => x.Type == "WorkerName").Value);

        return TypedResults.Ok(worker.Jobs.Select(j => new JobInfo(j.Id, j.Worker.Name, j.Status, JsonSerializer.Deserialize<JsonDocument>(j.JobData), JsonSerializer.Deserialize<JsonDocument>(j.JobResult == null ? "{}" : j.JobResult))).ToList());
    }

    public record WorkerInfo(string WorkerName, Guid Pass);
    public static Results<Ok, NotFound<string>> ProcessingJob(Guid uuid, ClaimsPrincipal claims, WorkerTankContext workerTankContext)
    {
        var worker = workerTankContext.Workers.Single(w => w.Name == claims.Claims.First(x => x.Type == "WorkerName").Value);

        var job = worker.Jobs.Single(j => j.Id == uuid);
        if (job == null) return TypedResults.NotFound("Job not found!");

        job.Status = JobStatus.Processing;

        workerTankContext.SaveChanges();

        return TypedResults.Ok();
    }
    public static Results<Ok, NotFound<string>> FinishJob(Guid uuid, JsonDocument jobResult, ClaimsPrincipal claims, WorkerTankContext workerTankContext)
    {
        var worker = workerTankContext.Workers.Single(w => w.Name == claims.Claims.First(x => x.Type == "WorkerName").Value);

        var job = worker.Jobs.Single(j => j.Id == uuid);
        if (job == null) return TypedResults.NotFound("Job not found!");

        job.Status = JobStatus.Finished;
        job.JobResult = jobResult.ToString();

        workerTankContext.SaveChanges();
        return TypedResults.Ok();
    }

}