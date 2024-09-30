// Interfaces/IKubernetesService.cs
using k8s.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orbitax.K8sClient.Api.Interfaces
{
    public interface IKubernetesService
    {
        Task<List<string>> GetPodsAsync(string namespaceName);
        Task<List<string>> GetServicesAsync(string namespaceName);
        Task<List<string>> GetDeploymentsAsync(string namespaceName);
        Task<List<string>> GetJobsAsync(string namespaceName);
        Task<V1CronJob> CreateCronJobAsync(string name, string namespaceName, string schedule, string containerName, string image, string args = null);
    }
}
