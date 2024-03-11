using System;
using System.ComponentModel;
using System.Globalization;

namespace AnimatedImage.Avalonia
{
    /// <summary>
    /// Converts the string expressing uri to AnimatedImageSource.
    /// </summary>
    public class AnimatedImageSourceConverter : TypeConverter
    {
        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            => Convert((string)value);

        /// <summary>
        /// Converts the string expressing uri to AnimatedImageSource.
        /// </summary>
        /// <param name="uriTxt">The string expressing uri.</param>
        public static AnimatedImageSource Convert(string uriTxt)
        {
            var s = uriTxt;
            var uri = s.StartsWith("/")
                ? new Uri(s, UriKind.Relative)
                : new Uri(s, UriKind.RelativeOrAbsolute);

            return new AnimatedImageUri(uri);
        }
    }
}
