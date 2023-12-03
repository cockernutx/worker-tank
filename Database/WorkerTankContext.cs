using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text.Json;

namespace WorkerTankApi.Database;

public class WorkerTankContext : DbContext
{
    public DbSet<Job> Jobs {get; set;}
    public DbSet<Worker> Workers {get; set;}



    public WorkerTankContext()
    {

    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
       protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseNpgsql($"User ID=worker_tank;Password=worker_tank_aAVIY@20SDnVV3;Host=db;Port=5432;Database=worker_tank;Pooling=true;");
}

public enum JobStatus
{
    Requested,
    Processing,
    Finished
}

public record Job
{
    public Guid Id { get; init; } = new Guid();
    public required Worker Worker {get; init;}
    public required JobStatus Status { get; set; }
    [Column(TypeName = "jsonb")]
    public required string JobData { get; set; }
[Column(TypeName = "jsonb")]
    public string? JobResult {get; set;}
}


public record Worker 
{
    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^*_";
    [Key]
    public required string Name {get; set;}
    public ICollection<Job> Jobs {get; set;} = new List<Job>();
    public string Pass {get; init;} = new string(Enumerable.Repeat(chars, 26)
        .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
}

