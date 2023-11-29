using Microsoft.AspNetCore.Mvc;
using WorkerTankApi.Models;
using WorkerTankApi.Services;

[Route("[controller]")]
[ApiController]
public class WorkersController : ControllerBase
{
    private readonly ManagerService _managerService;
    public WorkersController(ManagerService managerService) {
        this._managerService = managerService;
    }

    [HttpPost("RegisterWorker")]
    public ActionResult<Guid> RegisterWorker([FromBody] string name) {
        try {
            return _managerService.RegisterWorker(name);
        }
        catch(ManagerService.WorkerAlreadyRegisteredException) {
            return Conflict("Worker already created");
        }
    }

}