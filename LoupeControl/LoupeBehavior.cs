using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Xaml.Behaviors;
using System.Windows.Input;

namespace LoupeControl
{
    public class LoupeBehavior : Behavior<Window>
    {
        private Ellipse _loupe;
        private Point? _initialPosition;
        private RenderTargetBitmap _renderTargetBitmap;
        private bool _isWorking = false;

        private const int DefaultSize = 60;
        private const double DefaultRate = 2.0;
        private const bool DefaultAnimationSupported = true;

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(int), typeof(LoupeBehavior), new PropertyMetadata(DefaultSize));

        public int Size
        {
            get { return (int)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        public static readonly DependencyProperty RateProperty =
            DependencyProperty.Register("Rate", typeof(double), typeof(LoupeBehavior), new PropertyMetadata(DefaultRate));

        public double Rate
        {
            get { return (double)GetValue(RateProperty); }
            set { SetValue(RateProperty, value); }
        }

        public static readonly DependencyProperty AnimationSupportedProperty =
            DependencyProperty.Register("AnimationSupported", typeof(bool), typeof(LoupeBehavior), new PropertyMetadata(DefaultAnimationSupported));
        public bool AnimationSupported
        {
            get { return (bool)GetValue(AnimationSupportedProperty); }
            set { SetValue(AnimationSupportedProperty, value); }
        }

        public class LoupeCommand : ICommand
        {
            public event EventHandler? CanExecuteChanged;
            internal event Action Executor;
            internal event Func<bool> CanExecutor;

            public bool CanExecute(object? parameter)
            {
                return CanExecutor?.Invoke() ?? false;
            }

            public void Execute(object? parameter)
            {
                Executor?.Invoke();
            }

            internal void RaiseExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public static readonly LoupeCommand MagnifyStart = new LoupeCommand();
        public static readonly LoupeCommand MagnifyFinish = new LoupeCommand();

        private static LoupeBehavior? _instance;
        public static bool IsWorking => _instance?._isWorking ?? false;

        protected override void OnAttached()
        {
            base.OnAttached();
            _instance = this;
            _loupe = new Ellipse()
            {
                Width = Size,
                Height = Size,
                IsHitTestVisible = false,
                Visibility = Visibility.Hidden,
            };

            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.MouseMove += AssociatedObject_MouseMove;
            AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
            AssociatedObject.LocationChanged += AssociatedObject_LocationChanged;

            MagnifyStart.Executor += () => { BeginLoupe(); MagnifyStart.RaiseExecuteChanged(); MagnifyFinish.RaiseExecuteChanged(); };
            MagnifyStart.CanExecutor += () => !_isWorking;
            MagnifyFinish.Executor += () => { EndLoupe(); MagnifyStart.RaiseExecuteChanged(); MagnifyFinish.RaiseExecuteChanged(); };
            MagnifyFinish.CanExecutor += () => _isWorking;
        }

        private void RecalculatePosition()
        {
            _loupe.RenderTransform = Transform.Identity;
            var visual = _loupe.TransformToAncestor(AssociatedObject) as MatrixTransform;
            _initialPosition = new Point(visual!.Matrix.OffsetX, visual.Matrix.OffsetY);
            _loupe.Visibility = Visibility.Hidden;
        }

        private void AssociatedObject_LocationChanged(object? sender, EventArgs e)
        {
            if (AssociatedObject.IsLoaded)
            {
                RecalculatePosition();
            }
        }

        private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AssociatedObject.IsLoaded)
            {
                RecalculatePosition();
            }
        }

        private void AssociatedObject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isWorking)
                return;

            Point currentPosition = e.GetPosition(AssociatedObject);

            var bitmap = Capture(currentPosition);
            if (bitmap == null) return;

            _loupe.Visibility = Visibility.Visible;
            _loupe.Fill = new ImageBrush(bitmap) { Stretch = Stretch.None };

            if (!_initialPosition.HasValue)
            {
                var visual = _loupe.TransformToAncestor(AssociatedObject) as MatrixTransform;
                _initialPosition = new Point(visual!.Matrix.OffsetX, visual.Matrix.OffsetY);
            }

            var trans = new TranslateTransform(-_initialPosition.Value.X + currentPosition.X - _loupe.Width / 2, -_initialPosition.Value.Y + currentPosition.Y - _loupe.Height / 2);
            _loupe.RenderTransform = trans;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.Content is IAddChild parent)
            {
                parent.AddChild(_loupe);
            }
        }

        private BitmapSource Capture(Point point)
        {
            try
            {
                _loupe.Visibility = Visibility.Hidden;

                var renderTargetBitmap = _renderTargetBitmap;
                if (renderTargetBitmap == null)
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)AssociatedObject.ActualWidth, (int)AssociatedObject.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render(AssociatedObject);
                }
                if (renderTargetBitmap == null) return null!;

                var cropRect = new Int32Rect(Math.Max(0, (int)point.X - Size), Math.Max(0, (int)point.Y - Size), Size * 2, Size * 2);
                if (cropRect.IsEmpty) return null!;
                var cropped = new CroppedBitmap(renderTargetBitmap, cropRect);
                return new TransformedBitmap(cropped, new ScaleTransform(Rate, Rate));
            }
            catch
            {
                return null!;
            }
        }

        private void BeginLoupe()
        {
            if (_isWorking) return;

            if (!AnimationSupported)
            {
                _renderTargetBitmap = new RenderTargetBitmap((int)AssociatedObject.ActualWidth, (int)AssociatedObject.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                _renderTargetBitmap.Render(AssociatedObject);
            }
            _isWorking = true;
        }

        private void EndLoupe()
        {
            if (!_isWorking) return;
            _loupe.Fill = null!;
            _loupe.Visibility = Visibility.Hidden;
            _isWorking = false;
            _renderTargetBitmap = null!;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            _loupe = null!;
            _instance = null;
        }
    }
}
