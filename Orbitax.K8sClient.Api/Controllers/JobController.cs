using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using k8s;
using System.Security.Cryptography.X509Certificates;
using System.IO;


namespace Orbitax.K8sClient.Api.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class JobController : ControllerBase
    {
        private readonly ILogger<JobController> _logger;
        private readonly IKubernetes client;
        public JobController(ILogger<JobController> logger)
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
    }
}
