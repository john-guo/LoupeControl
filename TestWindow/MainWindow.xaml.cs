using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool louping = false;

        const int r = 20;
        const double f = 1.25;
        Point? lp; 

        public MainWindow()
        {
            InitializeComponent();
        }

        private BitmapSource Capture(Point point)
        {
            try
            {
                loupe.Visibility = Visibility.Hidden;

                var renderTargetBitmap =
                    new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(this);

                if (renderTargetBitmap == null) return null;

                var cropRect = new Int32Rect(Math.Max(0, (int)point.X - r), Math.Max(0, (int)point.Y - r), r * 2, r * 2);

                if (cropRect.IsEmpty) return null;

                var croppedBitmap = new CroppedBitmap(renderTargetBitmap, cropRect);


                TransformedBitmap scaledBitmap = new TransformedBitmap(croppedBitmap,
                    new ScaleTransform(f, f));

                return scaledBitmap;
            }
            catch { return null; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            loupe.Visibility = Visibility.Hidden;
            louping = !louping;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!louping) return;

            Point currentPosition = e.GetPosition(this);

            var bitmap = Capture(currentPosition);
            if (bitmap == null) return;

            loupe.Visibility = Visibility.Visible;
            loupe.Fill = new ImageBrush(bitmap);

            if (!lp.HasValue)
            {
                var visual = loupe.TransformToAncestor(this) as MatrixTransform;
                lp = new Point(visual.Matrix.OffsetX, visual.Matrix.OffsetY);
            }

            var trans = new TranslateTransform(-lp.Value.X + currentPosition.X - loupe.Width / 2, -lp.Value.Y + currentPosition.Y - loupe.Height / 2);
            loupe.RenderTransform = trans;
        }
    }
}