using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace HostConfViz.Sample.GenericHost
{
    public static class Program
    {
        public static async Task Main( string[] args )
        {
            using IHost host = CreateHostBuilder( args ).Build();

            host.DisplayHostInfo();

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder( string[] args ) =>
            Host.CreateDefaultBuilder( args )
                .ConfigureServices( ( hostContext, services ) => {
                } );
    }
}
