using WorkerTankApi.Models;

namespace WorkerTankApi.Services;

public class ManagerService
{
    private readonly IConfiguration _configurationManager;

    public ManagerService(IConfiguration manager)
    {
        this._configurationManager = manager;

    }

    private List<JobInfo> jobs = new();
    private Dictionary<string, Guid> workersList = new();

    public Guid RegisterWorker(string worker)
    {
        if (workersList.ContainsKey(worker))
        {
            throw new WorkerAlreadyRegisteredException();
        }

        var id = Guid.NewGuid();
        workersList.Add(worker, id);
        return id;
    }

    public bool ValidateWorker(string worker, Guid pass)
    {
        if (workersList.Contains(new KeyValuePair<string, Guid>(worker, pass)))
        {
            return true;
        }
        return false;
    }

    public Guid AddJob(string worker, dynamic data)
    {
        CheckWorker(worker);
        var uuid = Guid.NewGuid();
        jobs.Add(new JobInfo
        {
            WorkerName = worker,
            JobID = uuid,
            Status = JobStatus.Requested,
            JobData = data
        });

        return uuid;
    }

    public void ChangeJobStatus(JobStatus status, Guid job)
    {
        var jobIndex = CheckJob(job).Item2;
        jobs[jobIndex].Status = status;
    }
    public void FinishJob(Guid job, dynamic data) {
        var jobIndex = CheckJob(job).Item2;
        jobs[jobIndex].JobData = data;
    }

    public void RemoveJob(Guid jobID)
    {
        var jobToRemove = CheckJob(jobID);
        jobs.Remove(jobToRemove.Item1);
    }

    public JobInfo? FindJob(Guid jobID) =>
        jobs.Find(x => x.JobID == jobID);

    public List<JobInfo> WorkerJobs(string worker)
    {
        var list = jobs.FindAll(x => x.WorkerName == worker);
        return list;
    }
    private (JobInfo, int) CheckJob(Guid job)
    {
        var fnd = jobs.Find(x => x.JobID == job);
        if (fnd == null)
        {
            throw new JobNotFoundException();
        }
        var idx = jobs.IndexOf(fnd);
        return (fnd, idx);

    }
    private void CheckWorker(string worker)
    {
        if (!workersList.ContainsKey(worker))
        {
            throw new WorkerNotFoundException();
        }

    }

    public class JobNotFoundException : Exception
    {
        const string exceptionMessage = "The requested job was not found!";
        public JobNotFoundException() : base(exceptionMessage)
        {

        }
    }
    public class WorkerAlreadyRegisteredException : Exception
    {
        const string exceptionMessage = "This worker was already registered!";
        public WorkerAlreadyRegisteredException() : base(exceptionMessage)
        {

        }
    }
    public class WorkerNotFoundException : Exception
    {
        const string exceptionMessage = "Worker not found!";
        public WorkerNotFoundException() : base(exceptionMessage)
        {

        }
    }
}