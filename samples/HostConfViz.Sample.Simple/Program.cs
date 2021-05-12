using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;

namespace HostConfViz.Sample.Simple
{
    public static class Program
    {
        static void Main()
        {
            IConfigurationRoot confiuguration = new ConfigurationBuilder()
                .SetBasePath( Directory.GetCurrentDirectory() )
                .AddInMemoryCollection(
                    new Dictionary<string, string> { { "ENVIRONMENT", "Development" } } )
                .AddEnvironmentVariables( "DOTNET_" )
                .AddJsonFile( "appsettings.json", false, true )
                .AddJsonFile( "appsettings.development.json", false, true )
                .Build();

            var env = new HostingEnvironment {
                ApplicationName = "Sample.Simple",
                ContentRootPath = Directory.GetCurrentDirectory(),
                EnvironmentName = "Development"
            };

            var hostInfo = new HostInfo( confiuguration, env );
            hostInfo.Display();

            Console.ReadKey( true );
        }
    }
}
