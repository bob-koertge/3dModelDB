using MauiApp3.Models;
using MauiApp3.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MauiApp3.ViewModels
{
    public class ModelDetailViewModel : INotifyPropertyChanged
    {
        private Model3DFile _model;
        private string _newTagText = string.Empty;

        public Model3DFile Model
        {
            get => _model;
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }

        public string NewTagText
        {
            get => _newTagText;
            set
            {
                if (_newTagText != value)
                {
                    _newTagText = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand AddTagCommand { get; }
        public ICommand RemoveTagCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand ResetViewCommand { get; }

        public ModelDetailViewModel(Model3DFile model)
        {
            _model = model;
            AddTagCommand = new Command(AddTag);
            RemoveTagCommand = new Command<string>(RemoveTag);
            CloseCommand = new Command(async () => await Close());
            ResetViewCommand = new Command(ResetView);
        }

        private void AddTag()
        {
            if (string.IsNullOrWhiteSpace(NewTagText))
                return;

            var trimmedTag = NewTagText.Trim();
            
            if (Model.Tags.Contains(trimmedTag, StringComparer.OrdinalIgnoreCase))
                return;

            Model.Tags.Add(trimmedTag);
            NewTagText = string.Empty;
        }

        private void RemoveTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                Model.Tags.Remove(tag);
            }
        }

        private async Task Close()
        {
            await Shell.Current.GoToAsync("..");
        }

        private void ResetView()
        {
            // This will be handled by the page's code-behind
            MessagingCenter.Send(this, "ResetView");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
