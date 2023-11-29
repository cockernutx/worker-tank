using System.Runtime.InteropServices;

namespace WorkerTankApi.Models;

public enum JobStatus
{
    Requested,
    Processing,
    Finished
}

public class JobInfo
{
    public required string WorkerName { get; set; }
    public required Guid JobID { get; set; }
    public required JobStatus Status { get; set; }
    public required dynamic JobData { get; set; }

    public dynamic? JobResult {get; set;}
}