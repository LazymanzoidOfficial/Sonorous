using Newtonsoft.Json;
using SonorousDAB.Helpers;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace SonorousDAB.Views
{
    public partial class LoginWindow : Window
    {
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public string AuthEmail { get; private set; }
        public string AuthUsername { get; private set; }

       

        public LoginWindow()
        {
            InitializeComponent();
            this.Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            int attributeValue = 1; // 1 for dark mode, 0 for light mode
            DwmApi.DwmSetWindowAttribute(windowHandle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref attributeValue, sizeof(int));
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            var email = usernameBox.Text;
            var password = passwordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter both email and password.");
                return;
            }

            var loginData = new { email, password };
            var content = new StringContent(JsonConvert.SerializeObject(loginData), Encoding.UTF8, "application/json");

            try
            {
                var response = await LoginData.client.PostAsync("https://dabmusic.xyz/api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(result);

                    AuthEmail = loginResponse.User.Email;
                    AuthUsername = loginResponse.User.Username;

                    var sessionCookie = LoginData.cookieJar.GetCookies(new Uri("https://dabmusic.xyz"))["session"];
                    if (sessionCookie != null)
                    {
                        Properties.Settings.Default.SessionCookie = sessionCookie.Value;
                        Properties.Settings.Default.Save();
                    }

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Login failed. Please check your credentials.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to server: {ex.Message}");
            }
        }

        public class LoginResponse
        {
            public string Message { get; set; }
            public UserInfo User { get; set; }
        }

        public class UserInfo
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public DateTime Created_At { get; set; }
        }

        private void passwordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Key.Enter == e.Key)
            {
                Login_Click(this, new RoutedEventArgs());
            }
        }

        private void usernameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Key.Enter == e.Key)
            {
                Login_Click(this, new RoutedEventArgs());
            }
        }
    }
}