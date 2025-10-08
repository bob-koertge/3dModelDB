using Microsoft.Maui.Controls;

namespace MauiApp3.Pages
{
    public partial class ProgressPage : ContentPage
    {
        private readonly Label _messageLabel;
        private readonly ActivityIndicator _activityIndicator;
        private readonly ScrollView _logScrollView;
        private readonly Label _logLabel;
        private readonly System.Text.StringBuilder _logBuilder;

        public ProgressPage(string title)
        {
            Title = title;
            _logBuilder = new System.Text.StringBuilder();
            
            _messageLabel = new Label
            {
                Text = "Processing...",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center
            };

            _activityIndicator = new ActivityIndicator
            {
                IsRunning = true,
                Color = Color.FromArgb("#0078D4"),
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 50,
                HeightRequest = 50
            };

            _logLabel = new Label
            {
                Text = "",
                FontSize = 12,
                FontFamily = "Consolas", // Use built-in monospace font instead
                TextColor = Color.FromArgb("#CCCCCC"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            _logScrollView = new ScrollView
            {
                Content = _logLabel,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 15,
                Children =
                {
                    _messageLabel,
                    _activityIndicator,
                    new BoxView
                    {
                        HeightRequest = 1,
                        Color = Color.FromArgb("#444444"),
                        Margin = new Thickness(0, 10, 0, 0)
                    },
                    new Label
                    {
                        Text = "Progress Log:",
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    },
                    _logScrollView
                }
            };
            
            BackgroundColor = Color.FromArgb("#1E1E1E");
        }

        public void UpdateMessage(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _messageLabel.Text = message;
            });
        }

        public void AppendLog(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _logBuilder.AppendLine(message);
                _logLabel.Text = _logBuilder.ToString();
                
                // Auto-scroll to bottom
                _logScrollView.ScrollToAsync(_logLabel, ScrollToPosition.End, false);
            });
        }

        public void SetComplete(bool success, string finalMessage)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _activityIndicator.IsRunning = false;
                _activityIndicator.IsVisible = false;
                _messageLabel.Text = finalMessage;
                _messageLabel.TextColor = success ? Color.FromArgb("#00FF00") : Color.FromArgb("#FF0000");
            });
        }
    }
}
