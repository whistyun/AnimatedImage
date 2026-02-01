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
        public static bool MatchImage(BitmapFace receivedSourceImage, string directoryName, string imageName)
        {
            var receivedPath = CreatePath(directoryName, imageName, "received");
            var approvalPath = CreatePath(directoryName, imageName, "approved");

            SKBitmap approvalImage = Open(approvalPath);
            {
                using var outstream = new FileStream(receivedPath, FileMode.Create);
                receivedSourceImage.Bitmap.Encode(outstream, SKEncodedImageFormat.Png, 0);
            }
            SKBitmap receivedImage = Open(receivedPath);

            return MatchImage(approvalImage, receivedImage);
        }

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

        private static bool MatchImage(SKBitmap approval, SKBitmap received)
        {
            if (approval.Width != received.Width) return false;
            if (approval.Height != received.Height) return false;

            var abytes = ToBytes(approval);
            var rbytes = ToBytes(received);

            if (abytes.Length != rbytes.Length)
                return false;

            for (var idx = 0; idx < abytes.Length; ++idx)
            {
                if (Math.Abs(abytes[idx] - rbytes[idx]) > 1)
                {
                    var shift = idx & 0xFFFFFFFC;
                    var acol = (abytes[shift] << 24) | (abytes[shift + 1] << 16) | (abytes[shift + 2] << 8) | abytes[shift + 3];
                    var rcol = (rbytes[shift] << 24) | (rbytes[shift + 1] << 16) | (rbytes[shift + 2] << 8) | rbytes[shift + 3];

                    var msg = $"unmatch index: {idx}, ({(idx % (4 * approval.Width)) / 4}, {idx / 4 / approval.Width}), approval_color:{acol.ToString("X8")}, received_color:{rcol.ToString("X8")}";
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
            => CreatePath(imageName, $"{imageName}#{frameIndex.ToString("D2")}", label);

        private static string CreatePath(string directoryName, string imageName, string label)
            => Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "Outputs",
                    directoryName,
                    $"{imageName}.{label}.png");
    }
}
