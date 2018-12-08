using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace HideMenu
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad("{ADFC4E64-0397-11D1-9F4E-00A0C911004F}", PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad("{F1536EF8-92EC-443C-9ED7-FDADF150DA82}", PackageAutoLoadFlags.BackgroundLoad)]
    [Guid("efbf9aff-f52e-4e34-9bd0-13f0f01a50b8")]
    public sealed class HideMenuPackage : AsyncPackage
    {
        bool _isMenuVisible;
        FrameworkElement _menuContainer;
        Window _mainWindow;

        bool IsMenuVisible
        {
            get => _isMenuVisible;
            set
            {
                if (_isMenuVisible == value) return;
                
                _isMenuVisible = value;
                if (_menuContainer == null) return;
                
                if (_isMenuVisible) _menuContainer.ClearValue(FrameworkElement.HeightProperty);
                else _menuContainer.Height = 0.0;
            }
        }

        FrameworkElement MenuContainer
        {
            get => _menuContainer;
            set
            {
                if (_menuContainer != null) 
                    _menuContainer.IsKeyboardFocusWithinChanged -= OnMenuContainerFocusChanged;

                _menuContainer = value;
                if (_menuContainer == null) return;
                
                if (_isMenuVisible) _menuContainer.ClearValue(FrameworkElement.HeightProperty);
                else _menuContainer.Height = 0.0;

                _menuContainer.IsKeyboardFocusWithinChanged += OnMenuContainerFocusChanged;
            }
        }

        void OnMenuContainerFocusChanged(object sender, DependencyPropertyChangedEventArgs e)
            => IsMenuVisible = IsAggregateFocusInMenuContainer(MenuContainer);

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            EventManager.RegisterClassHandler(typeof(UIElement), UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(PopupLostKeyboardFocus));
        
            _mainWindow = Application.Current.MainWindow;
            if (_mainWindow != null) _mainWindow.LayoutUpdated += LayoutUpdated;
        }

        void LayoutUpdated(object sender, EventArgs e)
        {
            var foundMenuContainer = false;
            foreach (Menu item in _mainWindow.FindDescendants<Menu>())
            {
                if (AutomationProperties.GetAutomationId(item) != "MenuBar") continue;
            
                FrameworkElement candidate = item;
                DependencyObject parent = item.GetVisualOrLogicalParent();
            
                if (parent != null) 
                    candidate = parent.GetVisualOrLogicalParent() as DockPanel ?? candidate;

                foundMenuContainer = true;
                MenuContainer = candidate;
            }

            if (foundMenuContainer) _mainWindow.LayoutUpdated -= LayoutUpdated;
        }

        void PopupLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (IsMenuVisible && MenuContainer != null && !IsAggregateFocusInMenuContainer(MenuContainer))
                IsMenuVisible = false;
        }

        static bool IsAggregateFocusInMenuContainer(FrameworkElement menuContainer)
        {
            if (menuContainer.IsKeyboardFocusWithin) return true;

            for (var focused = (DependencyObject)Keyboard.FocusedElement; focused != null; focused = focused.GetVisualOrLogicalParent())
                if (focused == menuContainer) return true;

            return false;
        }
    }
}
