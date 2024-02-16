using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.Configure<OpenAIOptions>(context.Configuration.GetSection("OpenAI"));

        services.AddScoped(provider =>
        {
            var options = provider.GetRequiredService<IOptions<OpenAIOptions>>().Value;

            var builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(
                deploymentName: options.DeploymentName,
                modelId: "gpt-35-turbo",
                endpoint: options.Endpoint,
                apiKey: options.ApiKey
            );

            var kernel = builder.Build();

            return kernel;
        });
    })
    .Build();

host.Run();


internal class OpenAIOptions
{
    public string DeploymentName { get; set; }
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
}