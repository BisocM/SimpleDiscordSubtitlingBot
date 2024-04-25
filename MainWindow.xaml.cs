using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace SimpleDiscordSubtitlingBot
{
    public partial class MainWindow : Window
    {
        private const int SampleLengthMilliseconds = 100;
        private DiscordClient _client;
        private VoiceNextExtension _voiceNext;
        private OverlayWindow _overlayWindow;

        private ConcurrentDictionary<uint, SpeechRecognition> _recognizers = new();

        public MainWindow()
        {
            _overlayWindow = new OverlayWindow();

            InitializeComponent();
            fontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;
        }

        private void ToggleDarkMode(bool isDark)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);

            paletteHelper.SetTheme(theme);
        }

        private async Task InitializeClientAsync(string token)
        {
            var config = new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            _client = new DiscordClient(config);
            _voiceNext = _client.UseVoiceNext(new VoiceNextConfiguration
            {
                PacketQueueSize = SampleLengthMilliseconds,
                AudioFormat = new AudioFormat(16000, 1, VoiceApplication.LowLatency),
                EnableIncoming = true
            });

            _client.GuildDownloadCompleted += GuildDownloadCompleted;
            await _client.ConnectAsync();
            _overlayWindow.Show();
        }

        private Task GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
        {
            Dispatcher.Invoke(UpdateGuildList);
            return Task.CompletedTask;
        }

        private void UpdateGuildList()
        {
            var guilds = _client.Guilds.Values.Select(g => g.Name).ToList();
            lstGuilds.ItemsSource = guilds;
        }

        #region UI Interactions

        private async void InitializeBot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                initButton.IsEnabled = false;

                _overlayWindow = new OverlayWindow();
                await InitializeClientAsync(txtToken.Text);
                BotUser.Instance.DiscordBotToken = txtToken.Text;
                BotUser.Instance.DiscordClientId = _client.CurrentUser.Id.ToString();
                BotUser.Instance.MicrosoftCognitiveServicesKey = txtCognitiveServicesKey.Text;

                txtGuildSearch.IsEnabled = true;
                txtUserSearch.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialize bot: " + ex.Message);
                initButton.IsEnabled = true;
            }
        }

        private void TxtGuildSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (_client.Guilds.Count != 0)
            {
                var filtered = _client.Guilds.Values
                    .Where(g => g.Name.Contains(txtGuildSearch.Text, StringComparison.CurrentCultureIgnoreCase))
                    .Select(g => g.Name);
                lstGuilds.ItemsSource = filtered;
            }
            else
            {
                MessageBox.Show($"No guilds to search through!");
            }
        }

        private void LstGuilds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("Guild selection changed");
            if (lstGuilds.SelectedItem != null)
            {
                string selectedGuildName = lstGuilds.SelectedItem.ToString();
                var guild = _client.Guilds.Values.FirstOrDefault(g => g.Name == selectedGuildName);
                if (guild != null)
                {
                    var userDisplayList = guild.Members.Values
                        .Where(member => !member.IsBot)
                        .Select(member => $"{member.Username}#{member.Discriminator}")
                        .ToList();

                    Debug.WriteLine("Updating user list with count: " + userDisplayList.Count);
                    lstUsers.ItemsSource = userDisplayList;
                }
            }
        }

        private void TxtUserSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (lstGuilds.SelectedItem == null)
                return;

            string selectedGuildName = lstGuilds.SelectedItem.ToString();
            var guild = _client.Guilds.Values.FirstOrDefault(g => g.Name == selectedGuildName);
            if (guild != null)
            {
                var filtered = guild.Members.Values
                    .Where(member => member.Username.StartsWith(txtUserSearch.Text, StringComparison.OrdinalIgnoreCase))
                    .Select(member => $"{member.Username}#{member.Discriminator}");
                lstUsers.ItemsSource = filtered;
            }
        }

        private async void StartTranscribing_Click(object sender, RoutedEventArgs e)
        {
            if (lstGuilds.SelectedItem == null)
            {
                MessageBox.Show("Please select a guild.");
                return;
            }

            if (lstUsers.SelectedItem == null)
            {
                MessageBox.Show("Please select a user.");
                return;
            }

            string selectedGuildName = lstGuilds.SelectedItem.ToString();
            var guild = _client.Guilds.Values.FirstOrDefault(g => g.Name == selectedGuildName);
            if (guild == null)
            {
                MessageBox.Show("Guild not found.");
                return;
            }

            var user = guild.Members.Values.FirstOrDefault(m => $"{m.Username}#{m.Discriminator}" == lstUsers.SelectedItem.ToString() && !m.IsBot);
            if (user == null)
            {
                MessageBox.Show("User not found in the selected guild.");
                return;
            }

            var voiceState = user.VoiceState;
            if (voiceState?.Channel == null)
            {
                MessageBox.Show("User is not in a voice channel.");
                return;
            }

            //Make sure the user cannot change the list selections anymore.
            lstGuilds.IsEnabled = false;
            lstUsers.IsEnabled = false;

            DiscordChannel voiceChannel = voiceState.Channel;
            VoiceNextConnection connection = await voiceChannel.ConnectAsync();
            _overlayWindow.Show();

            ManageSpeechRecognition(connection);
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue && _overlayWindow != null)
            {
                _overlayWindow.SetSubtitleTextColor(e.NewValue.Value);
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            _overlayWindow.SetBackgroundOpacity(e.NewValue);

        private void OverallOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            _overlayWindow?.SetOverallOpacity(e.NewValue);

        private void TextOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            _overlayWindow.SetTextOpacity(e.NewValue);

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            _overlayWindow?.SetFontSize(e.NewValue);

        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            ToggleDarkMode(true);
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleDarkMode(false);
        }


        #endregion

        #region Speech Recognition

        private void ManageSpeechRecognition(VoiceNextConnection connection)
        {
            connection.VoiceReceived += (s, ev) =>
            {
                var username = ev.User != null ? ev.User.Username : "Unknown";
                if (!_recognizers.TryGetValue(ev.SSRC, out var speechRecognition))
                {
                    speechRecognition = new SpeechRecognition(BotUser.Instance.MicrosoftCognitiveServicesKey, "eastus", username);
                    speechRecognition.StartRecognition();
                    _recognizers.TryAdd(ev.SSRC, speechRecognition);
                }
                else if (ev.User != null && speechRecognition.Username != username)
                {
                    speechRecognition.UpdateUsername(username);
                }

                speechRecognition.OnTranscriptUpdated += (user, text) =>
                {
                    _overlayWindow.UpdateSubtitle(user, text);
                };

                speechRecognition.OnTranscriptCleared += (user) =>
                {
                    _overlayWindow.ClearSubtitle(user);
                };

                speechRecognition.ProcessAudio(ev.PcmData.ToArray());
                return Task.CompletedTask;
            };

            connection.UserLeft += (s, ev) =>
            {
                if (_recognizers.TryRemove(ev.SSRC, out var speechRecog))
                {
                    speechRecog.StopRecognition();
                    _overlayWindow.ClearSubtitle(speechRecog.Username);
                }

                return Task.CompletedTask;
            };
        }

        #endregion
    }
}
