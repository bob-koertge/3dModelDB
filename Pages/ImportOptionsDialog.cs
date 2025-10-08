using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace MauiApp3.Pages
{
    public class ImportOptionsDialog : ContentPage
    {
        private TaskCompletionSource<string?> _taskCompletionSource;
        private readonly int _objectCount;
        private readonly string _fileName;

        public ImportOptionsDialog(string fileName, int objectCount)
        {
            _fileName = fileName;
            _objectCount = objectCount;
            _taskCompletionSource = new TaskCompletionSource<string?>();
            
            Title = "Import Options";
            BackgroundColor = Color.FromArgb("#E0000000"); // Semi-transparent for modal feel

            Content = CreateDialogContent();
        }

        private View CreateDialogContent()
        {
            var mainCard = new Border
            {
                BackgroundColor = Color.FromArgb("#2C2C2C"),
                Stroke = Color.FromArgb("#404040"),
                StrokeThickness = 1,
                Padding = 0,
                Margin = new Thickness(40, 80),
                MaximumWidthRequest = 450,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Shadow = new Shadow
                {
                    Brush = Colors.Black,
                    Offset = new Point(0, 4),
                    Radius = 16,
                    Opacity = 0.5f
                },
                Content = new VerticalStackLayout
                {
                    Spacing = 0,
                    Children =
                    {
                        CreateHeader(),
                        CreateFileInfo(),
                        CreateOptions(),
                        CreateFooter()
                    }
                }
            };

            return new Grid
            {
                Children = { mainCard }
            };
        }

        private View CreateHeader()
        {
            var closeButton = new Button
            {
                Text = "×",
                FontSize = 28,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#888888"),
                WidthRequest = 40,
                HeightRequest = 40,
                Padding = 0,
                Margin = new Thickness(0, 8, 8, 0),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start
            };
            closeButton.Clicked += (s, e) => CompleteDialog(null);

            return new Grid
            {
                Padding = new Thickness(24, 20, 16, 12),
                BackgroundColor = Color.FromArgb("#242424"),
                Children =
                {
                    new Label
                    {
                        Text = "Import Multi-Object File",
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        VerticalOptions = LayoutOptions.Center
                    },
                    closeButton
                }
            };
        }

        private View CreateFileInfo()
        {
            return new VerticalStackLayout
            {
                Spacing = 6,
                Padding = new Thickness(24, 16, 24, 20),
                BackgroundColor = Color.FromArgb("#1E1E1E"),
                Children =
                {
                    new Label
                    {
                        Text = System.IO.Path.GetFileName(_fileName),
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#4FC3F7"),
                        LineBreakMode = LineBreakMode.TailTruncation
                    },
                    new Label
                    {
                        Text = $"{_objectCount} objects detected",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#999999")
                    }
                }
            };
        }

        private View CreateOptions()
        {
            return new VerticalStackLayout
            {
                Spacing = 12,
                Padding = new Thickness(24, 20),
                Children =
                {
                    new Label
                    {
                        Text = "How would you like to import?",
                        FontSize = 13,
                        TextColor = Color.FromArgb("#CCCCCC"),
                        Margin = new Thickness(0, 0, 0, 4)
                    },
                    CreateCompactOptionButton(
                        "Combined Model",
                        "Single unified 3D model",
                        "#2196F3",
                        "?",
                        () => CompleteDialog("combined")
                    ),
                    CreateCompactOptionButton(
                        "Separate Objects",
                        $"{_objectCount} individual models in new project",
                        "#4CAF50",
                        "?",
                        () => CompleteDialog("separate")
                    )
                }
            };
        }

        private View CreateFooter()
        {
            var cancelButton = new Button
            {
                Text = "Cancel",
                FontSize = 13,
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#888888"),
                Padding = new Thickness(16, 10),
                HorizontalOptions = LayoutOptions.Center
            };
            cancelButton.Clicked += (s, e) => CompleteDialog(null);

            return new StackLayout
            {
                Padding = new Thickness(24, 12, 24, 20),
                Children = { cancelButton }
            };
        }

        private Border CreateCompactOptionButton(string title, string subtitle, string color, string icon, Action onClicked)
        {
            var iconLabel = new Label
            {
                Text = icon,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb(color),
                WidthRequest = 32,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var textStack = new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontSize = 15,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White
                    },
                    new Label
                    {
                        Text = subtitle,
                        FontSize = 12,
                        TextColor = Color.FromArgb("#999999")
                    }
                }
            };

            var arrow = new Label
            {
                Text = "›",
                FontSize = 22,
                TextColor = Color.FromArgb("#666666"),
                VerticalOptions = LayoutOptions.Center
            };

            var contentGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 12,
                Padding = new Thickness(16, 14),
                Children = { iconLabel, textStack, arrow }
            };
            
            Grid.SetColumn(textStack, 1);
            Grid.SetColumn(arrow, 2);

            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#353535"),
                Stroke = Color.FromArgb(color),
                StrokeThickness = 1.5,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Content = contentGrid
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) =>
            {
                await border.ScaleTo(0.97, 80);
                await border.ScaleTo(1.0, 80);
                onClicked();
            };
            border.GestureRecognizers.Add(tapGesture);

            var pointerGesture = new PointerGestureRecognizer();
            pointerGesture.PointerEntered += (s, e) =>
            {
                border.BackgroundColor = Color.FromArgb("#3E3E3E");
                border.StrokeThickness = 2;
            };
            pointerGesture.PointerExited += (s, e) =>
            {
                border.BackgroundColor = Color.FromArgb("#353535");
                border.StrokeThickness = 1.5;
            };
            border.GestureRecognizers.Add(pointerGesture);

            return border;
        }

        private void CompleteDialog(string? result)
        {
            _taskCompletionSource?.SetResult(result);
        }

        public Task<string?> GetResultAsync()
        {
            return _taskCompletionSource.Task;
        }
    }
}
