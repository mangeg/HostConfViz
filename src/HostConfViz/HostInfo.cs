using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HostConfViz.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace HostConfViz
{
    internal class HostInfoOptions
    {
        public bool DisplayEnvironment { get; set; } = true;
        public bool DisplayConfig { get; set; } = true;
        public bool IgnoreGlobalEnv { get; set; } = true;
        public bool RedactSecrets { get; set; } = true;
    }

    public class HostInfo
    {
        private readonly IConfiguration _config;
        private readonly IHostEnvironment _env;
        private readonly HostInfoOptions _options;

        private const string SecretReplaceString = "*****";
        private static readonly Regex _connectionStringRegex = new( @"(?<key>[^=]+)=(?<value>[^;]+);?" );
        private static readonly Dictionary<string, string> _secretKeyWords = new( StringComparer.OrdinalIgnoreCase ) {
            { "key", SecretReplaceString },
            { "password", SecretReplaceString },
            { "secret", SecretReplaceString }
        };

        internal static readonly Style _mainStyle = new( Color.DeepSkyBlue3 );
        internal static readonly Style _headerStyle = new( Color.Yellow3 );
        internal static readonly Style _keyStyle = new( Color.PaleTurquoise1 );
        internal static readonly Style _valueStyle = new( Color.LightSalmon1 );
        internal static readonly Style _2ndValueStyle = new( Color.DarkOliveGreen2 );
        internal static readonly Style _numberStyle = new( Color.Gold1 );
        internal static readonly Style _boolStyle = new( Color.SlateBlue1 );

        public HostInfo( IConfiguration configuration, IHostEnvironment env )
        {
            _config = configuration;
            _env = env;

            _options = new HostInfoOptions();
            _config.GetSection( "HostInfo" ).Bind( _options );
        }

        public void Display()
        {
            if ( _options.DisplayEnvironment )
            {
                PrintEnvironment();
            }
            if ( _options.DisplayConfig )
            {
                PrintConfiguration();
            }

            AnsiConsole.Render( new Rule().RuleStyle( _headerStyle ) );
        }

        public void PrintEnvironment()
        {
            AnsiConsole.Render( new Rule( "🌳 Environment Details" ).LeftAligned().RuleStyle( _headerStyle ) );
            Table frameworkTable = new Table()
                .BorderStyle( _mainStyle )
                .RoundedBorder()
                .HideHeaders()
                .AddColumn( new TableColumn( HeaderText( "Key" ) ).Alignment( Justify.Right ) )
                .AddColumn( new TableColumn( HeaderText( "Value" ) ) )
                .AddRow( KeyText( "App Name" ), ValueText( _env.ApplicationName ) )
                .AddRow( KeyText( "App Version" ), NumberText( PlatformServices.Default.Application.ApplicationVersion ) )
                .AddRow( KeyText( "Environment" ), ValueText( _env.EnvironmentName ) )
                .AddRow( KeyText( "Content Root" ), Format( _env.ContentRootPath ) )
                .AddRow( KeyText( "Architecture" ), ValueText( RuntimeInformation.ProcessArchitecture ) )
                .AddRow( KeyText( "Framework Version" ), new Paragraph()
                    .AppendFormatted( PlatformServices.Default.Application.RuntimeFramework.Identifier )
                    .AppendFormatted( ",Version=" )
                    .AppendFormatted( PlatformServices.Default.Application.RuntimeFramework.Version )
                    .AppendFormatted( ",Profile=" )
                    .AppendFormatted( PlatformServices.Default.Application.RuntimeFramework.Profile ) )
                .AddRow( KeyText( "Runtime Name" ), ValueText( RuntimeInformation.FrameworkDescription ) )
                .AddRow( KeyText( "Runtime Version" ), NumberText( PlatformServices.Default.Application.RuntimeFramework.Version ) )
                .AddRow( KeyText( "Operating System" ), ValueText( RuntimeInformation.OSDescription ) );
            AnsiConsole.Render( frameworkTable );
        }

        public void PrintConfiguration()
        {
            if ( _config is not IConfigurationRoot root )
            {
                return;
            }

            List<IConfigurationProvider> providers = new();
            Stack<IConfigurationProvider> providersStack = new();
            root.Providers.Reverse().ToList().ForEach( providersStack.Push );
            while ( providersStack.TryPop( out IConfigurationProvider provider ) )
            {
                if ( provider is ChainedConfigurationProvider chainedProvider )
                {
                    foreach ( IConfigurationProvider innerProvider in chainedProvider.GetChianedProviders().Reverse() )
                    {
                        providersStack.Push( innerProvider );
                    }

                    continue;
                }

                if ( provider is EnvironmentVariablesConfigurationProvider envProvider )
                {
                    var prefix = envProvider.GetPrefix();
                    if ( string.IsNullOrWhiteSpace( prefix ) && _options.IgnoreGlobalEnv )
                    {
                        continue;
                    }
                }

                providers.Add( provider );
            }

            void RecurseChildren( IEnumerable<IConfigurationSection> children, Node parent )
            {
                foreach ( IConfigurationSection child in children )
                {
                    Stack<ValueInfo> values = HandleKey( providers, child.Path );
                    var node = new Node( child.Key ) { Values = values };
                    parent.Children.Add( node );

                    RecurseChildren( child.GetChildren(), node );
                }

                IEnumerable<Node> withValues = parent.Children.Where( c => c.Values.Count == 0 );
                IEnumerable<Node> withoutValues = parent.Children.Where( c => c.Values.Count > 0 );
                var newValueList = new List<Node>();
                newValueList.AddRange( withValues.OrderBy( v => v.Name ) );
                newValueList.AddRange( withoutValues.OrderBy( v => v.Name ) );
                parent.Children = newValueList;
            }

            var rootNode = new Node( string.Empty );
            RecurseChildren( root.GetChildren(), rootNode );

            AnsiConsole.Render( new Rule( "🔧 Configuration Details" ).LeftAligned().RuleStyle( _headerStyle ) );


            Table providerTable = new Table()
                .BorderStyle( _mainStyle )
                .RoundedBorder()
                .AddColumn( new TableColumn( new Text( "#", _headerStyle ) ) )
                .AddColumn( new TableColumn( new Text( "Type", _headerStyle ) ) )
                .AddColumn( new TableColumn( new Text( "Info", _headerStyle ) ) );

            foreach ( IConfigurationProvider provider in providers )
            {
                if ( provider is ChainedConfigurationProvider chainedProvider )
                {
                    foreach ( IConfigurationProvider innerProvider in chainedProvider.GetChianedProviders().Reverse() )
                    {
                        providersStack.Push( innerProvider );
                    }

                    continue;
                }

                Paragraph info = new();
                if ( provider is FileConfigurationProvider fileProvider )
                {
                    if ( fileProvider.Source.FileProvider is PhysicalFileProvider physicalFileProvider )
                    {
                        var fullPath = Path.Combine( physicalFileProvider.Root, fileProvider.Source.Path );
                        var relativePath = Path.GetRelativePath( _env.ContentRootPath, fullPath );
                        var exists = File.Exists( fullPath );
                        info.Append( relativePath, _2ndValueStyle.Link( fullPath ) );
                        info.Append( ", " ).Append( "Present", _keyStyle ).Append( "=" ).AppendFormatted( exists );
                        info.Append( ", " ).Append( "Optional", _keyStyle ).Append( "=" ).AppendFormatted( fileProvider.Source.Optional );
                    }
                    else
                    {
                        info.Append( fileProvider.Source.Path, _2ndValueStyle );
                    }
                }

                if ( provider is EnvironmentVariablesConfigurationProvider envProvider )
                {
                    var prefix = envProvider.GetPrefix();
                    info.Append( "Prefix", _keyStyle ).Append( " = " ).Append( prefix ?? "(none)", _valueStyle );
                }

                providerTable.AddRow(
               Format( providerTable.Rows.Count + 1 ),
                new Text( provider.GetType().Name, _valueStyle ),
                info );
            }

            AnsiConsole.Render( providerTable );
            Tree tree = new Tree( HeaderText( "Root" ) )
                .Style( _mainStyle )
                .Guide( TreeGuide.Line );
            RenderNode( tree, rootNode );

            AnsiConsole.Render( new Padder( tree, new Padding( 2, 0 ) ) );
        }

        private void RenderNode( IHasTreeNodes target, Node node )
        {
            if ( node.Children.Count == 0 && node.Values.Count == 0 )
            {
                return;
            }

            IHasTreeNodes parent = target;

            if ( !string.IsNullOrWhiteSpace( node.Name ) )
            {
                if ( node.Values.Count == 0 )
                {
                    parent = target.AddNode( KeyText( node.Name, Decoration.Dim ) );
                }
                else
                {
                    ValueInfo primary = node.Values.Pop();
                    var primaryValue = primary.Value;

                    GetReplacementForKey( node.Name, ref primaryValue );

                    Table table = new Table()
                        .NoBorder()
                        .HideHeaders()
                        .AddColumn( string.Empty )
                        .AddColumn( "Key" )
                        .AddColumn( string.Empty )
                        .AddColumn( "Value" )
                        .AddRow(
                            new Paragraph( primary.ProviderId.ToString( CultureInfo.InvariantCulture ), _2ndValueStyle ).Append( "|" ),
                            KeyText( node.Name ),
                            new Text( "=" ),
                            Format( primaryValue ) );

                    while ( node.Values.TryPop( out ValueInfo value ) )
                    {
                        var secondaryValue = value.Value;
                        GetReplacementForKey( node.Name, ref secondaryValue );
                        table.AddRow(
                            new Paragraph( value.ProviderId.ToString( CultureInfo.InvariantCulture ), _2ndValueStyle ).Append( "|" ),
                            new Text( string.Empty ),
                            new Text( string.Empty ),
                            Format( secondaryValue, Decoration.Dim | Decoration.Strikethrough ) );
                    }
                    parent = target.AddNode( table );
                }
            }

            foreach ( Node item in node.Children )
            {
                RenderNode( parent, item );
            }
        }

        private static Stack<ValueInfo> HandleKey( IList<IConfigurationProvider> providers, string key )
        {
            var ret = new Stack<ValueInfo>();

            for ( var i = 1; i <= providers.Count; i++ )
            {
                IConfigurationProvider provider = providers[i - 1];

                if ( !provider.TryGet( key, out string value ) )
                {
                    continue;
                }

                var maxLenght = 130;
                if ( value.Length > maxLenght )
                {
                    value = value[..maxLenght] + "...";
                }

                var valueInfo = new ValueInfo( value, i );


                ret.Push( valueInfo );
            }

            return ret;
        }

        private IRenderable Format( string text, Decoration decoration = default )
        {
            if ( decimal.TryParse( text, NumberStyles.Any, CultureInfo.InvariantCulture, out _ ) )
            {
                return NumberText( text, decoration );
            }
            if ( string.Equals( "true", text, StringComparison.OrdinalIgnoreCase ) || string.Equals( "false", text, StringComparison.OrdinalIgnoreCase ) )
            {
                return BoolText( text, decoration );
            }

            MatchCollection matches = _connectionStringRegex.Matches( text );
            if ( matches.Any() )
            {
                var p = new Paragraph();
                var endOfMatch = 0;
                foreach ( Match item in matches )
                {
                    string key = item.Groups["key"].Value;
                    string value = item.Groups["value"].Value;

                    GetReplacementForKey( key, ref value );

                    p.Append( key, _keyStyle ).Append( "=" ).Append( value, _valueStyle ).Append( ";" );
                    endOfMatch = item.Index + item.Length;
                }

                p.Append( text[endOfMatch..] );

                return p;
            }
            if ( Uri.TryCreate( text, UriKind.Absolute, out Uri uri ) )
            {
                return new Markup( $"[link {_2ndValueStyle.ToMarkup()}]{text}[/]" );
            }

            return ValueText( $"\"{text}\"", decoration );
        }

        private IRenderable Format( object obj, Decoration decoration = default )
        {
            var stringValue = obj switch {
                string objStr => objStr,
                _ => obj.ToString()
            };

            return Format( stringValue, decoration );
        }

        private bool GetReplacementForKey( string key, ref string target )
        {
            if ( !_options.RedactSecrets )
            {
                return false;
            }

            foreach ( var secretKeyName in _secretKeyWords.Keys )
            {
                if ( key.Contains( secretKeyName, StringComparison.OrdinalIgnoreCase ) )
                {
                    target = _secretKeyWords[secretKeyName];
                    return true;
                }
            }

            return false;
        }

        private static Text ValueText( string text, Decoration dec = default ) => Text( text, _valueStyle, dec );
        private static Text ValueText( object obj, Decoration dec = default ) => ValueText( obj.ToString(), dec );
        private static Text KeyText( string text, Decoration dec = default ) => Text( text, _keyStyle, dec );
        private static Text KeyText( object obj, Decoration dec = default ) => KeyText( obj.ToString(), dec );
        private static Text HeaderText( string text, Decoration dec = default ) => Text( text, _headerStyle, dec );
        private static Text HeaderText( object obj, Decoration dec = default ) => HeaderText( obj.ToString(), dec );
        private static Text NumberText( string text, Decoration dec = default ) => Text( text, _numberStyle, dec );
        private static Text NumberText( object obj, Decoration dec = default ) => NumberText( obj.ToString(), dec );
        private static Text BoolText( string text, Decoration dec = default ) => Text( text, _boolStyle, dec );
        private static Text BoolText( object obj, Decoration dec = default ) => BoolText( obj.ToString(), dec );

        private static Text Text( string text, Style style, Decoration decoration = default ) => new( text, style.Decoration( decoration ) );

        internal record Node( string Name )
        {
            internal List<Node> Children { get; set; } = new();
            internal Stack<ValueInfo> Values { get; init; } = new();
        }

        internal record ValueInfo( string Value, int ProviderId );
    }
}
