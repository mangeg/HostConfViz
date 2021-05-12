using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace HostConfViz.Internal
{
    internal static class ConfigurationProviderExtensions
    {
        internal static string GetPrefix( this EnvironmentVariablesConfigurationProvider provider )
        {
            Type type = typeof( EnvironmentVariablesConfigurationProvider );
            FieldInfo field = type.GetField( "_prefix", BindingFlags.NonPublic | BindingFlags.Instance );
            string? value = field?.GetValue( provider ) as string;
            return value ?? string.Empty;
        }

        internal static IEnumerable<IConfigurationProvider> GetChianedProviders( this ChainedConfigurationProvider provider )
        {
            Type type = typeof( ChainedConfigurationProvider );
            FieldInfo configField = type.GetField( "_config", BindingFlags.NonPublic | BindingFlags.Instance );
            if ( configField?.GetValue( provider ) is IConfigurationRoot configValue )
            {
                return configValue.Providers;
            }

            return Enumerable.Empty<IConfigurationProvider>();
        }
    }
}

