using AnimatedImage;
using AnimatedImage.Formats;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AnimatedImageTest
{
    public class TsetForAnimatedWebP
    {
        [Test]
        [TestCase("BouncingBeachBallWebP.webp")]
        public void Sequence(string filename)
        {
            var imageStream = Open(filename);
            var renderer = new WebpRenderer(imageStream, new BitmapFaceFactory());

            for (int i = 0; i < renderer.FrameCount; ++i)
            {
                renderer.ProcessFrame(i);

                var imageName = Path.GetFileNameWithoutExtension(filename);
                if (!ImageMatcher.MatchImage((BitmapFace)renderer.Current, imageName, i))
                {
                    Assert.Fail($"Frame unmatch: '{filename}' frame {i}");
                }
            }

        }

        [Test]
        [TestCase("BouncingBeachBallWebP.webp")]
        public void Jump(string filename)
        {
            var imageStream = Open(filename);
            var renderer = new WebpRenderer(imageStream, new BitmapFaceFactory());

            var indics = new List<int>();

            foreach (var step in Enumerable.Range(1, renderer.FrameCount))
            {
                indics.Add(0);

                for (int start = 1; start < renderer.FrameCount; ++start)
                    for (int idx = start; idx < renderer.FrameCount; idx += step)
                        indics.Add(idx);
            }

            foreach (int i in indics)
            {
                renderer.ProcessFrame(i);

                var imageName = Path.GetFileNameWithoutExtension(filename);
                if (!ImageMatcher.MatchImage((BitmapFace)renderer.Current, imageName, i))
                {
                    Assert.Fail($"Frame unmatch: '{filename}' frame {i}");
                }
            }
        }

        public static Stream Open(string imagefilename)
        {
            var path = $"AnimatedImageTest.Inputs.{imagefilename}";

            return Assembly.GetCallingAssembly().GetManifestResourceStream(path)
                   ?? throw new ArgumentException($"image not found: '{imagefilename}'");
        }
    }
}
