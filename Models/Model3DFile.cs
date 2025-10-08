using MauiApp3.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiApp3.Models
{
    public class Model3DFile : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = string.Empty;
        private string _filePath = string.Empty;
        private string _fileType = string.Empty;
        private DateTime _uploadedDate = DateTime.Now;
        private long _fileSize;
        private byte[]? _thumbnailData;
        private StlParser.StlModel? _parsedModel;
        private string? _projectId;
        private string? _projectName;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public string FileType
        {
            get => _fileType;
            set => SetProperty(ref _fileType, value);
        }

        public DateTime UploadedDate
        {
            get => _uploadedDate;
            set => SetProperty(ref _uploadedDate, value);
        }

        public long FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        public byte[]? ThumbnailData
        {
            get => _thumbnailData;
            set => SetProperty(ref _thumbnailData, value);
        }

        public StlParser.StlModel? ParsedModel
        {
            get => _parsedModel;
            set => SetProperty(ref _parsedModel, value);
        }

        public ObservableCollection<string> Tags { get; set; } = new();

        /// <summary>
        /// Collection of attached image file paths or byte arrays serialized as base64
        /// </summary>
        public ObservableCollection<AttachedImage> AttachedImages { get; set; } = new();

        /// <summary>
        /// Collection of attached G-code files
        /// </summary>
        public ObservableCollection<AttachedGCode> AttachedGCodeFiles { get; set; } = new();

        /// <summary>
        /// ID of the project this model belongs to (null if not in a project)
        /// </summary>
        public string? ProjectId
        {
            get => _projectId;
            set => SetProperty(ref _projectId, value);
        }

        /// <summary>
        /// Name of the project this model belongs to (cached for display)
        /// </summary>
        public string? ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
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
