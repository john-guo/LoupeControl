using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Xaml.Behaviors;
using System.Windows.Input;

namespace TestWindow
{
    internal class LoupeBehavior : Behavior<Window>
    {
        private Ellipse loupe;
        private Point? lp;
        const int r = 20;
        const int f = 2;

        public LoupeBehavior()
        {
            loupe = new Ellipse()
            {
                Width = r * 2 * f,
                Height = r * 2 * f,
                IsHitTestVisible = false,
                Visibility = Visibility.Hidden,
            };
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.MouseMove += AssociatedObject_MouseMove;
            AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
            AssociatedObject.LocationChanged += AssociatedObject_LocationChanged;
        }

        private void AssociatedObject_LocationChanged(object? sender, EventArgs e)
        {
            if (AssociatedObject.IsLoaded)
            {
                loupe.RenderTransform = Transform.Identity;
                var visual = loupe.TransformToAncestor(AssociatedObject) as MatrixTransform;
                lp = new Point(visual.Matrix.OffsetX, visual.Matrix.OffsetY);
                loupe.Visibility = Visibility.Hidden;
            }
        }

        private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AssociatedObject.IsLoaded)
            {
                loupe.RenderTransform = Transform.Identity;
                var visual = loupe.TransformToAncestor(AssociatedObject) as MatrixTransform;
                lp = new Point(visual.Matrix.OffsetX, visual.Matrix.OffsetY);
                loupe.Visibility = Visibility.Hidden;
            }
        }

        private void AssociatedObject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point currentPosition = e.GetPosition(AssociatedObject);

            var bitmap = Capture(currentPosition);
            if (bitmap == null) return;

            loupe.Visibility = Visibility.Visible;
            loupe.Fill = new ImageBrush(bitmap);

            if (!lp.HasValue)
            {
                var visual = loupe.TransformToAncestor(AssociatedObject) as MatrixTransform;
                lp = new Point(visual.Matrix.OffsetX, visual.Matrix.OffsetY);
            }

            var trans = new TranslateTransform(-lp.Value.X + currentPosition.X - loupe.Width / 2, -lp.Value.Y + currentPosition.Y - loupe.Height / 2);
            loupe.RenderTransform = trans;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.Content is IAddChild parent)
            {
                parent.AddChild(loupe);
            }
        }

        private BitmapSource Capture(Point point)
        {
            try
            {
                loupe.Visibility = Visibility.Hidden;

                var renderTargetBitmap =
                    new RenderTargetBitmap((int)AssociatedObject.ActualWidth, (int)AssociatedObject.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(AssociatedObject);

                if (renderTargetBitmap == null) return null;

                var cropRect = new Int32Rect(Math.Max(0, (int)point.X - r), Math.Max(0, (int)point.Y - r), r * 2, r * 2);

                if (cropRect.IsEmpty) return null;
                return new CroppedBitmap(renderTargetBitmap, cropRect);
            } 
            catch 
            { 
                return null; 
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }
    }
}
