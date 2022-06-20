using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Shared.Extensions
{
    public static class UriExtensions
    {
        public static string Trim(this Uri uri)
        {
            return uri.ToString().TrimEnd('/');
        }
    }
}
