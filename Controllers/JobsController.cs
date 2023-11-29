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
            return BadRequest("Worker not found!");
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


}

public class JobRequest
{
    public required string WorkerName { get; set; }
    public required dynamic JobData { get; set; }
}