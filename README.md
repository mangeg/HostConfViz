# HostConfViz
Display environment and configuration information for .net core 3+ and .net 5+ applications

It uses [Spectre.Console](https://github.com/spectreconsole/spectre.console) to render the information. It is best displayed in a console that supports ANSI colors and unicode character set. It will fall back to supported colors and character set if needed when used in for example Kubernetes.

There is currently no way to configure the colors or layout used in the rendering.

### Supported framework versions
- netstandard 2.1
- net 5.0

## Samples
- ### **HostConfViz.Sample.GenericHost**\
  Use the ```IHost``` from the ```IHostBuilder.Build()``` to call extension method ```IHost.DisplayHostInfo()```.
- ### **HostConfViz.Sample.Simple**\
  Manually create an instance of the ```HostInfo``` class by passing IConfiguration and IHostingEnvironment to it and then call ```HostInfo.Display()```

## Usage
Most common usage is through extension method ```IHost.DisplayHostInfo()```.
```csharp
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
```

## Configuration
Configure by setting the below settings in your IConfiguration. Default values are as displayed in the code snippet.
```json
{
    "HostConfViz": {
        "DisplayEnvironment": true,         // Display environment info (app name, version, framework version etc.)
        "DisplayConfig": true,              // Display Configuration information (providers, values and overrides)
        "IgnoreGlobalEnvironment": false,   // Do not display configuration from EnvironmentVariablesConfigurationProvider with no prefix specified. 
        "RedactSecrets": true               // Redact secrets from the Configuration display (keys and values containing 'password', 'key' or 'secret' )
    }
}
```

## Tips
Set the with of the console in code to force it to render in a certain width. This to prevent it from short lines when hosted in Kubernetes etc where the reported console width is very narrow.
```csharp
if ( !string.IsNullOrWhiteSpace( Environment.GetEnvironmentVariable( "DOTNET_RUNNING_IN_CONTAINER" ) ) )
{
    // Static Spectre.Console configuration
    Spectre.Console.AnsiConsole.Profile.Width = 150;
}
```