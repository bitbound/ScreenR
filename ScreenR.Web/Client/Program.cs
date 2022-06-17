using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Services;
using ScreenR.Web.Client;
using ScreenR.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("ScreenR.Web.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddHttpClient<IApiClient>("ScreenR.Web.ServerAPI", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ScreenR.Web.ServerAPI"));

builder.Services.AddSingleton<IUserHubConnection, UserHubConnection>();
builder.Services.AddSingleton<IHubConnectionBuilderFactory, HubConnectionBuilderFactory>();
builder.Services.AddScoped<IJsInterop, JsInterop>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IModalService, ModalService>();
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddSingleton<IAppState, AppState>();

builder.Logging.AddFilter("System.Net.Http.HttpClient.ScreenR.Web.ServerAPI", LogLevel.Warning);

builder.Services.AddApiAuthorization();

await builder.Build().RunAsync();
