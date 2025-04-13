using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PictureView.Folders;
using PictureView.Folders.FileExplorer;
using PictureView.ViewModels;
using StdOttStandard;

namespace PictureView.Views;

public partial class MainWindow : Window
{
    private const double zoomSpeed = 0.01, maxZoomFactor = 30;

    private MainWindowViewModel viewModel;
    private readonly IFileExplorer? fileExplorer;
    private bool gidImageLeaveState, gidControlsEnterState, didMove;
    private Point lastMouseToImagePos;

    public MainWindow()
    {
        InitializeComponent();

        fileExplorer = FileExplorerHelper.GetFileExplorer();
        btnOpenExplorer.IsVisible = fileExplorer != null;

        // TODO: make this work
        DragDrop.SetAllowDrop(this, true);
        DragDrop.DropEvent.AddClassHandler<MainWindow>(OnDrop);
    }

    private static void OnDrop(MainWindow sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            sender.viewModel.Sources = (string[])e.Data.Get(DataFormats.Files)!;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        viewModel = (MainWindowViewModel)DataContext!;
        viewModel.ViewControls = false;

        viewModel.Sources = Environment.GetCommandLineArgs().Skip(1).ToArray();
    }

    private void OnActivated(object? sender, EventArgs e)
    {
#if !DEBUG
        viewModel?.UpdateImages();
#endif
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (FocusManager?.GetFocusedElement()?.GetType() == typeof(TextBox))
        {
            if (e.Key == Key.Escape) imgCurrent.Focus();

            return;
        }

        FileInfo? currentFile = viewModel.CurrentImage?.File;

        switch (e.Key)
        {
            case Key.W:
                if (await Delete(currentFile, !viewModel.IsDeleteDirect))
                {
                    viewModel.UpdateImages();
                }

                break;

            case Key.Left:
            case Key.A:
                viewModel.UpdateImages(-1);
                break;

            case Key.S:
                if (await Copy(currentFile, viewModel.Destination, viewModel.CopyCollision))
                {
                    viewModel.UpdateImages(1);
                }

                break;

            case Key.Right:
            case Key.D:
                viewModel.UpdateImages(1);
                break;

            case Key.Delete:
                if (await Delete(currentFile)) viewModel.UpdateImages(0);
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

            case Key.C when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                await CopyToClipboard(viewModel.CurrentImage);
                break;

            default:
                return;
        }

        e.Handled = true;
    }

    private async Task<bool> Copy(FileInfo? srcFile, Folder destFolder, FileSystemCollision collision)
    {
        if (srcFile == null || !Directory.Exists(destFolder.Path)) return false;

        string destFileName = Path.Combine(destFolder.Path, srcFile.Name);

        for (int i = 0; i < 10; i++)
        {
            try
            {
                if (collision == FileSystemCollision.Override) srcFile.CopyTo(destFileName, true);
                else if (!File.Exists(destFileName)) srcFile.CopyTo(destFileName);
                else if (collision == FileSystemCollision.Ask && await AskCopy(srcFile, destFileName))
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

    private async Task<bool> AskCopy(FileInfo srcFile, string destFileName)
    {
        FileInfo destFile = new FileInfo(destFileName);
        string message =
            $"Replace file?\r\nSource: {srcFile.FullName}\r\n" +
            $"Size {StdUtils.GetFormattedFileSize(srcFile.Length)}\r\n" +
            $"Destination: {destFileName}\r\n" +
            $"Size {StdUtils.GetFormattedFileSize(destFile.Length)}";

        var result = await DialogWindow.Show(this, message, "Replace?", "Yes", "No");
        return result == DialogWindow.DialogResult.Primary;
    }

    private async Task<bool> Delete(FileInfo? file, bool ask = true)
    {
        if (file == null) return false;

        try
        {
            if (ask && !await AskDelete(file)) return false;

            file.Delete();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> AskDelete(FileInfo file)
    {
        string size = StdUtils.GetFormattedFileSize(file.Length);
        string message = $"Delete?\r\n{file.FullName}\r\nSize: {size}";

        var result = await DialogWindow.Show(this, message, "Replace?", "Yes", "No");
        return result == DialogWindow.DialogResult.Primary;
    }

    private async Task CopyToClipboard(FileSystemImage? image)
    {
        byte[]? imageData = image?.GetImageBytes();
        if (image == null || imageData == null) return;

        try
        {
            TopLevel topLevel = GetTopLevel(this)!;
            DataObject data = new DataObject();
            data.Set(StdUtils.GetMimeType(image.File.Extension), imageData);
            await topLevel.Clipboard!.SetDataObjectAsync(data);
        }
        catch (Exception exc)
        {
            System.Diagnostics.Debug.WriteLine(exc);
        }
    }

    private void BtnExplorer_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            FileSystemImage? img = viewModel.CurrentImage;
            if (img != null) fileExplorer?.Open(img.File.FullName, true);
        }
        catch (Exception exc)
        {
            System.Diagnostics.Debug.WriteLine(exc);
        }
    }

    private async void BtnCopy_Click(object? sender, RoutedEventArgs e)
    {
        FileInfo? currentFile = viewModel.CurrentImage?.File;
        if (await Copy(currentFile, viewModel.Destination, viewModel.CopyCollision))
        {
            viewModel.UpdateImages(1);
        }
    }

    private async void BtnDelete_Click(object? sender, RoutedEventArgs e)
    {
        if (await Delete(viewModel.CurrentImage?.File, !viewModel.IsDeleteDirect))
        {
            viewModel.UpdateImages();
        }
    }

    private void GidImage_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (gidImageLeaveState) viewModel.ViewControls = true;
    }

    private void GidImage_PointerExited(object? sender, PointerEventArgs e)
    {
        gidImageLeaveState = viewModel.ViewControls;
        viewModel.ViewControls = false;
    }

    private void GidImage_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        double zoomFactorOffset = 1 + e.Delta.Y * zoomSpeed;

        SetZoom(new Point(), zoomFactorOffset, e.GetPosition(imgCurrent));
    }

    private void GidImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        InputElement element = (InputElement)sender!;
        element.PointerMoved += GidImage_PointerMove;

        lastMouseToImagePos = e.GetPosition(element);

        if (e.ClickCount == 2) viewModel.ResetZoom();

        imgCurrent.Focus();
        didMove = false;
    }

