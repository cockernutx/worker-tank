using Microsoft.AspNetCore.Mvc;
using WorkerTankApi.Models;
using WorkerTankApi.Services;

namespace WorkerTankApi.Controllers;

[Route("[controller]")]
[ApiController]
public class JobsController : ControllerBase
{

    private readonly ManagerService _jobManager;
    public JobsController(ManagerService jobManager)
    {
        this._jobManager = jobManager;
    }

    [HttpPost]
    public ActionResult<Guid> RequestJob([FromBody] JobRequest jobRequest)
    {
        try
        {
            var id = _jobManager.AddJob(jobRequest.WorkerName, jobRequest.JobData);
            return id;
        }
        catch (ManagerService.WorkerNotFoundException)
        {
            return NotFound("Worker not found!");
        }
    }
    [HttpGet("{uuid}")]
    public JobInfo? GetJob(Guid uuid) => _jobManager.FindJob(uuid);



    [HttpGet("WorkerJobs")]
    public ActionResult<List<JobInfo>> FetchJobs(string workerName, Guid pass) {
        bool workerValidation = _jobManager.ValidateWorker(workerName, pass);
        if(!workerValidation) return Unauthorized();

        return _jobManager.WorkerJobs(workerName);
    }

    [HttpPatch("ProcessingJob/{uuid}")]
    public IActionResult ProcessingJob(Guid uuid, [FromBody] WorkerInfo workerInfo) {
        bool workerValidation = _jobManager.ValidateWorker(workerInfo.WorkerName, workerInfo.Pass);
        if(!workerValidation) return Unauthorized();
        try {
            _jobManager.ChangeJobStatus(JobStatus.Processing, uuid);
        }
        catch {
            return NotFound("Job not found");
        }
        return Ok();
    }

}

public class WorkerInfo {
    public required string WorkerName {get; set;}
    public required Guid Pass {get; set;}
}

public class JobRequest
{
    public required string WorkerName { get; set; }
    public required dynamic JobData { get; set; }
}