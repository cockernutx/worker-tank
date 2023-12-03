using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WorkerTankApi.Database;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
namespace WorkerTankApi.Endpoints.Jobs;

public static class JobsModule
{
    public static void AddJobsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/jobs");
        group.MapPost("/request_jobs/", RequestJobs);
        group.MapGet("/get_job/{jobId}", GetJob);

        var workersOnlyGroup = group.MapGroup("/workers_only").RequireAuthorization();        
        workersOnlyGroup.MapGet("/fetch_jobs", FetchJobs);
        workersOnlyGroup.MapPatch("/processing/{uuid}", ProcessingJob);
        workersOnlyGroup.MapPatch("/finish/{uuid}", FinishJob);
    }

    public record JobRequest(string WorkerName, JsonDocument JobData);
    public static Results<Created, NotFound<string>> RequestJobs(JobRequest jobRequest, WorkerTankContext workerTankContext)
    {
        var worker = workerTankContext.Workers.SingleOrDefault(w => w.Name == jobRequest.WorkerName);
        if (worker == null) return TypedResults.NotFound("Worker not found!");

        var job = new Job()
        {
            Worker = worker,
            Status = Database.JobStatus.Requested,
            JobData = JsonSerializer.Serialize(jobRequest.JobData)
        };
        worker.Jobs.Add(job);

        workerTankContext.SaveChanges();

        return TypedResults.Created($"/jobs/get_job/{job.Id}");
    }
    public record JobInfo(Guid Id, string WorkerName, JobStatus Status, JsonDocument? JobData, JsonDocument? JobResult);
    public static Results<Ok<JobInfo>, NotFound> GetJob(Guid jobId, WorkerTankContext workerTankContext)
    {
        var j = workerTankContext.Jobs.Where(job => job.Id == jobId).Include(job => job.Worker).SingleOrDefault();
        if (j == null) return TypedResults.NotFound();
        var worker = j.Worker;
        return TypedResults.Ok(new JobInfo(j.Id, worker.Name, j.Status, JsonSerializer.Deserialize<JsonDocument>(j.JobData), JsonSerializer.Deserialize<JsonDocument>(j.JobResult == null ? "{}" : j.JobResult)));

    }
    public static Ok<List<JobInfo>> FetchJobs(ClaimsPrincipal claims, WorkerTankContext workerTankContext)
    {
        var workerName = claims.Claims.First(x => x.Type == "WorkerName").Value;
        var worker = workerTankContext.Workers.Where(w => w.Name == workerName).Include(w => w.Jobs).Single();

        return TypedResults.Ok(worker.Jobs.Select(j => new JobInfo(j.Id, j.Worker.Name, j.Status, JsonSerializer.Deserialize<JsonDocument>(j.JobData), JsonSerializer.Deserialize<JsonDocument>(j.JobResult == null ? "{}" : j.JobResult))).ToList());
    }

    public record WorkerInfo(string WorkerName, Guid Pass);
    public static Results<Ok, NotFound<string>> ProcessingJob(Guid uuid, ClaimsPrincipal claims, WorkerTankContext workerTankContext)
    {
        var workerName = claims.Claims.First(x => x.Type == "WorkerName").Value;
        var job = workerTankContext.Jobs.Where(j => j.Id == uuid && j.Worker.Name == workerName).Include(j => j.Worker).SingleOrDefault();
        if (job == null) return TypedResults.NotFound("Job not found!");

        job.Status = JobStatus.Processing;

        workerTankContext.SaveChanges();

        return TypedResults.Ok();
    }
    public static Results<Ok, NotFound<string>> FinishJob(Guid uuid, JsonDocument jobResult, ClaimsPrincipal claims, WorkerTankContext workerTankContext)
    {
              var workerName = claims.Claims.First(x => x.Type == "WorkerName").Value;
        var job = workerTankContext.Jobs.Where(j => j.Id == uuid && j.Worker.Name == workerName).Include(j => j.Worker).SingleOrDefault();
        if (job == null) return TypedResults.NotFound("Job not found!");

        job.Status = JobStatus.Finished;
        job.JobResult = JsonSerializer.Serialize(jobResult);

        workerTankContext.SaveChanges();
        return TypedResults.Ok();
    }

}