using MauiApp3.Models;

namespace MauiApp3.Pages
{
    public class ProjectSelectorDialog : ContentPage
    {
        private readonly TaskCompletionSource<Project?> _taskCompletionSource = new();
        private readonly List<Project> _projects;
        private readonly string _modelName;

        public ProjectSelectorDialog(List<Project> projects, string modelName)
        {
            _projects = projects ?? throw new ArgumentNullException(nameof(projects));
            _modelName = modelName ?? "this model";

            BackgroundColor = Color.FromRgba(0, 0, 0, 0.85);
            
            var dialog = CreateDialog();
            Content = dialog;
        }

        public Task<Project?> GetResultAsync()
        {
            return _taskCompletionSource.Task;
        }

        private Grid CreateDialog()
        {
            var dialogGrid = new Grid
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 500,
                MaximumHeightRequest = 600,
                Padding = 0
            };

            var dialogFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#2C2C2C"),
                CornerRadius = 15,
                HasShadow = true,
                Padding = 0
            };

            var mainStack = new VerticalStackLayout
            {
                Spacing = 0
            };

            // Header
            var headerFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#1E1E1E"),
                CornerRadius = 0,
                Padding = 20,
                HasShadow = false
            };

            var headerStack = new VerticalStackLayout
            {
                Spacing = 8
            };

            headerStack.Children.Add(new Label
            {
                Text = "Add to Project",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Start
            });

            headerStack.Children.Add(new Label
            {
                Text = $"Select a project for '{_modelName}'",
                FontSize = 14,
                TextColor = Color.FromArgb("#AAAAAA"),
                HorizontalOptions = LayoutOptions.Start
            });

            headerFrame.Content = headerStack;
            mainStack.Children.Add(headerFrame);

            // Divider
            mainStack.Children.Add(new BoxView
            {
                HeightRequest = 1,
                BackgroundColor = Color.FromArgb("#444444")
            });

            // Projects List
            var scrollView = new ScrollView
            {
                Padding = 15,
                MaximumHeightRequest = 400
            };

            var projectsStack = new VerticalStackLayout
            {
                Spacing = 10
            };

            foreach (var project in _projects)
            {
                projectsStack.Children.Add(CreateProjectCard(project));
            }

            if (_projects.Count == 0)
            {
                projectsStack.Children.Add(new VerticalStackLayout
                {
                    Spacing = 10,
                    Padding = 40,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "??",
                            FontSize = 48,
                            HorizontalOptions = LayoutOptions.Center,
                            TextColor = Color.FromArgb("#666666")
                        },
                        new Label
                        {
                            Text = "No projects available",
                            FontSize = 16,
                            TextColor = Color.FromArgb("#888888"),
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "Create a project first from the main screen",
                            FontSize = 12,
                            TextColor = Color.FromArgb("#666666"),
                            FontAttributes = FontAttributes.Italic,
                            HorizontalOptions = LayoutOptions.Center
                        }
                    }
                });
            }

            scrollView.Content = projectsStack;
            mainStack.Children.Add(scrollView);

            // Divider
            mainStack.Children.Add(new BoxView
            {
                HeightRequest = 1,
                BackgroundColor = Color.FromArgb("#444444")
            });

            // Footer with Cancel button
            var footerFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#1E1E1E"),
                CornerRadius = 0,
                Padding = 20,
                HasShadow = false
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                BackgroundColor = Color.FromArgb("#3C3C3C"),
                TextColor = Colors.White,
                CornerRadius = 8,
                Padding = new Thickness(20, 12),
                HorizontalOptions = LayoutOptions.End,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            };

            cancelButton.Clicked += (s, e) =>
            {
                _taskCompletionSource.TrySetResult(null);
            };

            footerFrame.Content = cancelButton;
            mainStack.Children.Add(footerFrame);

            dialogFrame.Content = mainStack;
            dialogGrid.Children.Add(dialogFrame);

            return dialogGrid;
        }

        private Frame CreateProjectCard(Project project)
        {
            var cardFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#3C3C3C"),
                CornerRadius = 10,
                HasShadow = false,
                Padding = 15,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                _taskCompletionSource.TrySetResult(project);
            };
            cardFrame.GestureRecognizers.Add(tapGesture);

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 15
            };

            // Color indicator
            var colorBar = new BoxView
            {
                Color = Color.FromArgb(project.Color),
                WidthRequest = 6,
                HeightRequest = 50,
                CornerRadius = 3,
                VerticalOptions = LayoutOptions.Center
            };
            grid.Add(colorBar, 0, 0);

            // Project info
            var infoStack = new VerticalStackLayout
            {
                Spacing = 6,
                VerticalOptions = LayoutOptions.Center
            };

            infoStack.Children.Add(new Label
            {
                Text = project.Name,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            });

            if (!string.IsNullOrEmpty(project.Description))
            {
                infoStack.Children.Add(new Label
                {
                    Text = project.Description,
                    FontSize = 13,
                    TextColor = Color.FromArgb("#AAAAAA"),
                    LineBreakMode = LineBreakMode.TailTruncation,
                    MaxLines = 2
                });
            }

            var metaStack = new HorizontalStackLayout
            {
                Spacing = 15
            };

            metaStack.Children.Add(new Label
            {
                Text = $"?? {project.ModelIds.Count} model{(project.ModelIds.Count != 1 ? "s" : "")}",
                FontSize = 12,
                TextColor = Color.FromArgb("#888888")
            });

            metaStack.Children.Add(new Label
            {
                Text = $"?? {project.CreatedDate:MMM dd, yyyy}",
                FontSize = 12,
                TextColor = Color.FromArgb("#888888")
            });

            infoStack.Children.Add(metaStack);

            grid.Add(infoStack, 1, 0);

            // Arrow icon
            var arrowLabel = new Label
            {
                Text = "?",
                FontSize = 24,
                TextColor = Color.FromArgb("#666666"),
                VerticalOptions = LayoutOptions.Center
            };
            grid.Add(arrowLabel, 2, 0);

            cardFrame.Content = grid;

            // Hover effect
            var pointerGesture = new PointerGestureRecognizer();
            pointerGesture.PointerEntered += (s, e) =>
            {
                cardFrame.BackgroundColor = Color.FromArgb("#4C4C4C");
                arrowLabel.TextColor = Color.FromArgb("#0078D4");
            };
            pointerGesture.PointerExited += (s, e) =>
            {
                cardFrame.BackgroundColor = Color.FromArgb("#3C3C3C");
                arrowLabel.TextColor = Color.FromArgb("#666666");
            };
            cardFrame.GestureRecognizers.Add(pointerGesture);

            return cardFrame;
        }
    }
}
