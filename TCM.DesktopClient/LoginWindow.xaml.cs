using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using TCM.Shared.Models;

namespace TCM.DesktopClient
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginBtn.IsEnabled = false;
            ErrorText.Visibility = Visibility.Collapsed;

            var loginDto = new UserLoginDto
            {
                Username = UsernameTxt.Text,
                Password = PasswordBox.Password
            };

            try
            {
                using var client = new HttpClient { BaseAddress = new Uri("https://localhost:7243/") };
                var response = await client.PostAsJsonAsync("api/Auth/login", loginDto);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

                    AppSession.JwtToken = result.Token;

                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    ErrorText.Text = "Błędna nazwa użytkownika lub hasło!";
                    ErrorText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception)
            {
                ErrorText.Text = "Brak połączenia z serwerem API.";
                ErrorText.Visibility = Visibility.Visible;
            }
            finally
            {
                LoginBtn.IsEnabled = true;
            }
        }
    }
}