using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PictureView.Folders.FileExplorer;

namespace PictureView.Folders;

public partial class FolderPicker : UserControl
{
    public static readonly StyledProperty<Folder> FolderProperty =
        AvaloniaProperty.Register<FolderPicker, Folder>(nameof(Folder), new Folder("", SubfolderType.This));

    public static readonly StyledProperty<string[]> FilesProperty =
        AvaloniaProperty.Register<FolderPicker, string[]>(nameof(Folder), Array.Empty<string>());

    private bool canOpen, withSubFolderSelection, withSelectFilesButton;
    private readonly IFileExplorer? fileExplorer;

    public bool WithSubFolderSelection
    {
        get => withSubFolderSelection;
        set
        {
            withSubFolderSelection = value;
            cbxSubFolder.IsVisible = value;
        }
    }

    public bool WithSelectFilesButton
    {
        get => withSelectFilesButton;
        set
        {
            withSelectFilesButton = value;
            btnSelectFiles.IsVisible = canOpen && value;
        }
    }

    public Folder Folder
    {
        get => GetValue(FolderProperty);
        set => SetValue(FolderProperty, value);
    }

    public string[] Files
    {
        get => GetValue(FilesProperty);
        set => SetValue(FilesProperty, value);
    }

    static FolderPicker()
    {
        FolderProperty.Changed.AddClassHandler<FolderPicker>(OnFolderChanged);
        FilesProperty.Changed.AddClassHandler<FolderPicker>(OnFilesChanged);
    }

    public FolderPicker()
    {
        InitializeComponent();

        fileExplorer = FileExplorerHelper.GetFileExplorer();
        btnOpen.IsVisible = fileExplorer != null;

        cbxSubFolder.IsVisible = withSubFolderSelection = true;
    }

    private static void OnFolderChanged(FolderPicker sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not Folder folder) return;

        sender.tbxPath.Text = folder.Path;
        sender.cbxSubFolder.IsChecked = folder.SubType == SubfolderType.All;
    }

    private static void OnFilesChanged(FolderPicker sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not string[] files) return;

        if (files.Length == 0)
        {
            sender.tblFiles.IsVisible = false;
            sender.tbxPath.IsVisible = true;
        }
        else if (files.Length == 1)
        {
            sender.Folder = sender.Folder with { Path = files[0] };
            sender.tblFiles.IsVisible = false;
            sender.tbxPath.IsVisible = true;
        }
        else
        {
            sender.tblFiles.Text = $"{files.Length} File(s)";
            sender.tblFiles.IsVisible = true;
            sender.tbxPath.IsVisible = false;
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        canOpen = topLevel.StorageProvider.CanOpen;
        btnSelectFolder.IsVisible = canOpen;
        btnSelectFiles.IsVisible = canOpen && withSelectFilesButton;
    }

    private void TbxPath_TextChanged(object? sender, TextChangedEventArgs e)
    {
        TextBox? tbx = sender as TextBox;
        if (tbx?.Text != null) Folder = Folder with { Path = tbx.Text };
    }

    private void CbxSubfolder_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cbx) return;

        SubfolderType subType = cbx.IsChecked == true ? SubfolderType.All : SubfolderType.This;
        Folder = Folder with { SubType = subType };
    }

    private async void BtnSelectFolder_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        IReadOnlyList<IStorageFolder> folder = await topLevel
            .StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        if (folder.Count > 0)
        {
            Folder = Folder with { Path = folder[0].Path.LocalPath };
            Files = Array.Empty<string>();
        }
    }

    private async void BtnSelectFiles_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        IReadOnlyList<IStorageFile> files = await topLevel
            .StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("All Images")
                    {
                        Patterns = new string[]
                        {
                            "*.jpg",
                            "*.jpeg",
                            "*.jpe",
                            "*.gif",
                            "*.tiff",
                            "*.ico",
                            "*.png",
                            "*.bmp",
                        },
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new string[] { "*" },
                    },
                },
            });

        Files = files.Select(f => f.Path.LocalPath).ToArray();
    }

    private void BtnOpen_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            fileExplorer?.Open(Folder.Path);
        }
        catch (Exception exc)
        {
            System.Diagnostics.Debug.WriteLine(exc);
        }
    }
}