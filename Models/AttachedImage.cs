using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiApp3.Models
{
    /// <summary>
    /// Represents an image attached to a 3D model
    /// </summary>
    public class AttachedImage : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _fileName = string.Empty;
        private byte[]? _imageData;
        private DateTime _attachedDate = DateTime.Now;
        private string _description = string.Empty;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public byte[]? ImageData
        {
            get => _imageData;
            set => SetProperty(ref _imageData, value);
        }

        public DateTime AttachedDate
        {
            get => _attachedDate;
            set => SetProperty(ref _attachedDate, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
