using Microsoft.Maui.Controls;

namespace MauiApp3.Pages
{
    public partial class DiagnosticsPage : ContentPage
    {
        public DiagnosticsPage(string title, string diagnosticText)
        {
            Title = title;
            
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = 20,
                    Spacing = 10,
                    Children =
                    {
                        new Label
                        {
                            Text = title,
                            FontSize = 20,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Colors.White
                        },
                        new Editor
                        {
                            Text = diagnosticText,
                            IsReadOnly = true,
                            AutoSize = EditorAutoSizeOption.TextChanges,
                            FontSize = 12,
                            FontFamily = "Courier New",
                            TextColor = Colors.White,
                            BackgroundColor = Color.FromArgb("#2C2C2C"),
                            Margin = new Thickness(0, 10, 0, 0)
                        },
                        new HorizontalStackLayout
                        {
                            Spacing = 10,
                            HorizontalOptions = LayoutOptions.Center,
                            Margin = new Thickness(0, 20, 0, 0),
                            Children =
                            {
                                new Button
                                {
                                    Text = "Copy to Clipboard",
                                    BackgroundColor = Color.FromArgb("#0078D4"),
                                    TextColor = Colors.White,
                                    Command = new Command(async () =>
                                    {
                                        await Clipboard.SetTextAsync(diagnosticText);
                                        await Application.Current.MainPage.DisplayAlert("Copied", "Diagnostics copied to clipboard!", "OK");
                                    })
                                },
                                new Button
                                {
                                    Text = "Close",
                                    BackgroundColor = Color.FromArgb("#C42B1C"),
                                    TextColor = Colors.White,
                                    Command = new Command(async () =>
                                    {
                                        await Navigation.PopModalAsync();
                                    })
                                }
                            }
                        }
                    }
                }
            };
            
            BackgroundColor = Color.FromArgb("#1E1E1E");
        }
    }
}
