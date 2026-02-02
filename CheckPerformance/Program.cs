namespace CheckPerformance;

using AnimatedImage;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.IO;
using System.Reflection;


[SimpleJob(RuntimeMoniker.Net462)]
[SimpleJob(RuntimeMoniker.Net472)]
[SimpleJob(RuntimeMoniker.NetCoreApp31)]
[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.Net90)]
[RPlotExporter]
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<Program>();
    }

    [Params("BouncingBeachBall.png", "GrayscaleBouncingBeachBall.png")]
    public string TargetFile = "";

    private FrameRenderer _renderer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _renderer = Open(TargetFile);
    }

    [Benchmark]
    public object Logic()
    {
        for (int i = 0; i < _renderer.FrameCount; i++)
        {
            _renderer.ProcessFrame(i);
        }
        return _renderer.Current;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        ((IDisposable)_renderer).Dispose();
        _renderer = null!;
    }

    public static FrameRenderer Open(string imagefilename)
    {
        var path = $"CheckPerformance.Inputs.{imagefilename}";

        using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(path)
               ?? throw new ArgumentException($"image not found: '{imagefilename}'");

        var factory = new BitmapFaceFactory();
        return FrameRenderer.TryCreate(stream, factory, out var renderer) ?
                    renderer :
                    throw new ArgumentException("Unsupport format: '{imagefilename}'");
    }

}