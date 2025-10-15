using AnimatedImage.Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DemoForAvaloniaUI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private ImageSource? _selectedSource;

        [ObservableProperty]
        private ObservableCollection<ImageSource> _sources;

        [ObservableProperty]
        private double _speedRatio;

        [ObservableProperty]
        private RepeatBehavior _repeatBehavior;

        [ObservableProperty]
        private bool _isDefaultRepeatBehavior;

        [ObservableProperty]
        private bool _isForeverRepeatBehavior;

        [ObservableProperty]
        private bool _isSpecificCountRepeatBehavior;

        [ObservableProperty]
        private ulong _repeatCount;

        public MainWindowViewModel()
        {
            _sources = new(new[] {
                new ImageSource("--- animated gif ---"),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/bomb-once.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/Bomb.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/earth.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/monster.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/partialfirstframe.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/radar.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/siteoforigin.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/working.gif")),
                new ImageSource("--- animated png ---"),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/BouncingBeachBall.png")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/pause.png")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/play.png")),
                new ImageSource("--- animated webp ---"),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/stickman.webp")),
                new ImageSource("--- no animated ---"),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/nonanimated.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/nonanimated.png")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/nonanimated.webp")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/nonanimated_lossless.webp")),
                new ImageSource("--- not supported ---"),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/UnsupportImageFormat.bmp")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/UnsupportImageFormat.jpg")),
            });

            _speedRatio = 1;
            _isDefaultRepeatBehavior = true;
            _repeatBehavior = RepeatBehavior.Default;
            _repeatCount = 3;

            this.PropertyChanged += MainWindowViewModel_PropertyChanged;
        }

        private void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsDefaultRepeatBehavior) && IsDefaultRepeatBehavior)
            {
                RepeatBehavior = RepeatBehavior.Default;
            }

            if (e.PropertyName == nameof(IsForeverRepeatBehavior) && IsForeverRepeatBehavior)
            {
                RepeatBehavior = RepeatBehavior.Forever;
            }

            if (e.PropertyName == nameof(IsSpecificCountRepeatBehavior) && IsSpecificCountRepeatBehavior)
            {
                RepeatBehavior = new RepeatBehavior(RepeatCount);
            }

            if (e.PropertyName == nameof(RepeatCount))
            {
                IsSpecificCountRepeatBehavior = true;
                RepeatBehavior = new RepeatBehavior(RepeatCount);
            }
        }

        [RelayCommand]
        public async Task OpenFile()
        {
            var app = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (app is null) return;

            var result = await app.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open File",
                FileTypeFilter = new[]{
                    new FilePickerFileType("Images"){ Patterns=new[]{"*.png", "*.gif", "*.webp"} },
                    new FilePickerFileType("All Files"){ Patterns=new[]{"*.*" } }
                },
                AllowMultiple = false
            });

            if (result != null && result.Count > 0)
            {
                var source = new ImageSource(result[0].Path);
                Sources.Add(source);
                SelectedSource = source;
            }
        }

        public async void OpenUrl()
        {
            var app = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (app is null) return;

            var inputDialog = new InputDialog();
            var result = await inputDialog.ShowDialog<string?>(app);

            if (!String.IsNullOrEmpty(result))
            {
                var source = new ImageSource(new Uri(result));
                Sources.Add(source);
                SelectedSource = source;
            }
        }
    }

    public class ImageSource
    {
        public string Name { get; }
        public AnimatedImageSource? Source { get; }

        public ImageSource(string text)
        {
            Name = text;
            Source = null;
        }
        public ImageSource(Uri source)
        {
            Name = source.ToString();
            if (Name.Length > 100) Name = Name.Substring(0, 100);
            Source = new AnimatedImageSourceUri(source);
        }

        /* public ImageSource(Uri source)
        {
            Name = source.ToString();
            Source = (BitmapStream)AssetLoader.Open(source);
        } */

        public override string ToString() => Name;
    }
}
