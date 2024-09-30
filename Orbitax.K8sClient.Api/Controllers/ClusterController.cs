using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using k8s;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using k8s.Models;
using System.Threading;
using System;
using System.Collections.Generic;


namespace Orbitax.K8sClient.Api.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ClusterController : ControllerBase
    {
        private readonly ILogger<ClusterController> _logger;
        private readonly IKubernetes client;
        public ClusterController(ILogger<ClusterController> logger)
        {
            var caCertPath = "/var/run/secrets/kubernetes.io/serviceaccount/ca.crt";
            var caCertPem = System.IO.File.ReadAllText(caCertPath);
            var caCertBytes = System.Text.Encoding.UTF8.GetBytes(caCertPem);
            var caCertCollection = new X509Certificate2Collection();
            var caCert = new X509Certificate2(caCertBytes);
            caCertCollection.Add(caCert);

            var tokenPath = "/var/run/secrets/kubernetes.io/serviceaccount/token";
            var token = System.IO.File.ReadAllTextAsync(tokenPath).Result;

            var config = new KubernetesClientConfiguration
            {
                Host = "https://kubernetes.default.svc",
                AccessToken = token,
                SslCaCerts = caCertCollection // Set the collection of CA certificates here
            };
            _logger = logger;
            client = new Kubernetes(config);
        }

        [HttpGet("Pods")]
        public IActionResult Pods([FromQuery] string namespaceName)
        {
            var list = client.CoreV1.ListNamespacedPod(namespaceName);           
            var podNames = list.Items.Select(p => p.Metadata.Name).ToList();
            return Ok(podNames);
        }

        // [HttpGet("Services")]
        // public IActionResult Services([FromBody] string namespaceName)
        // {
        //     var services = client.CoreV1.ListNamespacedService(namespaceName);
        //     var serviceNames = services.Items.Select(s => s.Metadata.Name).ToList();
        //     return Ok(serviceNames);
        // }

        // [HttpGet("Deployments")]
        // public IActionResult Deployments([FromBody] string namespaceName)
        // {
        //     var deployments = client.AppsV1.ListNamespacedDeployment(namespaceName);
        //     var deploymentNames = deployments.Items.Select(d => d.Metadata.Name).ToList();
        //     return Ok(deploymentNames);
        // }

        // [HttpGet("Jobs")]
        // public IActionResult Jobs([FromBody] string namespaceName)
        // {
        //     var jobs = client.BatchV1.ListNamespacedJob(namespaceName);
        //     var jobNames = jobs.Items.Select(j => j.Metadata.Name).ToList();
        //     return Ok(jobNames);
        // }

        // [HttpPost("CreateJob")]
        // public IActionResult CreateJob([FromBody] string namespaceName, [FromBody] V1Job job)
        // {
        //     var createdJob = client.BatchV1.CreateNamespacedJob(job, namespaceName);
        //     return Ok(createdJob.Metadata.Name);
        // }

        [HttpPost("CreateCronJob")]
        public IActionResult CreateCronJob(
            [FromQuery] string name,
            [FromQuery] string namespaceName,
            [FromQuery] string schedule,
            [FromQuery] string containerName,
            [FromQuery] string image,
            [FromQuery] string args = null)
        {
            var cronJob = new V1CronJob
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name
                },
                Spec = new V1CronJobSpec
                {
                    Schedule = schedule,
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
                                            Name = containerName,
                                            Image = image,
                                            Args = !string.IsNullOrEmpty(args) ? args.Split(',') : null
                                        }
                                    },
                                    RestartPolicy = "OnFailure"
                                }
                            }
                        }
                    }
                }
            };

            try
            {
                var createdCronJob = client.BatchV1.CreateNamespacedCronJobAsync(cronJob, namespaceName);

                return Ok(createdCronJob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating CronJob");
                return StatusCode(500, "Error creating CronJob: " + ex.Message);
            }
        }
    }

}
