#nullable disable
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScreenR.Desktop.Shared.Models;
using ScreenR.Web.Server.Models;

namespace ScreenR.Web.Server.Data
{
    public class AppDb : ApiAuthorizationDbContext<ApplicationUser>
    {
        public AppDb(
            DbContextOptions options,
            IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions)
        {
        }

        public DbSet<ServiceDevice> Devices { get; init; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ServiceDevice>()
                .HasKey(x => x.Id);

            builder.Entity<ServiceDevice>();
        }
    }
}