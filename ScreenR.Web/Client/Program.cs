using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using ScreenR.Web.Client;
using ScreenR.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("ScreenR.Web.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ScreenR.Web.ServerAPI"));

builder.Services.AddSingleton<IHubConnectionBuilder, HubConnectionBuilder>();
builder.Services.AddSingleton<IUserHubConnection, UserHubConnection>();
builder.Services.AddScoped<IJsInterop, JsInterop>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IModalService, ModalService>();

builder.Logging.AddFilter("System.Net.Http.HttpClient.ScreenR.Web.ServerAPI", LogLevel.Warning);

builder.Services.AddApiAuthorization();

await builder.Build().RunAsync();
