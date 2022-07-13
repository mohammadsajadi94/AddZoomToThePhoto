using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;

namespace AddZoomToThePhoto
{
    public partial class MainPage : UserControl
    {
        private MatrixTransform transform;
        private Point clickPoint;
        private Point currentOffset;
        private Image selectedpicbox;
        private double zoom = 1;
        private bool isDragging;
        public double MinZoom = 1;
        public double MaxZoom = 10;
        public double ZoomStep = 1;
        public double InitialZoom = 1;
        public double Zoom
        {
            get
            {
                return transform.Matrix.M11; //m11 and m22 are always the same value
            }
        }
        public MainPage()
        {
            InitializeComponent();
            imageBox.HorizontalAlignment = HorizontalAlignment.Center;
            imageBox.VerticalAlignment = VerticalAlignment.Center;
            imageBox.MouseLeftButtonDown += ImageBox_MouseLeftButtonDown;
            imageBox.MouseRightButtonDown += ImageBox_MouseRightButtonDown;
            imageBox.MouseWheel += ImageBox_MouseWheel;
            imageBox.MouseMove += ImageBox_MouseMove;
            imageBox.MouseLeftButtonUp += ImageBox_MouseLeftButtonUp;
            imageBox.MouseEnter += ImageBox_MouseEnter;
            imageBox.MouseLeave += ImageBox_MouseLeave;
            BtnloadImage.Click += BtnloadImage_Click;
            Btnroutatimage.Click += Btnroutatimage_Click;
        }

        private void Btnroutatimage_Click(object sender, RoutedEventArgs e)
        {
            if (selectedpicbox != null)
            {
                var pbox = selectedpicbox;
                Angle = (Angle - 90) % 360;
                var TransformedCenter = transform.Matrix.Transform(new Point(selectedpicbox.ActualWidth / 2, selectedpicbox.ActualHeight / 2));

                transform.Matrix = MultiplyMatrix(transform.Matrix, CreateRotationRadians(-90 * (Math.PI / 180.0),
                    TransformedCenter.X, TransformedCenter.Y));
                pbox.RenderTransform = transform;
            }
        }

        //private byte[] GetResourceImage(string imageFileName)
        //{
        //    System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
        //    System.Collections.Generic.IEnumerable<string> sa = asm.GetManifestResourceNames();

