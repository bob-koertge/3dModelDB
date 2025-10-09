using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiApp3.Models
{
    /// <summary>
    /// Represents a G-code file attached to a 3D model
    /// </summary>
    public class AttachedGCode : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _fileName = string.Empty;
        private string _filePath = string.Empty;
        private long _fileSize;
        private DateTime _attachedDate = DateTime.Now;
        private string _description = string.Empty;
        private string _slicerName = string.Empty;
        private string _printSettings = string.Empty;

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

        /// <summary>
        /// File path to the G-code file on disk
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public long FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
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

        /// <summary>
        /// Name of slicer used (e.g., Cura, PrusaSlicer, etc.)
        /// </summary>
        public string SlicerName
        {
            get => _slicerName;
            set => SetProperty(ref _slicerName, value);
        }

        /// <summary>
        /// Brief description of print settings (e.g., "0.2mm, PLA, 200°C")
        /// </summary>
        public string PrintSettings
        {
            get => _printSettings;
            set => SetProperty(ref _printSettings, value);
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
