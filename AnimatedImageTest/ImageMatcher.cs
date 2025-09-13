using NUnit.Framework;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace AnimatedImageTest
{
    internal class ImageMatcher
    {
        public static bool MatchImage(BitmapFace receivedSourceImage, string imageName, int frameIndex)
        {
            var receivedPath = CreatePath(imageName, frameIndex, "received");
            var approvalPath = CreatePath(imageName, frameIndex, "approved");

            SKBitmap approvalImage = Open(approvalPath);
            {
                using var outstream = new FileStream(receivedPath, FileMode.Create);
                receivedSourceImage.Bitmap.Encode(outstream, SKEncodedImageFormat.Png, 0);
            }
            SKBitmap receivedImage = Open(receivedPath);

            return MatchImage(approvalImage, receivedImage);
        }

        private static bool MatchImage(SKBitmap img1, SKBitmap img2)
        {
            if (img1.Width != img2.Width) return false;
            if (img1.Height != img2.Height) return false;

            var bytes1 = ToBytes(img1);
            var bytes2 = ToBytes(img2);

            if (bytes1.Length != bytes2.Length)
                return false;

            for (var idx = 0; idx < bytes1.Length; ++idx)
            {
                if (Math.Abs(bytes1[idx] - bytes2[idx]) > 1)
                {
                    var shift = idx & 0xFFFFFFFC;
                    var col1 = (bytes1[shift] << 24) | (bytes1[shift + 1] << 16) | (bytes1[shift + 2] << 8) | bytes1[shift + 3];
                    var col2 = (bytes2[shift] << 24) | (bytes2[shift + 1] << 16) | (bytes2[shift + 2] << 8) | bytes2[shift + 3];

                    var msg = $"unmatch index: {idx}, ({(idx % (4 * img1.Width)) / 4}, {idx / 4 / img1.Width}), color:<{col1.ToString("X8")}><{col2.ToString("X8")}>";
                    Console.Write(msg);
                    return false;
                }
            }

            return true;

            static byte[] ToBytes(SKBitmap img)
            {
                var bits = new byte[img.Width * img.Height * 4];
                Marshal.Copy(img.GetPixels(), bits, 0, bits.Length);

                return bits;
            }
        }

        private static SKBitmap Open(string filepath)
        {
            using var stream = new FileStream(filepath, FileMode.Open);
            return SKBitmap.FromImage(SKImage.FromEncodedData(stream));
        }

        private static string CreatePath(string imageName, int frameIndex, string label)
            => Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "Outputs",
                    imageName,
                    $"{imageName}#{frameIndex.ToString("D2")}.{label}.png");
    }
}