        //    List<string> names = new List<string>();
        //    foreach (string s in sa)
        //    {
        //        if (s.EndsWith(imageFileName))
        //            names.Add(s);
        //    }
        //    if (names.Count > 0)
        //    {
        //        System.IO.Stream s = asm.GetManifestResourceStream(names[0]);
        //        byte[] buf = new byte[s.Length];
        //        s.Read(buf, 0, (int)s.Length);
        //        s.Close();
        //        return buf;
        //    }
        //    return null;
        //}
        private void BtnloadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog()==true)
            {
                using (FileStream file = openFileDialog.File.OpenRead())
                {
                    //byte[] bfile = GetResourceImage("imagtest.jpg");
                    byte[] bfile = new byte[file.Length];
                    file.Read(bfile, 0, Convert.ToInt32(bfile.Length));
                    file.Close();

                    Stream imageStream = new System.IO.MemoryStream(bfile);

                    System.Windows.Media.Imaging.BitmapImage b = new System.Windows.Media.Imaging.BitmapImage();
                    b.SetSource(imageStream);
                    imageBox.Source = b;
                }
            }
        }

        private void ImageBox_MouseLeave(object sender, MouseEventArgs e)
        {
            var pbox = sender as Image;
            pbox.Cursor = System.Windows.Input.Cursors.Hand;
            isDragging = false;
        }

        private void ImageBox_MouseEnter(object sender, MouseEventArgs e)
        {
            var pbox = sender as Image;
            pbox.Cursor = System.Windows.Input.Cursors.Hand;
        }

        private void ImageBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var pbox = sender as Image;
            if (pbox != null)
            {
                pbox.Cursor = System.Windows.Input.Cursors.Hand;
                isDragging = false;
            }
        }

        private void ImageBox_MouseMove(object sender, MouseEventArgs e)
        {
            var child = sender as Image;
            if (!isDragging)
                return;
            Point newPoint = e.GetPosition(child.Parent as Grid);
            Point delta = Subtract(clickPoint, newPoint);

            double OffsetX = currentOffset.X - delta.X, OffsetY = currentOffset.Y - delta.Y;


            Matrix updatedXform = new Matrix(
                transform.Matrix.M11, transform.Matrix.M12, transform.Matrix.M21, transform.Matrix.M22,
                OffsetX, OffsetY
                );

            transform.Matrix = updatedXform;
            child.RenderTransform = transform;
        }

        private void ImageBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var pbox = sender as Image;
            if (selectedpicbox == pbox)
            {
                e.Handled = true;

                double scale = 1;
                if (e.Delta > 0)
                {
                    zoom += ZoomStep;
                    scale = 1.2;
                }
                else
                {
                    zoom -= ZoomStep;
                    scale = 1 / 1.2;
                }
                if (zoom > MaxZoom)
                {
                    zoom = MaxZoom;
                    scale = 1;
                }
                if (zoom < MinZoom)
                {
                    zoom = MinZoom;
                    scale = 1;
                }
                Point mousePos = e.GetPosition(pbox);
                transform.Matrix = MultiplyMatrix(transform.Matrix, CreateScaling(scale, scale, mousePos.X, mousePos.Y));
                pbox.RenderTransform = transform;
            }

        }

        private void ImageBox_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pbox = sender as Image;
            if (pbox != null)
            {
                reset(pbox);
            }
        }

        private void ImageBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pbox = sender as Image;
            if (pbox != null)
            {
                //var parntpixbox = pbox.Parent as Grid;
                transform = (pbox.RenderTransform as MatrixTransform);
                if (InitialZoom < MinZoom) InitialZoom = MinZoom;
                if (zoom == 1)
                {
                    transform.Matrix = new Matrix(InitialZoom, 0.0, 0.0, InitialZoom, 0.0, 0.0);
                }
                pbox.Cursor = System.Windows.Input.Cursors.Hand;
                var tmp = clickPoint;
                clickPoint = e.GetPosition(pbox.Parent as Grid);
                currentOffset = new Point(transform.Matrix.OffsetX, transform.Matrix.OffsetY);
                selectedpicbox = pbox;
                isDragging = true;
            }

        }
        private Point Subtract(Point a, Point b)
        {
            Point ret = new Point();
            ret.X = a.X - b.X;
            ret.Y = a.Y - b.Y;
            return ret;
        }
        public void reset(Image pbox)
        {
            transform = pbox.RenderTransform as MatrixTransform;
            Matrix m = new Matrix(1, 0, 0, 1, 0, 0);
            transform.Matrix = m;
            pbox.RenderTransform = transform;
            zoom = 1;
        }
        internal static Matrix CreateRotationRadians(double angle, double centerX, double centerY)
        {


            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            double dx = (centerX * (1.0 - cos)) + (centerY * sin);
            double dy = (centerY * (1.0 - cos)) - (centerX * sin);
            return new Matrix { M11 = cos, M12 = sin, M21 = -sin, M22 = cos, OffsetX = dx, OffsetY = dy };
        }
        internal static Matrix MultiplyMatrix(Matrix A, Matrix B)
        {
            return new Matrix(A.M11 * B.M11 + A.M12 * B.M21,
                              A.M11 * B.M12 + A.M12 * B.M22,
                              A.M21 * B.M11 + A.M22 * B.M21,
                              A.M21 * B.M12 + A.M22 * B.M22,
                              A.OffsetX * B.M11 + A.OffsetY * B.M21 + B.OffsetX,
                              A.OffsetX * B.M12 + A.OffsetY * B.M22 + B.OffsetY);
        }
        internal static Matrix CreateScaling(double scaleX, double scaleY, double centerX, double centerY)
        {
            return new Matrix(scaleX, 0, 0, scaleY, centerX - scaleX * centerX, centerY - scaleY * centerY);
        }
        public double Angle
        {
            get;
            private set;
        }
    }
}
