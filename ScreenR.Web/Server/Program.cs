using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ScreenR.Web.Server.Data;
using ScreenR.Web.Server.Hubs;
using ScreenR.Web.Server.Models;
using ScreenR.Web.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDb>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AppDb>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<ApplicationUser, AppDb>();

builder.Services.AddAuthentication()
    .AddIdentityServerJwt();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services
    .AddSignalR(config =>
    {
        config.MaximumParallelInvocationsPerClient = 3;
        config.MaximumReceiveMessageSize = 64_000;
    })
    .AddMessagePackProtocol();

builder.Services.AddSingleton<IDeviceConnectionsCache, DeviceConnectionsCache>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();


app.MapRazorPages();
app.MapControllers();
app.MapHub<ServiceHub>("/service-hub");
app.MapHub<DesktopHub>("/desktop-hub");
app.MapHub<UserHub>("/user-hub");
app.MapFallbackToFile("index.html");

using var scope = app.Services.CreateScope();
using var appDb = scope.ServiceProvider.GetRequiredService<AppDb>();
await appDb.Database.MigrateAsync();

app.Run();
