using AnimatedImage;
using AnimatedImage.Formats;
using AnimatedImageTest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AnimatedImageTestNative
{
    public sealed class TsetForAnimatedWebP
    {
        public TsetForAnimatedWebP()
        {
            var asmdir = Path.GetDirectoryName(typeof(TsetForAnimatedWebP).Assembly.Location);
            NativeLibsLocator.SetNativeLibraryPath(Path.Combine(asmdir, "Libs"));
        }

        [Test]
        [TestCase("BouncingBeachBallWebP.webp")]
        public void Sequence(string filename)
        {
            var imageStream = Open(filename);

            var success = FrameRenderer.TryCreate(
                                imageStream,
                                new BitmapFaceFactory(),
                                out var rendererBase);

            Assert.IsTrue(success, $"Failed to create FrameRenderer: '{filename}'");
        }

        public static Stream Open(string imagefilename)
        {
            var path = $"AnimatedImageTestNative.Inputs.{imagefilename}";

            return Assembly.GetCallingAssembly().GetManifestResourceStream(path)
                   ?? throw new ArgumentException($"image not found: '{imagefilename}'");
        }
    }
}
