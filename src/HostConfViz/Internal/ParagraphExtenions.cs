using System;
using System.Globalization;
using Spectre.Console;

namespace HostConfViz.Internal
{
    internal static class ParagraphExtenions
    {
        public static Paragraph AppendFormatted( this Paragraph paragraph, string text )
        {
            if ( decimal.TryParse( text, NumberStyles.Any, CultureInfo.InvariantCulture, out _ ) )
            {
                paragraph.Append( text, HostInfo._numberStyle );
            }
            else if ( string.Equals( "true", text, StringComparison.OrdinalIgnoreCase ) || string.Equals( "false", text, StringComparison.OrdinalIgnoreCase ) )
            {
                paragraph.Append( text, HostInfo._boolStyle );
            }
            else
            {
                paragraph.Append( text, HostInfo._valueStyle );
            }

            return paragraph;
        }

        public static Paragraph AppendFormatted( this Paragraph paragraph, object obj )
        {
            var stringValue = obj switch {
                string objStr => objStr,
                null => "null",
                _ => string.Format( CultureInfo.InvariantCulture, "{0}", obj )
            };

            return paragraph.AppendFormatted( stringValue );
        }
    }
}

