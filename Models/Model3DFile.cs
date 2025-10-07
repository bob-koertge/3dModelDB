using MauiApp3.Services;
using System.Collections.ObjectModel;

namespace MauiApp3.Models
{
    public class Model3DFile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // "STL" or "3MF"
        public DateTime UploadedDate { get; set; } = DateTime.Now;
        public long FileSize { get; set; }
        public byte[]? ThumbnailData { get; set; }
        public StlParser.StlModel? ParsedModel { get; set; }
        public ObservableCollection<string> Tags { get; set; } = new();
    }
}
