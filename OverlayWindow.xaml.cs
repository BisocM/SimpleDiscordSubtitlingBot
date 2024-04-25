using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SimpleDiscordSubtitlingBot
{
    public partial class OverlayWindow : Window
    {
        private double currentBackgroundOpacity = 1.0;
        private double currentTextOpacity = 1.0;
        private double currentFontSize = 16;
        private Color selectedSubtitleColor = Colors.White;

        public OverlayWindow()
        {
            InitializeComponent();
            SetWindowPosition();
            InitializeFontSize();
        }

        private void InitializeFontSize()
        {
            double initialFontSize = SystemParameters.WorkArea.Height / 4 / 5;
            AdjustFontSizeForAllTextBlocks();
        }

        private void SetWindowPosition()
        {
            Width = SystemParameters.WorkArea.Width / 2;
            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            Height = 100;
            Top = SystemParameters.WorkArea.Height - Height;
        }

        private void ResetTimerForSubtitle(string username)
        {
            var border = subtitlesPanel.Children.OfType<Border>().FirstOrDefault(b => ((StackPanel)b.Child).Tag?.ToString() == username);
            if (border != null)
            {
                var timer = (DispatcherTimer)border.Tag;
                timer.Stop();
                timer.Start();
            }
        }


        private void CreateSubtitleElement(string username, string text)
        {
            var textBlock = new TextBlock
            {
                Text = $"{username}: {text}",
                Foreground = new SolidColorBrush(selectedSubtitleColor),
                Background = Brushes.Transparent,
                TextWrapping = TextWrapping.Wrap,
                FontSize = currentFontSize,
                Margin = new Thickness(5),
                MaxWidth = SystemParameters.WorkArea.Width / 2,
                Opacity = currentTextOpacity,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var closeButton = new Button
            {
                Content = "X",
                Margin = new Thickness(5),
                Visibility = Visibility.Hidden,
                Width = 20,
                Height = 20
            };
            closeButton.Click += (s, e) => RemoveSubtitle(username);

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Tag = username };
            panel.Children.Add(textBlock);
            panel.Children.Add(closeButton);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, e) =>
            {
                RemoveSubtitle(username);
                timer.Stop();
            };
            timer.Start();

            var border = new Border
            {
                Child = panel,
                Background = new SolidColorBrush(Color.FromArgb((byte)(currentBackgroundOpacity * 255), 0, 0, 0)),
                CornerRadius = new CornerRadius(5),
                Tag = timer
            };

            border.MouseEnter += (s, e) =>
            {
                closeButton.Visibility = Visibility.Visible;
                ((DispatcherTimer)border.Tag).Stop();
            };
            border.MouseLeave += (s, e) =>
            {
                closeButton.Visibility = Visibility.Hidden;
                ((DispatcherTimer)border.Tag).Start();
            };

            subtitlesPanel.Children.Add(border);
        }


        #region Dragging

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }


        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void DraggableArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        #endregion

        #region Opacity Control

        public void SetSubtitleTextColor(Color color)
        {
            selectedSubtitleColor = color;

            foreach (TextBlock tb in subtitlesPanel.Children.OfType<Border>().SelectMany(b => ((StackPanel)b.Child).Children.OfType<TextBlock>()))
                tb.Foreground = new SolidColorBrush(color);
        }

        public void SetTextOpacity(double opacity)
        {
            currentTextOpacity = opacity;
            foreach (var border in subtitlesPanel.Children.OfType<Border>())
            {
                var textBlock = (TextBlock)((StackPanel)border.Child).Children[0];
                textBlock.Opacity = opacity;
            }
        }

        public void SetBackgroundOpacity(double opacity)
        {
            currentBackgroundOpacity = opacity;
            foreach (var border in subtitlesPanel.Children.OfType<Border>())
                border.Background = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 0, 0, 0));
        }

        public void SetOverallOpacity(double opacity)
        {
            foreach (var border in subtitlesPanel.Children.OfType<Border>())
            {
                border.Opacity = opacity;
                var stackPanel = (StackPanel)border.Child;
                foreach (var child in stackPanel.Children.OfType<TextBlock>())
                    child.Opacity = opacity;
            }
        }

        #endregion

        #region Subtitle Management

        public void RemoveSubtitle(string username)
        {
            Dispatcher.Invoke(() =>
            {
                var borderToRemove = subtitlesPanel.Children
                    .OfType<Border>()
                    .FirstOrDefault(border => ((StackPanel)border.Child).Tag?.ToString() == username);
                if (borderToRemove != null)
                {
                    var timer = (DispatcherTimer)borderToRemove.Tag;
                    timer.Stop();
                    subtitlesPanel.Children.Remove(borderToRemove);
                }

                AdjustOverlayWindowSize();
            });
        }


        public void ClearSubtitle(string username)
        {
            Dispatcher.Invoke(() =>
            {
                var subtitleToRemove = subtitlesPanel.Children
                    .OfType<Border>()
                    .FirstOrDefault(border => ((StackPanel)border.Child).Tag?.ToString() == username);
                if (subtitleToRemove != null)
                {
                    subtitlesPanel.Children.Remove(subtitleToRemove);
                }
            });
        }

        public void UpdateSubtitle(string username, string text)
        {
            Dispatcher.Invoke(() =>
            {
                var existingContainer = subtitlesPanel.Children.OfType<Border>().FirstOrDefault(border => ((StackPanel)border.Child).Tag?.ToString() == username);

                if (existingContainer != null)
                {
                    var textBlock = (TextBlock)((StackPanel)existingContainer.Child).Children[0];
                    textBlock.Text = $"{username}: {text}";
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    ResetTimerForSubtitle(username);
                }
                else
                {
                    CreateSubtitleElement(username, text);
                }

                AdjustOverlayWindowSize();
            });
        }


        #endregion

        #region Font/Height Control

        public void SetFontSize(double fontSize)
        {
            currentFontSize = fontSize;
            AdjustFontSizeForAllTextBlocks();
        }

        private void AdjustFontSizeForAllTextBlocks()
        {
            Dispatcher.Invoke(() =>
            {
                foreach (TextBlock tb in subtitlesPanel.Children.OfType<Border>().SelectMany(b => ((StackPanel)b.Child).Children.OfType<TextBlock>()))
                {
                    tb.FontSize = currentFontSize;
                    tb.TextWrapping = TextWrapping.Wrap;
                }

                UpdateContainerHeight();
            });
        }

        private void UpdateContainerHeight()
        {
            foreach (Border border in subtitlesPanel.Children.OfType<Border>())
            {
                border.Height = Double.NaN;
            }

            AdjustOverlayWindowSize();
        }

        private void AdjustOverlayWindowSize()
        {
            double totalHeight = subtitlesPanel.Children.OfType<Border>()
                .Sum(border => border.DesiredSize.Height + border.Margin.Top + border.Margin.Bottom);

            Height = totalHeight + 10;
        }
        #endregion
    }
}
