﻿using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.System;
using System.Numerics;
using Woop.ViewModels;
using Woop.Models;
using Woop.Services;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Woop.Views
{
    public sealed partial class MainPage : Page
    {
        private CoreApplicationViewTitleBar _coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
        private readonly SettingsService _settingsService;

        public double CoreTitleBarHeight => _coreTitleBar.Height;

        public Thickness CoreTitleBarPadding
        {
            get
            {
                if (FlowDirection == FlowDirection.LeftToRight)
                {
                    return new Thickness { Left = _coreTitleBar.SystemOverlayLeftInset, Right = _coreTitleBar.SystemOverlayRightInset };
                }
                else
                {
                    return new Thickness { Left = _coreTitleBar.SystemOverlayRightInset, Right = _coreTitleBar.SystemOverlayLeftInset };
                }
            }
        }

        public MainViewModel ViewModel { get; private set; }

        public MainPage()
        {
            _settingsService = new SettingsService();
            _settingsService.ApplicationThemeChanged += OnApplicationThemeChanged;
            ViewModel = new MainViewModel(Dispatcher, _settingsService);

            RequestedTheme = _settingsService.ApplicationTheme;

            InitializeComponent();

            Window.Current.SetTitleBar(TitleBar);

            Selector.Translation += new Vector3(0, 0, 32);

            ApplicationView.PreferredLaunchViewSize = new Size(480, 480);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            SetTitleBarColors();
        }

        private void SetTitleBarColors()
        {
            // todo resources color are not correct when theme changes. hardcode values for each theme?

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;

            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            titleBar.ButtonHoverBackgroundColor = (App.Current.Resources["TitleBarButtonPointerOverBackground"] as SolidColorBrush).Color;
            titleBar.ButtonPressedBackgroundColor = (App.Current.Resources["TitleBarButtonPressedBackground"] as SolidColorBrush).Color;

            titleBar.ButtonForegroundColor = (Color)Resources["SystemBaseHighColor"];
            titleBar.ButtonHoverForegroundColor = (Color)Resources["SystemBaseHighColor"];
            titleBar.ButtonPressedForegroundColor = (Color)Resources["SystemBaseHighColor"];
        }

        private async void OnApplicationThemeChanged(object sender, ElementTheme e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                RequestedTheme = e;
                SetTitleBarColors();
            });
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.InitializeAsync();
            Buffer.Focus(FocusState.Programmatic);
        }

        private void Query_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                ViewModel.ClosePicker();
                Buffer.Focus(FocusState.Programmatic);
            } 
            else if (e.Key == VirtualKey.Enter)
            {
                ViewModel.RunSelectedScript();
                Buffer.Focus(FocusState.Programmatic);
            }
            else if (e.Key == VirtualKey.Up)
            {
                ViewModel.SelectPrevious();
            }
            else if (e.Key == VirtualKey.Down)
            {
                ViewModel.SelectNext();
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Scripts.ScrollIntoView(ViewModel.SelectedScript);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(null);
            _settingsService.ApplicationThemeChanged -= OnApplicationThemeChanged;
            ViewModel = null;
            _coreTitleBar = null;
        }

        private void Buffer_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Buffer.SelectedText))
            {
                ViewModel.Selection = new Selection
                {
                    Start = Buffer.SelectionStart,
                    Length = Buffer.SelectionLength,
                    Content = Buffer.SelectedText
                };
            }
            else
            {
                ViewModel.Selection = null;
            }
        }

        private async void OnSettingsTapped(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsDialog(_settingsService);
            await settings.ShowAsync();
        }
    }
}