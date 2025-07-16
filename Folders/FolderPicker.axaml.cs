using System;
using System.Collections.Generic;
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

    private bool withSubFolderSelection;
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

    public Folder Folder
    {
        get => GetValue(FolderProperty);
        set => SetValue(FolderProperty, value);
    }

    public FolderPicker()
    {
        InitializeComponent();
        
        fileExplorer = FileExplorerHelper.GetFileExplorer();
        btnOpen.IsVisible = fileExplorer != null;

        cbxSubFolder.IsVisible = withSubFolderSelection = true;
        FolderProperty.Changed.AddClassHandler<FolderPicker>(OnFolderChanged);
    }

    private static void OnFolderChanged(FolderPicker sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not Folder folder) return;

        sender.tbxPath.Text = folder.Path;
        sender.cbxSubFolder.IsChecked = folder.SubType == SubfolderType.All;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        btnChange.IsVisible = topLevel.StorageProvider.CanOpen;
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

    private async void BtnChange_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        IReadOnlyList<IStorageFolder> folder = await topLevel
            .StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        if (folder.Count > 0) Folder = Folder with { Path = folder[0].Path.AbsolutePath };
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