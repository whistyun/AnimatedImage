using ReactiveUI;
using AnimatedImage.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoForAvaloniaUI.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private ImageSource _selectedSource;
        public ImageSource SelectedSource
        {
            get => _selectedSource;
            set => this.RaiseAndSetIfChanged(ref _selectedSource, value);
        }

        private ObservableCollection<ImageSource> _sources;
        public ObservableCollection<ImageSource> Sources
        {
            get => _sources;
            set => this.RaiseAndSetIfChanged(ref _sources, value);
        }

        public MainWindowViewModel()
        {
            Sources = new(new[] {
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/bomb-once.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/Bomb.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/BouncingBeachBall.png")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/earth.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/monster.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/nonanimated.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/nonanimated.png")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/partialfirstframe.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/pause.png")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/play.png")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/radar.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/siteoforigin.gif")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/UnsupportImageFormat.bmp")),
                new ImageSource(new Uri("avares://DemoForAvaloniaUI/Assets/working.gif"))
            });


            this.PropertyChanged += MainWindowViewModel_PropertyChanged;
        }

        private void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedSource))
            {
                SelectedSourceUpdated();
            }
        }

        private void SelectedSourceUpdated()
        {


        }
    }

    public class ImageSource
    {
        public string Name { get; }
        public IBitmapSource Source { get; }

        public ImageSource(Uri source)
        {
            Name = source.ToString();
            Source = (BitmapUri)source;
        }

        /* public ImageSource(Uri source)
        {
            Name = source.ToString();
            Source = (BitmapStream)AssetLoader.Open(source);
        } */

        public override string ToString() => Name;
    }
}
