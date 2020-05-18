using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FolderFile;
using StdOttFramework.RestoreWindow;
using StdOttStandard;

namespace PictureView
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double zoomSpeed = 0.001, maxZoomFactor = 30;

        private readonly ViewModel viewModel;
        private bool gidImageLeaveState, gidControlsEnterState, didMove;
        private Point lastMouseToImagePos;

        public MainWindow()
        {
            InitializeComponent();

            RestoreWindowHandler.Activate(this, RestoreWindowSettings.GetDefault());

            viewModel = (ViewModel)DataContext;
            viewModel.BackgroundColor = Background is SolidColorBrush brush ? brush.Color : Colors.White;
            viewModel.ViewControls = false;

            viewModel.Sources = Environment.GetCommandLineArgs().Skip(1).ToArray();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
#if !DEBUG
            viewModel?.UpdateImages(0);
#endif
        }

        private void GidImage_MouseEnter(object sender, MouseEventArgs e)
        {
            if (gidImageLeaveState) viewModel.ViewControls = true;
        }

        private void GidImage_MouseLeave(object sender, MouseEventArgs e)
        {
            gidImageLeaveState = viewModel.ViewControls;
            viewModel.ViewControls = false;
        }

        private void GidControls_MouseEnter(object sender, MouseEventArgs e)
        {
            gidControlsEnterState = viewModel.ViewControls;
            viewModel.ViewControls = true;
        }

        private void GidControls_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!gidControlsEnterState) viewModel.ViewControls = false;
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            FileSystemImage img = viewModel.CurrentImage;
            BitmapSource crop = (BitmapSource)img?.CroppedImage;
            string text =
                $"Image: {img?.Image?.PixelWidth} x {img?.Image?.PixelHeight}\r\n" +
                $"Crop: {crop?.PixelWidth ?? -1} x {crop?.PixelHeight ?? -1}\r\n" +
                $"Size: {StdUtils.GetFormattedFileSize(img?.File.Length ?? -1)}";

            MessageBox.Show(text);
        }

        private void BtnOpenExplorer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = viewModel.CurrentImage.File.FullName;
                string args = $"/select,\"{path}\"";
                Process.Start("explorer.exe", args);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Open explorer error");
            }
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            viewModel.UpdateImages(-1);
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            viewModel.UpdateImages(1);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement.GetType() == typeof(TextBox))
            {
                if (e.Key == Key.Escape) imgCurrent.Focus();

                return;
            }

            FileInfo currentFile = viewModel.CurrentImage?.File;

            switch (e.Key)
            {
                case Key.W:
                    if (Delete(currentFile, !viewModel.IsDeleteDirect)) viewModel.UpdateImages(0);
                    break;

                case Key.Left:
                case Key.A:
                    viewModel.UpdateImages(-1);
                    break;

                case Key.S:
                    if (Copy(currentFile, viewModel.Destination, viewModel.CopyCollision))
                    {
                        viewModel.UpdateImages(1);
                    }

                    break;

                case Key.Right:
                case Key.D:
                    viewModel.UpdateImages(1);
                    break;

                case Key.Delete:
                    if (Delete(currentFile)) viewModel.UpdateImages(0);
                    break;

                case Key.Enter:
                    viewModel.UpdateImages(0);
                    break;

                case Key.Escape:
                    viewModel.IsFullscreen = false;
                    break;

                case Key.F:
                    viewModel.IsFullscreen = !viewModel.IsFullscreen;
                    break;

                case Key.C when Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl):
                    CopyToClipboard(viewModel.CurrentImage);
                    break;

                default:
                    return;
            }

            e.Handled = true;
        }

        private static bool Copy(FileInfo srcFile, Folder destFolder, FileSystemCollision collision)
        {
            if (srcFile == null || destFolder == null) return false;

            string destFileName = Path.Combine(destFolder.FullName, srcFile.Name);

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    if (collision == FileSystemCollision.Override) srcFile.CopyTo(destFileName, true);
                    else if (!File.Exists(destFileName)) srcFile.CopyTo(destFileName);
                    else if (collision == FileSystemCollision.Ask && AskCopy(srcFile, destFileName))
                    {
                        srcFile.CopyTo(destFileName, true);
                    }
                    else break;

                    return true;
                }
                catch (IOException)
                {
                    if (!File.Exists(destFileName)) break;
                }
                catch
                {
                    break;
                }
            }

            return false;
        }

        private static bool AskCopy(FileInfo srcFile, string destFileName)
        {
            FileInfo destFile = new FileInfo(destFileName);
            string message =
                $"Replace file?\r\nSource: {srcFile.FullName}\r\n" +
                $"Size {StdUtils.GetFormattedFileSize(srcFile.Length)}\r\n" +
                $"Destination: {destFileName}\r\n" +
                $"Size {StdUtils.GetFormattedFileSize(destFile.Length)}";

            return MessageBox.Show(message, "Replace?", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        private static bool Delete(FileInfo file, bool ask = true)
        {
            if (file == null) return false;

            try
            {
                if (ask && !AskDelete(file)) return false;

                file.Delete();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool AskDelete(FileInfo file)
        {
            if (file == null) return false;

            string size = StdUtils.GetFormattedFileSize(file.Length);
            string message = $"Delete?\r\n{file.FullName}\r\nSize: {size}";

            return MessageBox.Show(message, "Delete?", MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        private static void CopyToClipboard(FileSystemImage image)
        {
            try
            {
                if (image?.Image != null) Clipboard.SetImage(image.Image);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Copy file drop error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                if (image?.File == null) return;

                StringCollection dropList = new StringCollection {image.File.FullName};
                Clipboard.SetFileDropList(dropList);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Copy file drop error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (Copy(viewModel.CurrentImage?.File, viewModel.Destination, viewModel.CopyCollision))
            {
                viewModel.UpdateImages(1);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Delete(viewModel.CurrentImage?.File, !viewModel.IsDeleteDirect)) viewModel.UpdateImages(0);
        }

        private void BtnExplorer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = viewModel.CurrentImage?.File?.FullName;

                if (string.IsNullOrWhiteSpace(filePath)) return;

                string args = $"/select,\"{filePath}\"";
                Process.Start("explorer.exe", args);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Open explorer error");
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                viewModel.Sources = (string[])e.Data.GetData(DataFormats.FileDrop);
            }
        }

        private void GidImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            element.MouseMove += GidImage_MouseMove;
            lastMouseToImagePos = e.GetPosition(element);

            if (e.ClickCount == 2) viewModel.ResetZoom();

            imgCurrent.Focus();
            didMove = false;
        }

        private void GidImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).MouseMove -= GidImage_MouseMove;

            if (!didMove) viewModel.ViewControls = !viewModel.ViewControls;
        }

        private void GidImage_MouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                element.MouseMove -= GidImage_MouseMove;
                return;
            }

            Point currentPos = e.GetPosition(element);
            double deltaX = (lastMouseToImagePos.X - currentPos.X);
            double deltaY = (lastMouseToImagePos.Y - currentPos.Y);

            SetZoom(new Point(deltaX, deltaY), 1, currentPos);
            lastMouseToImagePos = currentPos;

            didMove = true;
        }

        private void GidImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactorOffset = 1 + e.Delta * zoomSpeed;

            SetZoom(new Point(), zoomFactorOffset, e.GetPosition(imgCurrent));
        }

        private void GidImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetZoom(new Point(), 1, new Point());
        }

        private void SetZoom(Point pixelOffset, double zoomFactorOffset, Point pixelZoomPoint)
        {
            FileSystemImage fileImage = viewModel.CurrentImage;
            BitmapSource img = fileImage?.Image;

            if (img == null) return;

            Rect rect = fileImage.CropRect ?? new Rect(0, 0, img.PixelWidth, img.PixelHeight);
            double zoomFactor = Math.Max(img.PixelWidth / rect.Width, img.PixelHeight / rect.Height);

            if (zoomFactor * zoomFactorOffset > maxZoomFactor) return;

            pixelOffset.X /= imgCurrent.ActualWidth;
            pixelOffset.Y /= imgCurrent.ActualHeight;

            pixelZoomPoint.X = rect.X + pixelZoomPoint.X / imgCurrent.ActualWidth * rect.Width;
            pixelZoomPoint.Y = rect.Y + pixelZoomPoint.Y / imgCurrent.ActualHeight * rect.Height;

            fileImage.CropRect = Zoom(gidImage.ActualWidth / gidImage.ActualHeight,
                rect, pixelOffset, zoomFactorOffset, pixelZoomPoint);
        }

        private static Rect Zoom(double gridRatio, Rect rect, Point offset,
            double zoomFactorOffset, Point zoomPoint)
        {
            double x, y, width, height;

            if (Math.Abs(zoomFactorOffset) > 0.001)
            {
                x = zoomPoint.X - (zoomPoint.X - rect.X) / zoomFactorOffset;
                y = zoomPoint.Y - (zoomPoint.Y - rect.Y) / zoomFactorOffset;
                width = rect.Width / zoomFactorOffset;
                height = rect.Height / zoomFactorOffset;
            }
            else
            {
                x = rect.X;
                y = rect.Y;
                width = rect.Width;
                height = rect.Height;
            }

            if (width / height < gridRatio) width = (int)(height * gridRatio);
            else if (width / height > gridRatio) height = (int)(width / gridRatio);

            x += offset.X * width;
            y += offset.Y * height;

            return new Rect(x, y, width, height);
        }

        private void SplFiles_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) viewModel.UseSource = true;
        }
    }
}
