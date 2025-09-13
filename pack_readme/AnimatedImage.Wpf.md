A simple library to display animated GIF images , animated PNG images and animated WEBP images in WPF, usable in XAML or in code.

## How to use

*These properties are compatible with those of [WpfAnimatedGif](https://github.com/XamlAnimatedGif/WpfAnimatedGif).*

It's very easy to use: in XAML, instead of setting the `Source` property, set the `AnimatedSource` attached property to the image you want:

```xml
<Window x:Class="WpfAnimatedGif.Demo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:anim="https://github.com/whistyun/AnimatedImage.Wpf"
        Title="MainWindow" Height="350" Width="525">
    <Grid>
        <Image anim:ImageBehavior.AnimatedSource="Images/animated.gif" />
```

You can also specify the repeat behavior (the default is `0x`, which means it will use the repeat count from the GIF metadata):

```xml
<Image anim:ImageBehavior.RepeatBehavior="3x"
       anim:ImageBehavior.AnimatedSource="Images/animated.gif" />
```

And of course you can also set the image in code:

```csharp
var image = new BitmapImage();
image.BeginInit();
image.UriSource = new Uri(fileName);
image.EndInit();
ImageBehavior.SetAnimatedSource(img, image);
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
