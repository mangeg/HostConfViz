using HostConfViz;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    public static class HostInfoHostExtensions
    {
        public static HostInfo CreateHostInfo( this IHost host )
        {
            HostInfo instance = ActivatorUtilities.CreateInstance<HostInfo>( host.Services );
            return instance;
        }

        public static HostInfo DisplayHostInfo( this IHost host )
        {
            HostInfo instance = CreateHostInfo( host );
            instance.Display();
            return instance;
        }
    }
}
