using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SkillSnap.Client.Services;

namespace SkillSnap.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        // Configuration-based approach for API base address
        var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "https://localhost:7261/";
        
        builder.Services.AddHttpClient("SkillSnap.Api", client =>
        {
            client.BaseAddress = new Uri(apiBaseAddress);
        });

        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("SkillSnap.Api"));

        // Register the services that connect to the API
        builder.Services.AddScoped<IPortfolioUserService, PortfolioUserService>();
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddScoped<ISkillService, SkillService>();

        await builder.Build().RunAsync();
    }
}