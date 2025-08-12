using System.Net.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace SkillSnap.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        // Option: named HttpClient + factory (choose either https or http)
        builder.Services.AddHttpClient("SkillSnap.Api", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7261/"); // or "http://localhost:5025/"
        });

        // Make the named client the default HttpClient for @inject HttpClient Http
        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("SkillSnap.Api"));

        await builder.Build().RunAsync();
    }
}
