using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace SimpleDiscordSubtitlingBot
{
    public partial class MainWindow : Window
    {
        private const int SampleLengthMilliseconds = 100;
        private DiscordClient _client;
        private VoiceNextExtension _voiceNext;
        private OverlayWindow _overlayWindow;

        private ConcurrentDictionary<uint, SpeechRecognitionHandler> _recognizers = new();

        public MainWindow()
        {
            InitializeComponent();
            _overlayWindow = new OverlayWindow();

            //Connect event handlers if not already connected via XAML
            txtGuildSearch.KeyUp += TxtGuildSearch_KeyUp;
            fontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;
            DarkModeToggle.Checked += DarkModeToggle_Checked;
            DarkModeToggle.Unchecked += DarkModeToggle_Unchecked;
        }

        #region Initialization

        private async Task InitializeClientAsync(string token)
        {
            var config = new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                Intents = DiscordIntents.Guilds | DiscordIntents.GuildMessages | DiscordIntents.GuildVoiceStates | DiscordIntents.MessageContents
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

        #endregion

        #region UI Interactions

        private async void InitializeBot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                initButton.IsEnabled = false;

                if (string.IsNullOrWhiteSpace(txtToken.Text))
                {
                    MessageBox.Show("Bot token is required.");
                    initButton.IsEnabled = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtCognitiveServicesKey.Text))
                {
                    MessageBox.Show("Azure Cognitive Services API Key is required.");
                    initButton.IsEnabled = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(cboServiceRegion.SelectedValue?.ToString()))
                {
                    MessageBox.Show("Service region must be specified.");
                    initButton.IsEnabled = true;
                    return;
                }

                await InitializeClientAsync(txtToken.Text);

                BotUser.Instance.DiscordBotToken = txtToken.Text;
                BotUser.Instance.DiscordClientId = _client.CurrentUser.Id.ToString();
                BotUser.Instance.MicrosoftCognitiveServicesKey = txtCognitiveServicesKey.Text;
                if (cboServiceRegion.SelectedItem is ComboBoxItem selectedRegionItem)
                    BotUser.Instance.ServiceRegion = selectedRegionItem.Content.ToString();
                else
                {
                    MessageBox.Show("Service region must be selected.");
                    initButton.IsEnabled = true;
                    return;
                }

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
            if (_client?.Guilds.Count > 0)
            {
                var filtered = _client.Guilds.Values
                    .Where(g => g.Name.Contains(txtGuildSearch.Text, StringComparison.CurrentCultureIgnoreCase))
                    .Select(g => g.Name);
                lstGuilds.ItemsSource = filtered;
            }
            else
            {
                MessageBox.Show("No guilds to search through!");
            }
        }

        private void LstGuilds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstGuilds.SelectedItem == null) return;
            
            string? selectedGuildName = lstGuilds.SelectedItem.ToString();
            var guild = _client.Guilds.Values.FirstOrDefault(g => g.Name == selectedGuildName);
            if (guild == null) return;

            var userDisplayList = guild.Members.Values
                .Where(member => !member.IsBot)
                .Select(member => $"{member.Username}#{member.Discriminator}")
                .ToList();

            lstUsers.ItemsSource = userDisplayList;
        }

        private void TxtUserSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (lstGuilds.SelectedItem == null)
                return;

            string? selectedGuildName = lstGuilds.SelectedItem.ToString();
            var guild = _client.Guilds.Values.FirstOrDefault(g => g.Name == selectedGuildName);
            if (guild == null) return;
            
            var filtered = guild.Members.Values
                .Where(member => member.Username.StartsWith(txtUserSearch.Text, StringComparison.OrdinalIgnoreCase))
                .Select(member => $"{member.Username}#{member.Discriminator}");
            lstUsers.ItemsSource = filtered;
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

            var selectedGuildName = lstGuilds.SelectedItem.ToString();
            var guild = _client.Guilds.Values.FirstOrDefault(g => g.Name == selectedGuildName);
            if (guild == null)
            {
                MessageBox.Show("Guild not found.");
                return;
            }

            DiscordMember? user = guild.Members.Values.FirstOrDefault(m => $"{m.Username}#{m.Discriminator}" == lstUsers.SelectedItem.ToString() && !m.IsBot);
            if (user == null)
            {
                MessageBox.Show("User not found in the selected guild.");
                return;
            }
            BotUser.Instance.SelectedUser = user;

            var voiceState = user.VoiceState;
            if (voiceState?.Channel == null)
            {
                MessageBox.Show("User is not in a voice channel.");
                return;
            }

            lstGuilds.IsEnabled = false;
            lstUsers.IsEnabled = false;

            DiscordChannel voiceChannel = voiceState.Channel;
            VoiceNextConnection connection = await voiceChannel.ConnectAsync();
            _overlayWindow.Show();

            ManageSpeechRecognition(connection);
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (_overlayWindow == null) return;

            if (e.NewValue.HasValue)
                _overlayWindow.SetSubtitleTextColor(e.NewValue.Value);
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_overlayWindow == null) return;

            _overlayWindow.SetBackgroundOpacity(e.NewValue);
        }

        private void OverallOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_overlayWindow == null) return;

            _overlayWindow.SetOverallOpacity(e.NewValue);
        }

        private void TextOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_overlayWindow == null) return;
            
            _overlayWindow.SetTextOpacity(e.NewValue);
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_overlayWindow == null) return;

            _overlayWindow?.SetFontSize(e.NewValue);
        }

        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            ToggleDarkMode(true);
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleDarkMode(false);
        }

        private void ToggleDarkMode(bool isDark)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);

            paletteHelper.SetTheme(theme);
        }

        //Event handler for the Discord bot link button
        private void OpenDiscordBotLink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(
                new ProcessStartInfo("https://discord.com/developers/applications") { UseShellExecute = true });
        }

        //Event handler for the Azure Speech API link button
        private void OpenAzureSpeechLink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://azure.microsoft.com/en-us/services/cognitive-services/")
                { UseShellExecute = true });
        }

        //Event handler for hyperlinks in the Help tab
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
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
                    speechRecognition = new SpeechRecognitionHandler(BotUser.Instance.MicrosoftCognitiveServicesKey, BotUser.Instance.ServiceRegion, username);
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
                if (!_recognizers.TryRemove(ev.SSRC, out var speechRecog)) return Task.CompletedTask;
                speechRecog.StopRecognition();
                _overlayWindow.ClearSubtitle(speechRecog.Username);

                return Task.CompletedTask;
            };
        }

        #endregion
    }
}