    private void GidImage_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        InputElement element = (InputElement)sender!;
        element.PointerMoved -= GidImage_PointerMove;

        if (!didMove) viewModel.ViewControls = !viewModel.ViewControls;
    }

    private void GidImage_PointerMove(object? sender, PointerEventArgs e)
    {
        InputElement element = (InputElement)sender!;

        if (!e.Pointer.IsPrimary)
        {
            element.PointerMoved -= GidImage_PointerMove;
            return;
        }

        Point currentPos = e.GetPosition(element);
        double deltaX = (lastMouseToImagePos.X - currentPos.X);
        double deltaY = (lastMouseToImagePos.Y - currentPos.Y);

        SetZoom(new Point(deltaX, deltaY), 1, currentPos);
        lastMouseToImagePos = currentPos;

        didMove = true;
    }

    private void GidImage_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        SetZoom(new Point(), 1, new Point());
    }

    private void SetZoom(Point pixelOffset, double zoomFactorOffset, Point pixelZoomPoint)
    {
        FileSystemImage? fileImage = viewModel.CurrentImage;
        IImage? img = fileImage?.Image;

        if (img == null) return;

        Rect rect = viewModel.CropRect ?? new Rect(0, 0, img.Size.Width, img.Size.Height);
        double zoomFactor = Math.Max(img.Size.Width / rect.Width, img.Size.Height / rect.Height);

        if (zoomFactor * zoomFactorOffset > maxZoomFactor) return;

        double pixelOffsetX = pixelOffset.X / imgCurrent.Bounds.Width;
        double pixelOffsetY = pixelOffset.Y / imgCurrent.Bounds.Height;

        double pixelZoomPointX = rect.X + pixelZoomPoint.X / imgCurrent.Bounds.Width * rect.Width;
        double pixelZoomPointY = rect.Y + pixelZoomPoint.Y / imgCurrent.Bounds.Height * rect.Height;

        viewModel.CropRect = Zoom(gidImage.Bounds.Width / gidImage.Bounds.Height, rect,
            new Point(pixelOffsetX, pixelOffsetY), zoomFactorOffset, new Point(pixelZoomPointX, pixelZoomPointY));

        // force image to recalculate possible size
        imgCurrent.Margin = imgCurrent.Margin.Left > 0 ? new Thickness() : new Thickness(0.1);
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

    private void GidControls_PointerEntered(object? sender, PointerEventArgs e)
    {
        gidControlsEnterState = viewModel.ViewControls;
        viewModel.ViewControls = true;
    }

    private void GidControls_PointerExited(object? sender, PointerEventArgs e)
    {
        if (!gidControlsEnterState) viewModel.ViewControls = false;
    }

    private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.UpdateImages(-1);
    }

    private void BtnNext_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.UpdateImages(1);
    }

    private async void CbxBackgroundColor_OnDataContextChanged(object? sender, EventArgs e)
    {
        // Workaround to auto select first element in combo box
        while (cbxBackgroundColor.Items.Count == 0) await Task.Delay(100);
        cbxBackgroundColor.SelectedIndex = 0;
    }
}