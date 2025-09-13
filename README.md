# AnimatedImage

A simple library to display animated GIF images , animated PNG images and animated WEBP images in WPF and AvaloniaUI, usable in XAML or in code.

![demo](doc/demo.gif)

## How to use (WPF)

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


## How to use (AvaloniaUI)

It has some compatibility with WPF: set the `AnimatedSource` attached property to the image you want:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:anim="https://github.com/whistyun/AnimatedImage.Avalonia"
        Title="MainWindow" Height="350" Width="525">
    <Image anim:ImageBehavior.AnimatedSource="avares://MyLib/Images/animated.gif" />
```

You can also specify the repeat behavior (the default is `0x`, which means it will use the repeat count from the GIF metadata):

```xml
<Image anim:ImageBehavior.RepeatBehavior="3x"
       anim:ImageBehavior.AnimatedSource="Images/animated.gif" />
```


## NuGet

* https://www.nuget.org/packages/AnimatedImage
* https://www.nuget.org/packages/AnimatedImage.Avalonia
* https://www.nuget.org/packages/AnimatedImage.Wpf
* https://www.nuget.org/packages/AnimatedImage.Native


## WebP supporting

To use WebP, [AnimatedImage.Native](https://www.nuget.org/packages/AnimatedImage.Native) library is required.

```
dotnet add package AnimatedImage.Native 
```

We have verified operation on the following platforms.

| Platform | Framework            | Architecture    | 
|----------|----------------------|-----------------|
| Linux    | .Net 9.0             | x64, arm64      |
| OSX      | .Net 9.0             | arm64           |
| Windows  | .Net 9.0             | x86, x64, arm64 |
| Windows  | .NET Framework 4.7.2 | x86, x64, arm64 |

The following source code was referenced when adding WebP animation support.

thomas694/WebP-wrapper-animatedWebP  
https://github.com/thomas694/WebP-wrapper-animatedWebP  

Mr-Ojii/AviUtl-WebPFileReader-Plugin  
https://github.com/Mr-Ojii/AviUtl-WebPFileReader-Plugin  


