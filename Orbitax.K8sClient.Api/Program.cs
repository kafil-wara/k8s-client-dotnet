using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Orbitax.K8sClient.Api.Interfaces;
using Orbitax.K8sClient.Api.Services;

namespace Orbitax.K8sClient.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddScoped<IK8sService, K8Service>();
            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
