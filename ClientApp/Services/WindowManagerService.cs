using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using ClientApp.Helpers;
using ClientApp.ViewModels;
using ClientApp.Views;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;

namespace ClientApp.Services;

public class WindowManagerService
{
    private readonly Dictionary<string, AppWindow> _activeWindows = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly NavigationService _navigationService;

    public WindowManagerService(IServiceProvider serviceProvider, NavigationService navigationService)
    {
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;
    }

    public void RegisterWindow(string windowName, AppWindow window)
    {
        _activeWindows.TryAdd(windowName, window);
        window.Closed += (_, _) => UnregisterWindow(windowName);
    }

    public void UnregisterWindow(string windowName)
    {
        _activeWindows.Remove(windowName);
    }

    public AppWindow? GetActiveWindow(string windowName)
    {
        return _activeWindows.GetValueOrDefault(windowName);
    }

    public void CloseWindow(string windowName)
    {
        var window = GetActiveWindow(windowName);
        UnregisterWindow(windowName);
        window?.Close();
    }

    public void CloseWindow<T>(string windowName, T arg)
    {
        var window = GetActiveWindow(windowName);
        UnregisterWindow(windowName);
        window?.Close(arg);
    }

    public void OpenMainWindow(string windowName = "MainWindow", string frameName = "MainFrame")
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainWindowViewModel;
        mainWindow.InitializeComponent();
        RegisterWindow(windowName, mainWindow);
        if (mainWindow.FindControl<Frame>("MainFrame") is { } mFrame)
        {
            _navigationService.RegisterFrame(mainWindow, mFrame, frameName);
            _navigationService.NavigateTo<UserDashBoardViewModel>(mainWindow, frameName);
        }

