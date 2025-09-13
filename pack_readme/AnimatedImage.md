A library to read animated GIF images , animated PNG images and animated WEBP images, usable in code.

When displaying in the UI, consider using
[AnimatedImage.Avalonia](https://www.nuget.org/packages/AnimatedImage.Avalonia) or
[AnimatedImage.Wpf](https://www.nuget.org/packages/AnimatedImage.Wpf) instead of this library.


## How to use

This library abstracts pixel read/write operations, requiring you to implement them yourself.
For implementations using SkiaSharp, please refer to 
[BitmapFaceFactory](https://github.com/whistyun/AnimatedImage/blob/develop-native/AnimatedImageTest/BitmapFaceFactory.cs) and
[BitmapFace](https://github.com/whistyun/AnimatedImage/blob/develop-native/AnimatedImageTest/BitmapFace.cs).

The following code opens a GIF image and renders its frames.

```csharp
// Read image file
using var imageStream = File.Open("/path/to/image.gif", FileMode.Open);
FrameRenderer.TryCreate(imageStream, new BitmapFaceFactory(), out var renderer);

// If you want to draw an image by frame index.
renderer.ProcessFrame(9);

// If you want to draw an image by time span.
renderer.ProcessFrame(TimeSpan.FromSeconds(34));

// Get the rendererd image.
var bitmapface = (BitmapFace)renderer.Current;
SKBitmap image = bitmapface.Bitmap;
```


## WebP supporting

To use WebP, [AnimatedImage.Native](https://www.nuget.org/packages/AnimatedImage.Native) library is required.

We have verified operation on the following platforms.

| Framework            | Architecture    | 
|----------------------|-----------------|
| .Net 9.0             | x86, x64, arm64 |
| .NET Framework 4.7.2 | x86, x64, arm64 |


## Features

* Animates GIF images in a normal `Image` control; no need to use a specific control
* Takes actual frame duration into account
* Repeat behavior can be specified; if unspecified, the repeat count from the GIF metadata is used
* Notification when the animation completes, in case you need to do something after the animation
* Animation preview in design mode (must be enabled explicitly)
* Support for controlling the animation manually (pause/resume/seek)
