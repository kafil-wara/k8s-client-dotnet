// Controllers/ClusterController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orbitax.K8sClient.Api.Interfaces;
using System;
using System.Threading.Tasks;

namespace Orbitax.K8sClient.Api.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ClusterController : ControllerBase
    {
        private readonly IKubernetesService _kubernetesService;
        private readonly ILogger<ClusterController> _logger;

        public ClusterController(IKubernetesService kubernetesService, ILogger<ClusterController> logger)
        {
            _kubernetesService = kubernetesService;
            _logger = logger;
        }

        [HttpGet("Pods")]
        public async Task<IActionResult> GetPods([FromQuery] string namespaceName)
        {
            var podNames = await _kubernetesService.GetPodsAsync(namespaceName);
            return Ok(podNames);
        }

        [HttpGet("Services")]
        public async Task<IActionResult> GetServices([FromQuery] string namespaceName)
        {
            var serviceNames = await _kubernetesService.GetServicesAsync(namespaceName);
            return Ok(serviceNames);
        }

        [HttpGet("Deployments")]
        public async Task<IActionResult> GetDeployments([FromQuery] string namespaceName)
        {
            var deploymentNames = await _kubernetesService.GetDeploymentsAsync(namespaceName);
            return Ok(deploymentNames);
        }

        [HttpGet("Jobs")]
        public async Task<IActionResult> GetJobs([FromQuery] string namespaceName)
        {
            var jobNames = await _kubernetesService.GetJobsAsync(namespaceName);
            return Ok(jobNames);
        }

        [HttpPost("CreateCronJob")]
        public async Task<IActionResult> CreateCronJob(
            [FromQuery] string name,
            [FromQuery] string namespaceName,
            [FromQuery] string schedule,
            [FromQuery] string containerName,
            [FromQuery] string image,
            [FromQuery] string args = null)
        {
            try
            {
                var cronJob = await _kubernetesService.CreateCronJobAsync(name, namespaceName, schedule, containerName, image, args);
                return Ok(cronJob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating CronJob");
                return StatusCode(500, "Error creating CronJob: " + ex.Message);
            }
        }
    }
}
