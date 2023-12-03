using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WorkerTankApi.Services;
using WorkerTankApi.Models;
using System.Diagnostics;
namespace WorkerTankApi.Endpoints.Jobs;

public static class JobsModule
{
    public static void AddJobsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/jobs");

        group.MapPost("/request_jobs/", RequestJobs);
        group.MapGet("/get_job/{jobId}", GetJob);
        group.MapGet("/fetch_jobs", FetchJobs);
        group.MapPatch("/processing/{uuid}", ProcessingJob);
        group.MapPatch("/finish/{uuid}", FinishJob);
    }

    public record JobRequest(string WorkerName, dynamic JobData);
    public static Results<Created, NotFound<string>> RequestJobs(JobRequest jobRequest, ManagerService managerService)
    {
        Guid uuid;
        try { uuid = managerService.AddJob(jobRequest.WorkerName, jobRequest.JobData); }
        catch { return TypedResults.NotFound("Worker not found!"); }
        return TypedResults.Created($"/jobs/get_job/{uuid}");
    }

    public static Results<Ok<JobInfo>, NotFound> GetJob(Guid jobId, ManagerService manager) =>
        manager.FindJob(jobId) is JobInfo job ? TypedResults.Ok(job) : TypedResults.NotFound();

    public static Results<Ok<List<JobInfo>>, ForbidHttpResult> FetchJobs(string workerName, Guid pass, ManagerService jobManager)
    {
        bool workerValidation = jobManager.ValidateWorker(workerName, pass);
        if (!workerValidation) return TypedResults.Forbid();

        return TypedResults.Ok(jobManager.WorkerJobs(workerName));
    }

    public record WorkerInfo(string WorkerName, Guid Pass);
    public static Results<Ok, NotFound<string>, ForbidHttpResult> ProcessingJob(Guid uuid, WorkerInfo workerInfo, ManagerService manager)
    {
        bool workerValidation = manager.ValidateWorker(workerInfo.WorkerName, workerInfo.Pass);
        if (!workerValidation) return TypedResults.Forbid();
        try
        {
            manager.ChangeJobStatus(JobStatus.Processing, uuid);
        }
        catch
        {
            return TypedResults.NotFound("Job not found");
        }
        return TypedResults.Ok();
    }
    public record FinishJobRequest(string WorkerName, Guid Pass, dynamic? JobData);
    public static Results<Ok, NotFound<string>, ForbidHttpResult> FinishJob(Guid uuid, FinishJobRequest finishInfo, ManagerService manager)
    {
        bool workerValidation = manager.ValidateWorker(finishInfo.WorkerName, finishInfo.Pass);
        if (!workerValidation) return TypedResults.Forbid();
        try
        {
            manager.FinishJob(uuid, finishInfo.JobData);
        }
        catch
        {
            return TypedResults.NotFound("Job not found");
        }
        return TypedResults.Ok();
    }

}