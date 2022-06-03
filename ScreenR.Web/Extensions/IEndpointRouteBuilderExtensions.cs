using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ScreenR.Web.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Web.Extensions
{
    public static class IEndpointRouteBuilderExtensions
    {
        public static void MapScreenRHub(this IEndpointRouteBuilder endpoints, string route)
        {
            endpoints.MapHub<DesktopHub>(route);
        }
    }
}
