A simple library to display animated GIF images, animated PNG images and animated WEBP images in AvaloniaUI, usable in XAML or in code.

## How to use

It's very easy to use: in XAML, instead of setting the `Source` property, set the `AnimatedSource` attached property to the image you want:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:anim="https://github.com/whistyun/AnimatedImage.Avalonia"
        Title="MainWindow" Height="350" Width="525">
    <Grid>
        <Image anim:ImageBehavior.AnimatedSource="Images/animated.gif" />
```

You can also specify the repeat behavior (the default is `0x`, which means it will use the repeat count from the GIF metadata):

```xml
<Image anim:ImageBehavior.RepeatBehavior="3x"
       anim:ImageBehavior.AnimatedSource="Images/animated.gif" />
```


## WebP supporting

To use WebP, [AnimatedImage.Native](https://www.nuget.org/packages/AnimatedImage.Native) library is required.
