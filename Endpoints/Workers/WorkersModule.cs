using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;
using WorkerTankApi.Database;

namespace WorkerTankApi.Endpoints.Workers;

public static class WorkersModule
{
    public static void AddWorkersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/workers");

        group.MapPost("/register_worker", RegisterWorker);
        group.MapPost("/authenticate_worker", AuthenticateWorker);

        group.AllowAnonymous();
    }

    public static Results<Ok<string>, Conflict<string>> RegisterWorker(string name, WorkerTankContext workerTankContext)
    {
        var workerExists = workerTankContext.Workers.Single(w => w.Name == name);
        if (workerExists != null) return TypedResults.Conflict("Worker already exists");

        var worker = new Worker
        {
            Name = name
        };
        workerTankContext.Workers.Add(worker);

        return TypedResults.Ok(worker.Pass);

    }
    public record WorkerAuthenticationRequest(string Name, string Pass);
    public static Results<Ok<string>, UnauthorizedHttpResult> AuthenticateWorker(WorkerAuthenticationRequest authRequest, WorkerTankContext workerTankContext)
    {
        //workerTankContext.Workers.Single(w => w.Name == authRequest.Name && w.Pass == authRequest.Pass);
        var worker = (from w in workerTankContext.Workers where w.Name == authRequest.Name && w.Pass == authRequest.Pass select w).FirstOrDefault();
        if (worker == null) return TypedResults.Unauthorized();

        var tokenHandler = new JwtSecurityTokenHandler();
        var issuer = "WorkerTank";
        var audience = "";

        var tokenClaims = new Claim[]{
                new("WorkerName", worker.Name)
            };
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^*_";
        var key = new string(Enumerable.Repeat(chars, 26)
        .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.Now.AddHours(5);
        var token = new JwtSecurityToken(issuer: issuer, audience: audience, expires: expiry, signingCredentials: credentials, claims: tokenClaims);
        var stringToken = tokenHandler.WriteToken(token);

        return TypedResults.Ok(stringToken);
    }
}