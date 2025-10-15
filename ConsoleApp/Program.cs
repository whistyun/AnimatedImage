using AnimatedImage;
using ConsoleApp;
using SkiaSharp;
using System;
using System.IO;

public class Program
{
    public static void Main(string[] args)
    {
#if NativeLibLocation
        var arch = Environment.Is64BitProcess ? "MyNativeLibraryFor64" : "MyNativeLibraryFor86";
        NativeLibsLocator.SetNativeLibraryPath(Path.Combine(AppContext.BaseDirectory, arch));
#endif
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: input_image (time|frame) number(millisecond or frameindex) output_image");
        }

        var input = args[0];
        var flg = args[1] switch
        {
            "time" => FrameType.Time,
            "frame" => FrameType.Index,
            _ => throw new ArgumentException("Invalid argument: " + args[1])
        };
        var frame = double.Parse(args[2]);
        var output = args.Length > 3 ? args[4] :
                     Path.ChangeExtension(input, null) + "_" + frame + ".png";

        using var instream = File.OpenRead(input);
        using var renderer = FrameRenderer.Create(instream, new BitmapFaceFactory());

        if (flg == FrameType.Time)
        {
            renderer.ProcessFrame(TimeSpan.FromMilliseconds(frame));
        }
        else if (flg == FrameType.Index)
        {
            renderer.ProcessFrame((int)frame);
        }

        using var outstream = new FileStream(output, FileMode.Create);
        ((BitmapFace)renderer.Current).Bitmap.Encode(outstream, SKEncodedImageFormat.Png, 0);
    }

    enum FrameType
    {
        Time,
        Index
    }
}