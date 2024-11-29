using Avalonia.Animation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Avalonia.Threading;

namespace AnimatedImage.Avalonia
{
    internal class AnimationStyle : Style, IDisposable
    {
        private static readonly AttachedProperty<int> FrameIndexProperty =
            AvaloniaProperty.RegisterAttached<AnimationStyle, Image, int>("FrameIndex");

        private static Random s_random = new();

        private Image _image;
        private IterationCount _defaultCount;
        private Animation? _animation;
        private readonly FrameRenderer _renderer;
        private Observable<IImage>? _source;
        private IDisposable? _disposable1;
        private IDisposable? _disposable2;
        private int _oldIndex = -1;
        private bool _rendering = false;
        private bool _isDisposed = false;
        private Stopwatch _stopwatch = new Stopwatch();

        private AnimationStyle(Image image, FrameRenderer renderer) : base(x => x.OfType<Image>())
        {
            _image = image;
            _renderer = renderer;
            _source = new Observable<IImage>();
            _defaultCount = renderer.RepeatCount == 0 ?
                                IterationCount.Infinite :
                                new IterationCount((ulong)renderer.RepeatCount);

            _animation = new Animation()
            {
                Duration = renderer.Duration,
                IterationCount = _defaultCount,
            };

            for (var i = 0; i < renderer.FrameCount; ++i)
            {
                var keyframe = new KeyFrame() { KeyTime = renderer[i].Begin };
                keyframe.Setters.Add(new Setter(FrameIndexProperty, i));

                _animation.Children.Add(keyframe);
            }

            var lastKeyframe = new KeyFrame() { KeyTime = renderer.Duration };
            lastKeyframe.Setters.Add(new Setter(FrameIndexProperty, renderer.FrameCount - 1));
            _animation.Children.Add(lastKeyframe);

            Animations.Add(_animation);

            _disposable2 = image.Bind(Image.SourceProperty, _source);
            var observer = image.GetObservable(FrameIndexProperty);
            _disposable1 = observer.Subscribe(new Observer<int>(HandleFrame));
        }

        public void SetIterationCount(int count)
        {
            if (_animation is not null)
                _animation.IterationCount = _defaultCount;
        }

        public void SetSpeedRatio(double ratio)
        {
            if (_animation is not null)
                _animation.SpeedRatio = ratio;
        }

        public void SetRepeatBehavior(RepeatBehavior behavior)
        {
            if (_animation is not null)
            {
                if (behavior == RepeatBehavior.Default)
                    _animation.IterationCount = _defaultCount;

                else if (behavior == RepeatBehavior.Forever)
                    _animation.IterationCount = IterationCount.Infinite;

                else if (behavior.HasCount)
                    _animation.IterationCount = new IterationCount(behavior.Count);

                else if (behavior.HasDuration)
                {
                    var count = (ulong)Math.Ceiling(behavior.Duration.Ticks / (double)_animation.Duration.Ticks);
                    _animation.IterationCount = new IterationCount(count);
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_animation is not null)
            {
                _animation.Children.Clear();
                _animation.IterationCount = new IterationCount(0);

                _animation = null;
            }

            if (_disposable1 is not null)
            {
                _disposable1.Dispose();
                _disposable1 = null;
            }

            if (_disposable2 is not null)
            {
                _disposable2.Dispose();
                _disposable2 = null;
            }

            if (_source is not null)
            {
                _source.Dispose();
                _source = null;
            }

            _image.Styles.Remove(this);
        }

        private void HandleFrame(int frameIndex)
        {
            if (_source is null)
                return;

            var newIndex = frameIndex % _renderer.FrameCount;
            if (newIndex == _oldIndex)
            {
                return;
            }

            if (_rendering)
            {
                return;
            }

            var frame = _renderer[newIndex];
            var delay = (long)(frame.End - frame.Begin).TotalMilliseconds;

            _stopwatch.Restart();
            _rendering = true;
            _renderer.ProcessFrameAsync(newIndex)
                     .ConfigureAwait(false)
                     .GetAwaiter()
                     .OnCompleted(async () =>
                     {
                         try
                         {
                             if (_isDisposed) return;

                             var elapsed = await Dispatcher.UIThread.InvokeAsync(() =>
                             {
                                 if (_isDisposed) return 0;
                                 var face = (WriteableBitmapFace)_renderer.Current;
                                 _source.OnNext(face.Bitmap);
                                 _image.InvalidateVisual();
                                 return _stopwatch.ElapsedMilliseconds;
                             });

                             if (_isDisposed) return;

                             // If drawing takes too long, the next frame drawing will fire immediately.
                             // As a result, It may cause crowding the UI thread.
                             // The following code makes the drawing task to wait to reduce the workload on the UI thread.
                             if (delay < elapsed)
                             {
                                 var order = (int)Math.Max(1, Math.Log10((elapsed - delay) / 2));
                                 var baseV = Math.Min((int)Math.Pow(10, order), 500);
                                 var waittime = baseV + s_random.Next(baseV);
                                 await Task.Delay(waittime);
                             }

                             if (_isDisposed) return;

                             Dispatcher.UIThread.Invoke(() =>
                             {
                                 if (_isDisposed) return;
                                 _oldIndex = _renderer.CurrentIndex;
                                 _rendering = false;
                             });
                         }
                         catch (TaskCanceledException) { }
                     });
        }

        public static void Setup(Image image, double speedRatio, RepeatBehavior behavior, FrameRenderer renderer)
        {
            var animeStyle = new AnimationStyle(image, renderer);
            animeStyle.SetSpeedRatio(speedRatio);
            animeStyle.SetRepeatBehavior(behavior);
            image.Styles.Add(animeStyle);
        }
    }
}
