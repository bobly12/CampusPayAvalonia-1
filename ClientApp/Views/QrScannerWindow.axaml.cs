using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClientApp.Helpers;
using ClientApp.ViewModels;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;

namespace ClientApp.Views;

public partial class QrScannerWindow : AppWindow
{
    public QrScannerWindow()
    {
        InitializeComponent();
        TitleBar.ExtendsContentIntoTitleBar = true;
        Closed += (_, _) =>
        {
            if (DataContext is QrScannerWindowViewModel vm)
            {
                vm.OnClosed();
            }
        };
    }
}