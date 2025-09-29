using Newtonsoft.Json;
using SonorousDAB.Helpers;
using SonorousDAB.Views;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SonorousDAB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool isLoggedIn = true;
        private bool isPlaying = false;

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public MainWindow()
        {

            InitializeComponent();
            RestoreSessionCookie();
            playbackTimer.Interval = TimeSpan.FromMilliseconds(500);
            playbackTimer.Tick += PlaybackTimer_Tick;

            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            this.Loaded += MainWindow_Loaded;
            UpdateAuthenticationUi();

        }

        private void RestoreSessionCookie()
        {
            var sessionValue = Properties.Settings.Default.SessionCookie;
            if (!string.IsNullOrEmpty(sessionValue))
            {
                LoginData.cookieJar.Add(new Uri("https://dabmusic.xyz"), new Cookie("session", sessionValue));
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            int attributeValue = 1; // 1 for dark mode, 0 for light mode
            DwmApi.DwmSetWindowAttribute(windowHandle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref attributeValue, sizeof(int));
            CheckForUpdates();
            if (mediaPlayer != null && volumeSlider != null)
            {
                mediaPlayer.Volume = volumeSlider.Value;
            }

        }

        private async Task CheckForUpdates()
        {
            string apiUrl = "https://api.github.com/repos/LazymanzoidOfficial/Sonorous/releases/latest";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Sonorous");

                try
                {
                    var response = await client.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        dynamic? release = JsonConvert.DeserializeObject(json);
                        string? latestVersion = release?.tag_name;

                        if (latestVersion != AppInfo.CurrentVersion)
                        {
                            var result = MessageBox.Show($"New version available: {latestVersion}\nDo you want to download it?", "Update available!", MessageBoxButton.YesNo, MessageBoxImage.Information);
                            if (result == MessageBoxResult.Yes)
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = "https://github.com/LazymanzoidOfficial/Sonorous/releases/latest",
                                    UseShellExecute = true
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Optional: log or ignore
                }
            }
        }

        private void PlaybackTimer_Tick(object? sender, EventArgs e)
        {
            if (!isSeeking && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                double current = mediaPlayer.Position.TotalSeconds;
                double total = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;

                seekBar.Value = current;

                elapsedTime.Text = mediaPlayer.Position.ToString(@"m\:ss");
                totalTime.Text = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"m\:ss");
            }
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            playbackTimer.Stop();
            seekBar.Value = 0;
            elapsedTime.Text = "0:00";
        }

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (!mediaPlayer.NaturalDuration.HasTimeSpan) return;
            seekBar.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            totalTime.Text = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"m\:ss");
            playbackTimer.Start();
        }

        private readonly DispatcherTimer playbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        private bool isSeeking = false;
        // Loading blurbs to show while waiting for API responses
        // These are unused for now but can be expanded later
        private readonly string[] blurbs = new[]
        {
            "yeah this is unused for now",
        };


        private readonly DabMusicAPI _api = new();

        private async void Button_Click(object? sender, RoutedEventArgs? routedEventArgs)
        {
            try
            {
                // unused blurb selection for now
                loadingOverlay.Visibility = Visibility.Visible;
                //loadingBlurb.Text = blurbs[new Random().Next(blurbs.Length)];


                resultsList.ItemsSource = null;
                if (search.Text == "")
                {
                    MessageBox.Show("Please enter a search term.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    try
                    {
                        var query = search.Text;
                        var results = await _api.SearchTracksAsync(query); // Your API call
                        resultsList.ItemsSource = results;
                    }
                    finally
                    {
                        loadingOverlay.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string?> GetStreamUrlAsync(string trackId)
        {
            using var client = new HttpClient();
            var response = await LoginData.client.GetAsync($"https://dabmusic.xyz/api/stream?trackId={trackId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var obj = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);
            return obj.GetProperty("url").GetString();
        }

        private async void PlayStreamAsync(Track selectedTrack)
        {
            try
            {
                seekBar.Value = 0;
                elapsedTime.Text = "0:00";
                totalTime.Text = "--:--";

                playerTitle.Text = "Loading...";
                playerArtist.Text = "";
                playerHiRes.Visibility = Visibility.Collapsed;

                string? streamUrl = selectedTrack.ResolvedStreamUrl;

                if (string.IsNullOrEmpty(streamUrl))
                {
                    streamUrl = await GetStreamUrlAsync(selectedTrack.Id);
                    selectedTrack.ResolvedStreamUrl = streamUrl;
                }

                if (string.IsNullOrEmpty(streamUrl))
                {
                    MessageBox.Show("Failed to get stream URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                mediaPlayer.Source = new Uri(streamUrl);
                PlayBtn.Content = "⏸";
                isPlaying = true;
                mediaPlayer.Play();

                playerTitle.Text = selectedTrack.Title;
                playerArtist.Text = selectedTrack.Artist;
                playerAlbumCover.Source = new BitmapImage(new Uri(selectedTrack.AlbumCover));
                playerHiRes.Visibility = selectedTrack.IsHiRes ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
           

        }



        private async void AuthButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                if (authButton.Content.ToString() == "Login")
                {
                    // LoginWindow handles the login process and returns the email and username on success
                    var loginWindow = new LoginWindow { Owner = this };
                    if (loginWindow.ShowDialog() == true)
                    {
                        Properties.Settings.Default.ApiEmail = loginWindow.AuthEmail;
                        Properties.Settings.Default.ApiUsername = loginWindow.AuthUsername;
                        Properties.Settings.Default.Save();

                        MessageBox.Show("Logged in successfully!", "Sonorous", MessageBoxButton.OK, MessageBoxImage.Information);
                        isLoggedIn = true;
                        UpdateAuthenticationUi();
                    }
                }
                else
                {
                    try
                    {
                        // Makes a POST call to /auth/logout to invalidate the session on the server, if successful, clear local state
                        var response = await LoginData.client.PostAsync("https://dabmusic.xyz/api/auth/logout", null);

                        if (response.IsSuccessStatusCode)
                        {
                            // Clear local auth state
                            Properties.Settings.Default.ApiEmail = "";
                            Properties.Settings.Default.ApiUsername = "";
                            Properties.Settings.Default.SessionCookie = null;
                            Properties.Settings.Default.Save();

                            isLoggedIn = false;

                            // Wipe cookie from container
                            var uri = new Uri("https://dabmusic.xyz");
                            var cookies = LoginData.cookieJar.GetCookies(uri);
                            var session = cookies["session"];
                            if (session != null)
                            {
                                session.Expired = true;
                            }

                            UpdateAuthenticationUi();
                            MessageBox.Show("Logged out successfully", "Sonorous", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Logout failed: {response.StatusCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error during logout: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateAuthenticationUi()
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ApiUsername))
            {
                usernameLabel.Text = $"Logged in as {Properties.Settings.Default.ApiUsername}";
                authButton.Content = "Logout";
                isLoggedIn = true;
            }
            else
            {
                usernameLabel.Text = "";
                authButton.Content = "Login";
                isLoggedIn = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            UpdateAuthenticationUi();

        }
        private void search_KeyDown(object sender, KeyEventArgs e)
        {
            if (Key.Enter == e.Key)
            {
                Button_Click(null, null);
            }
        }

        private async void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            try
            {
                if (resultsList.SelectedItem is Track selectedTrack)
                {
                    await Task.Run(() => Dispatcher.Invoke(() => PlayStreamAsync(selectedTrack)));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                switch (isPlaying)
                {
                    case true:
                        mediaPlayer.Pause();
                        isPlaying = false;
                        PlayBtn.Content = "▶";
                        break;
                    case false:
                        mediaPlayer.Play();
                        isPlaying = true;
                        PlayBtn.Content = "⏸";
                        break;
                }
            }
        }

        private void ShowLoading(string blurb = "")
        {
            loadingOverlay.Visibility = Visibility.Visible;
            loadingBlurb.Text = blurb;
        }

        private void HideLoading()
        {
            loadingOverlay.Visibility = Visibility.Collapsed;
            loadingBlurb.Text = "";
        }

        private void mediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
           
        }

        private void mediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
           
        }

        private void SeekBar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            mediaPlayer.Position = TimeSpan.FromSeconds(seekBar.Value);
            isSeeking = false;
        }

        private void SeekBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isSeeking = true;
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer != null)
            {
                int volPercentRounded = (int)Math.Round(volumeSlider.Value);
                VolPercent.Content = $"{volPercentRounded}%";
                mediaPlayer.Volume = volumeSlider.Value / 100.0;
            }
        }

        private async Task<Track[]> FetchFavoritesAsync()
        {
            // you really don't know how long it took to make favorites work :(
            var response = await LoginData.client.GetAsync("https://dabmusic.xyz/api/favorites");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var favoritesJson = doc.RootElement.GetProperty("favorites").GetRawText();

            var tracks = JsonConvert.DeserializeObject<List<Track>>(favoritesJson);
            return tracks?.ToArray() ?? [];
        }


        private async void FavButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.ApiEmail))
                {
                    resultsList.ItemsSource = null;
                    ShowLoading("Fetching your favorite tracks...");

                    try
                    {
                        var favorites = await FetchFavoritesAsync();
                        resultsList.ItemsSource = favorites;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to load favorites: " + ex.Message + " You are not logged into your DAB Music account.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        HideLoading();
                    }
                    return;
                }

                resultsList.ItemsSource = null;
                ShowLoading("Fetching your favorite tracks...");

                try
                {
                    var favorites = await FetchFavoritesAsync();
                    resultsList.ItemsSource = favorites;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load favorites: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    HideLoading();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // these 3 buttons were originally used for a custom title bar, these are not used anymore but might be useful later
        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        bool isMaximized = false;
        private void MaxWinBtn_Click(object sender, RoutedEventArgs e)
        {
            // i thought switches looked cooler than if statements
            switch (isMaximized)
            {
                case true:
                    this.WindowState = WindowState.Normal;
                    isMaximized = false;
                    break;
                case false:
                    this.WindowState = WindowState.Maximized;
                    isMaximized = true;
                    break;
            }
        }

        private void MinBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}