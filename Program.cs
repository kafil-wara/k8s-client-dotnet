using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Kubernetes configuration using token from service account
var tokenPath = "/var/run/secrets/kubernetes.io/serviceaccount/token";
var token = await File.ReadAllTextAsync(tokenPath);

// TODO - for PROD
// var config = new KubernetesClientConfiguration
// {
//     Host = "https://kubernetes.default.svc", // Default API server URL within a cluster
//     AccessToken = token,
//     SslCaCerts = new System.Net.Http.HttpClientHandler().ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
// };

// DEV - NO SSL CERT


IKubernetes client = new Kubernetes(config);

// Define a payload model for job and cronjob creation
public class JobRequest
{
    public string Namespace { get; set; } // Namespace for the job
    public string JobName { get; set; }
    public string Image { get; set; }
    public List<string> Command { get; set; }
}

public class CronJobRequest
{
    public string Namespace { get; set; } // Namespace for the cronjob
    public string CronJobName { get; set; }
    public string Schedule { get; set; }
    public string Image { get; set; }
    public List<string> Command { get; set; }
}

// Get pods in a namespace
app.MapGet("/pods", async ([FromQuery] string namespaceName) =>
{
    var pods = await client.ListNamespacedPodAsync(namespaceName);
    return pods.Items.Select(p => p.Metadata.Name).ToList();
})
.WithName("GetPods")
.WithOpenApi();

// Create a job with custom image and command from the payload
app.MapPost("/create-job", async ([FromBody] JobRequest request) =>
{
    var job = new V1Job
    {
        Metadata = new V1ObjectMeta { Name = request.JobName },
        Spec = new V1JobSpec
        {
            Template = new V1PodTemplateSpec
            {
                Spec = new V1PodSpec
                {
                    Containers = new List<V1Container>
                    {
                        new V1Container
                        {
                            Name = request.JobName,
                            Image = request.Image,
                            Command = request.Command
                        }
                    },
                    RestartPolicy = "Never"
                }
            }
        }
    };

    try
    {
        var createdJob = await client.CreateNamespacedJobAsync(job, request.Namespace);
        return Results.Ok($"Job '{createdJob.Metadata.Name}' created successfully with image '{request.Image}' and command '{string.Join(" ", request.Command)}'.");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Failed to create job: {ex.Message}");
    }
})
.WithName("CreateJob")
.WithOpenApi();

// New endpoint to list all jobs in a namespace
app.MapGet("/jobs", async ([FromQuery] string namespaceName) =>
{
    try
    {
        var jobs = await client.ListNamespacedJobAsync(namespaceName);
        var jobList = jobs.Items.Select(job => new
        {
            Name = job.Metadata.Name,
            Status = job.Status.Conditions?.FirstOrDefault()?.Type ?? "Unknown",
            StartTime = job.Status.StartTime,
            CompletionTime = job.Status.CompletionTime
        }).ToList();

        return Results.Ok(jobList);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Failed to list jobs: {ex.Message}");
    }
})
.WithName("GetJobs")
.WithOpenApi();

// New endpoint to create a CronJob
app.MapPost("/create-cronjob", async ([FromBody] CronJobRequest request) =>
{
    var cronJob = new V1CronJob
    {
        Metadata = new V1ObjectMeta { Name = request.CronJobName },
        Spec = new V1CronJobSpec
        {
            Schedule = request.Schedule,
            JobTemplate = new V1JobTemplateSpec
            {
                Spec = new V1JobSpec
                {
                    Template = new V1PodTemplateSpec
                    {
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = request.CronJobName,
                                    Image = request.Image,
                                    Command = request.Command
                                }
                            },
                            RestartPolicy = "Never"
                        }
                    }
                }
            }
        }
    };

    try
    {
        var createdCronJob = await client.CreateNamespacedCronJobAsync(cronJob, request.Namespace);
        return Results.Ok($"CronJob '{createdCronJob.Metadata.Name}' created successfully with schedule '{request.Schedule}'.");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Failed to create CronJob: {ex.Message}");
    }
})
.WithName("CreateCronJob")
.WithOpenApi();

app.Run();