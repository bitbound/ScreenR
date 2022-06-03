using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Web.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static void AddScreenR(this IServiceCollection services, bool addSignalR = true)
        {
            if (addSignalR)
            {
                services
                    .AddSignalR(options =>
                    {
                        options.MaximumReceiveMessageSize = 64_000;
                        options.MaximumParallelInvocationsPerClient = 2;
                    })
                    .AddMessagePackProtocol();
            }
        }
    }
}
