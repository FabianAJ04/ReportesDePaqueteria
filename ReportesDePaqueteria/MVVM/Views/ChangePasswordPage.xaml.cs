using Microsoft.Maui.Controls;
using System;
using System.Text.RegularExpressions;

namespace ReportesDePaqueteria.MVVM.Views
{
    public partial class ChangePasswordPage : ContentPage
    {
        public bool OldHidden { get; set; } = true;
        public bool NewHidden { get; set; } = true;
        public bool New2Hidden { get; set; } = true;

        public string OldToggleText => OldHidden ? "Mostrar" : "Ocultar";
        public string NewToggleText => NewHidden ? "Mostrar" : "Ocultar";
        public string New2ToggleText => New2Hidden ? "Mostrar" : "Ocultar";

        public ChangePasswordPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        private void ShowError(string msg)
        {
            ErrorMsg.Text = msg;
            ErrorMsg.IsVisible = true;
        }
        private void ClearError() => ErrorMsg.IsVisible = false;

        private void OnToggleOld(object sender, EventArgs e)
        {
            OldHidden = !OldHidden;
            OldPwd.IsPassword = OldHidden;
            OnPropertyChanged(nameof(OldToggleText));
        }
        private void OnToggleNew(object sender, EventArgs e)
        {
            NewHidden = !NewHidden;
            NewPwd.IsPassword = NewHidden;
            OnPropertyChanged(nameof(NewToggleText));
        }
        private void OnToggleNew2(object sender, EventArgs e)
        {
            New2Hidden = !New2Hidden;
            NewPwd2.IsPassword = New2Hidden;
            OnPropertyChanged(nameof(New2ToggleText));
        }

        private void OnNewPwdTextChanged(object sender, TextChangedEventArgs e)
        {
            var score = PasswordScore(e.NewTextValue ?? string.Empty);
            StrengthBar.Progress = score.progress;
            StrengthText.Text = score.label;
        }

        private (double progress, string label) PasswordScore(string s)
        {
            if (string.IsNullOrEmpty(s)) return (0, "Débil");

            int pts = 0;
            if (s.Length >= 8) pts++;
            if (s.Length >= 12) pts++;
            if (Regex.IsMatch(s, "[A-Z]")) pts++;
            if (Regex.IsMatch(s, "[a-z]")) pts++;
            if (Regex.IsMatch(s, "[0-9]")) pts++;
            if (Regex.IsMatch(s, "[^A-Za-z0-9]")) pts++;

            if (pts <= 2) return (0.33, "Débil");
            if (pts <= 4) return (0.66, "Media");
            return (1.0, "Fuerte");
        }

        private async void OnUpdateClicked(object sender, EventArgs e)
        {
            ClearError();

            var oldPwd = OldPwd.Text?.Trim() ?? "";
            var newPwd = NewPwd.Text?.Trim() ?? "";
            var newPwd2 = NewPwd2.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(oldPwd))
            {
                ShowError("Ingresa tu contraseña actual.");
                return;
            }
            if (string.IsNullOrEmpty(newPwd) || newPwd.Length < 8)
            {
                ShowError("La nueva contraseña debe tener al menos 8 caracteres.");
                return;
            }
            if (newPwd != newPwd2)
            {
                ShowError("La confirmación no coincide.");
                return;
            }
            if (newPwd == oldPwd)
            {
                ShowError("La nueva contraseña no puede ser igual a la actual.");
                return;
            }

            await DisplayAlert("Contraseña", "Contraseña actualizada correctamente.", "OK");
            await Shell.Current.GoToAsync("..");
        }

        private async void OnCloseClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("..");
    }
}