        mainWindow.Show();
    }

    public void OpenMainWindowAuthAsDialog(string mainWindowName = "MainWindow",
        string mainFrameName = "MainFrame", string authWindowName = "AuthWindow",
        string authFrameName = "AuthFrame", string userDashBoardFrameName = "DashBoardFrame")
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainWindowViewModel;
        mainWindow.InitializeComponent();

        mainWindow.SplashScreen = new CustomSplashScreen();
        RegisterWindow(mainWindowName, mainWindow);

        if (mainWindow.FindControl<Frame>(mainFrameName) is { } mainFrame)
        {
            _navigationService.RegisterFrame(mainWindow, mainFrame, mainFrameName);
            _navigationService.NavigateTo<UserDashBoardViewModel>(mainWindow, mainFrameName);
        }

        mainWindow.Show();

        mainWindow.Loaded += async (_, _) =>
        {
            // Check if navigation is already done to avoid multiple calls
            if (mainWindow.MainFrame.Content is not UserDashBoardView)
            {
                return;
            }

            if (mainWindow.MainFrame.Content is UserDashBoardView dashBoardView &&
                dashBoardView.FindControl<Frame>(userDashBoardFrameName) is { } dashBoardFrame)
            {
                _navigationService.RegisterFrame(mainWindow, dashBoardFrame, userDashBoardFrameName);
            }

            await Task.Delay(900);

            mainWindow.Effect = new BlurEffect
            {
                Radius = 10
            };

            var authWindow = _serviceProvider.GetRequiredService<AuthWindow>();
            var authWindowViewModel = _serviceProvider.GetRequiredService<AuthWindowViewModel>();
            authWindow.DataContext = authWindowViewModel;
            authWindow.InitializeComponent();
            RegisterWindow(authWindowName, authWindow);

            if (authWindow.FindControl<Frame>(authFrameName) is { } authFrame)
            {
                _navigationService.RegisterFrame(authWindow, authFrame, authFrameName);
                _navigationService.NavigateTo<LoginViewModel>(authWindow, authFrameName);
            }

            var result = await authWindow.ShowDialog<bool>(mainWindow);

            if (!result)
            {
                mainWindow.Close();
            }
            else
            {
                _navigationService.NavigateTo<HomeViewModel>(mainWindow, userDashBoardFrameName);
                mainWindow.Effect = null;
            }
        };
    }


    public void OpenAuthWindow(string windowName = "AuthWindow", string frameName = "AuthFrame")
    {
        var authWindow = _serviceProvider.GetRequiredService<AuthWindow>();
        var authWindowViewModel = _serviceProvider.GetRequiredService<AuthWindowViewModel>();
        authWindow.DataContext = authWindowViewModel;
        authWindow.InitializeComponent();
        RegisterWindow(windowName, authWindow);
        if (authWindow.FindControl<Frame>("AuthFrame") is { } frame)
        {
            _navigationService.RegisterFrame(authWindow, frame, frameName);
            _navigationService.NavigateTo<LoginViewModel>(authWindow, frameName);
        }

        authWindow.SplashScreen = new CustomSplashScreen();
        authWindow.Show();
    }

    public async Task OpenAuthWindowAsDialogBaseAsync(string baseWindowName, string windowName = "AuthWindow",
        string frameName = "AuthFrame", string userDashBoardFrameName = "DashBoardFrame")
    {
        var currentWindow = _serviceProvider.GetRequiredService<MainWindow>();


        currentWindow.Effect = new BlurEffect
        {
            Radius = 10
        };

        var authWindow = _serviceProvider.GetRequiredService<AuthWindow>();
        var authWindowViewModel = _serviceProvider.GetRequiredService<AuthWindowViewModel>();
        authWindow.DataContext = authWindowViewModel;
        authWindow.InitializeComponent();
        RegisterWindow(windowName, authWindow);
        if (authWindow.FindControl<Frame>("AuthFrame") is { } frame)
        {
            _navigationService.RegisterFrame(authWindow, frame, frameName);
            _navigationService.NavigateTo<LoginViewModel>(authWindow, frameName);
        }

        var authWindowOpen = false;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            authWindowOpen = desktop.Windows.OfType<AuthWindow>().Any(w => w.IsVisible);
        }
        if (!authWindowOpen)
        {
            _navigationService.NavigateTo<PlaceHolderViewModel>(currentWindow, userDashBoardFrameName );
            var result = await authWindow.ShowDialog<bool>(currentWindow);
            if (!result)
            {
                currentWindow.Close();
            }
            else
            {
                _navigationService.NavigateTo<HomeViewModel>(currentWindow, userDashBoardFrameName );
                currentWindow.Effect = null;
            }
        }
    }

    public void OpenAuthWindowAsDialogBase(string baseWindowName, string windowName = "AuthWindow", string frameName = "AuthFrame")
    {
        var authWindow = _serviceProvider.GetRequiredService<AuthWindow>();
        var authWindowViewModel = _serviceProvider.GetRequiredService<AuthWindowViewModel>();
        authWindow.DataContext = authWindowViewModel;
        authWindow.InitializeComponent();
        RegisterWindow(windowName, authWindow);
        if (authWindow.FindControl<Frame>("AuthFrame") is { } frame)
        {
            _navigationService.RegisterFrame(authWindow, frame, frameName);
            _navigationService.NavigateTo<LoginViewModel>(authWindow, frameName);
        }

        var currentWindow = GetActiveWindow(baseWindowName);
        if (currentWindow != null) authWindow.ShowDialog(currentWindow);
    }

    public async Task<string?> OpenQrWindowAsDialog(string windowName = "QrWindow")
    {
        var qrScannerWindow = _serviceProvider.GetRequiredService<QrScannerWindow>();
        var qrScannerWindowViewModel = _serviceProvider.GetRequiredService<QrScannerWindowViewModel>();
        qrScannerWindow.DataContext = qrScannerWindowViewModel;
        qrScannerWindow.InitializeComponent();
        RegisterWindow(windowName, qrScannerWindow);
        var currentWindow = WindowHelper.Get();
        if (currentWindow != null) return await qrScannerWindow.ShowDialog<string>(currentWindow);

        return null;
    }

    public void OpenQrGeneratorWindowAsDialog(string transactionRef, string windowName = "QrGenWindow")
    {
        var qrGeneratorWindow = _serviceProvider.GetRequiredService<QrGeneratorWindow>();
        var qrGeneratorWindowViewModel = _serviceProvider.GetRequiredService<QrGeneratorWindowViewModel>();
        qrGeneratorWindowViewModel.TransactionRef = transactionRef;
        qrGeneratorWindow.DataContext = qrGeneratorWindowViewModel;
        qrGeneratorWindow.InitializeComponent();
        RegisterWindow(windowName, qrGeneratorWindow);
        var currentWindow = WindowHelper.Get();
        if (currentWindow != null)  _ = qrGeneratorWindow.ShowDialog(currentWindow);
    }

    public async Task OpenQrWindowAsDialogBase(string baseWindowName, string windowName = "QrWindow")
    {
        var qrScannerWindow = _serviceProvider.GetRequiredService<QrScannerWindow>();
        var qrScannerWindowViewModel = _serviceProvider.GetRequiredService<QrScannerWindowViewModel>();
        qrScannerWindow.DataContext = qrScannerWindowViewModel;
        qrScannerWindow.InitializeComponent();
        RegisterWindow(windowName, qrScannerWindow);
        var currentWindow = GetActiveWindow(baseWindowName);
        if (currentWindow != null) await qrScannerWindow.ShowDialog(currentWindow);
    }

    public void OpenCustomerWindow(string windowName = "CustomerWindow")
    {
        var customerWindow = _serviceProvider.GetRequiredService<CustomerWindow>();
        var customerViewModel = _serviceProvider.GetRequiredService<CustomerWindowViewModel>();
        customerWindow.DataContext = customerViewModel;
        customerWindow.InitializeComponent();
        RegisterWindow(windowName, customerWindow);

        customerWindow.Show();
    }
}