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
                    ErrorText.Text = "Invalid username or password!";
                    ErrorText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception)
            {
                ErrorText.Text = "Server connection error occured.";
                ErrorText.Visibility = Visibility.Visible;
            }
            finally
            {
                LoginBtn.IsEnabled = true;
            }
        }

        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}