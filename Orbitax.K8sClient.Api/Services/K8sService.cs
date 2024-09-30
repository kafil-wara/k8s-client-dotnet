// Services/KubernetesService.cs
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Orbitax.K8sClient.Api.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Orbitax.K8sClient.Api.Services
{
    public class KubernetesService : IKubernetesService
    {
        private readonly IKubernetes _client;
        private readonly ILogger<KubernetesService> _logger;

        public KubernetesService(ILogger<KubernetesService> logger)
        {
            var caCertPath = "/var/run/secrets/kubernetes.io/serviceaccount/ca.crt";
            var caCertPem = File.ReadAllText(caCertPath);
            var caCertBytes = Encoding.UTF8.GetBytes(caCertPem);
            var caCertCollection = new X509Certificate2Collection();
            var caCert = new X509Certificate2(caCertBytes);
            caCertCollection.Add(caCert);

            var tokenPath = "/var/run/secrets/kubernetes.io/serviceaccount/token";
            var token = File.ReadAllTextAsync(tokenPath).Result;

            var config = new KubernetesClientConfiguration
            {
                Host = "https://kubernetes.default.svc",
                AccessToken = token,
                SslCaCerts = caCertCollection
            };

            _logger = logger;
            _client = new Kubernetes(config);
        }

        public async Task<List<string>> GetPodsAsync(string namespaceName)
        {
            var pods = await _client.CoreV1.ListNamespacedPodAsync(namespaceName);
            return pods.Items.Select(p => p.Metadata.Name).ToList();
        }

        public async Task<List<string>> GetServicesAsync(string namespaceName)
        {
            var services = await _client.CoreV1.ListNamespacedServiceAsync(namespaceName);
            return services.Items.Select(s => s.Metadata.Name).ToList();
        }

        public async Task<List<string>> GetDeploymentsAsync(string namespaceName)
        {
            var deployments = await _client.AppsV1.ListNamespacedDeploymentAsync(namespaceName);
            return deployments.Items.Select(d => d.Metadata.Name).ToList();
        }

        public async Task<List<string>> GetJobsAsync(string namespaceName)
        {
            var jobs = await _client.BatchV1.ListNamespacedJobAsync(namespaceName);
            return jobs.Items.Select(j => j.Metadata.Name).ToList();
        }

        public async Task<V1CronJob> CreateCronJobAsync(string name, string namespaceName, string schedule, string containerName, string image, string args = null)
        {
            var cronJob = new V1CronJob
            {
                Metadata = new V1ObjectMeta { Name = name },
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
                var createdCronJob = await _client.BatchV1.CreateNamespacedCronJobAsync(cronJob, namespaceName);
                return createdCronJob;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating CronJob");
                throw;
            }
        }
    }
}
