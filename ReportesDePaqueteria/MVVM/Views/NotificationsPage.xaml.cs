using System;
using Microsoft.Maui.Controls;
using ReportesDePaqueteria.MVVM.ViewModels;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class NotificationsPage : ContentPage
    {
        private NotificationViewModel? _vm;

        public NotificationsPage(NotificationViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_vm is null) return;

            await _vm.StartListeningAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _vm?.StopListening();
        }

        private async void OnMarkAllReadClicked(object sender, EventArgs e)
        {
            if (_vm is null) return;
            await _vm.MarkAllReadAsync();
        }

        private async void OnClearAllClicked(object sender, EventArgs e)
        {
            if (_vm is null) return;
            await _vm.ClearAllAsync();
        }

        private async void OnMarkReadSwipe(object sender, EventArgs e)
        {
            if (_vm is null) return;
            if (sender is SwipeItem si && si.BindingContext is Models.NotificationModel n)
                await _vm.MarkReadAsync(n);
        }

        private async void OnDeleteSwipe(object sender, EventArgs e)
        {
            if (_vm is null) return;
            if (sender is SwipeItem si && si.BindingContext is Models.NotificationModel n)
                await _vm.DeleteAsync(n);
        }

        private async void OnOpenSwipe(object sender, EventArgs e)
        {
            if (_vm is null) return;
            if (sender is SwipeItem si && si.BindingContext is Models.NotificationModel n)
                await _vm.OpenAsync(n);
        }
    }
}
