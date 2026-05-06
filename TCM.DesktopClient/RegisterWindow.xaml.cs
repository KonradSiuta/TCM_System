using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Media;
using TCM.Shared.Models;

namespace TCM.DesktopClient
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(UsernameTxt.Text) || string.IsNullOrWhiteSpace(EmailTxt.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ShowStatus("Fill all required fields.", Colors.Red);
                return;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                ShowStatus("Passwords are not identical.", Colors.Red);
                return;
            }

            RegisterBtn.IsEnabled = false;

            var registerDto = new UserRegisterDto
            {
                Username = UsernameTxt.Text,
                Email = EmailTxt.Text,
                Password = PasswordBox.Password
            };

            try
            {
                using var client = new HttpClient { BaseAddress = new Uri("https://localhost:7243/") };
                var response = await client.PostAsJsonAsync("api/Auth/register", registerDto);

                if (response.IsSuccessStatusCode)
                {
                    ShowStatus("Account has been succesully created.", Colors.Green);
                    UsernameTxt.Text = ""; EmailTxt.Text = ""; PasswordBox.Password = ""; ConfirmPasswordBox.Password = "";
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    ShowStatus($"Błąd: {errorMsg}", Colors.Red);
                }
            }
            catch (Exception)
            {
                ShowStatus("Server connection error occured..", Colors.Red);
            }
            finally
            {
                RegisterBtn.IsEnabled = true;
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ShowStatus(string message, Color color)
        {
            StatusText.Text = message;
            StatusText.Foreground = new SolidColorBrush(color);
            StatusText.Visibility = Visibility.Visible;
        }
    }
}