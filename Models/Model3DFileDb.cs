using SQLite;

namespace MauiApp3.Models
{
    /// <summary>
    /// Database model for persisting 3D model metadata
    /// </summary>
    [Table("Models")]
    public class Model3DFileDb
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;
        
        public string FilePath { get; set; } = string.Empty;
        
        public string FileType { get; set; } = string.Empty;
        
        public DateTime UploadedDate { get; set; }
        
        public long FileSize { get; set; }
        
        /// <summary>
        /// Thumbnail stored as byte array (max 2MB)
        /// </summary>
        [MaxLength(2000000)]
        public byte[]? ThumbnailData { get; set; }
        
        /// <summary>
        /// Tags stored as comma-separated string for SQLite compatibility
        /// </summary>
        public string TagsString { get; set; } = string.Empty;
        
        /// <summary>
        /// Attached images stored as JSON string
        /// </summary>
        [MaxLength(10000000)] // 10MB max for images
        public string? AttachedImagesJson { get; set; }
        
        /// <summary>
        /// Attached G-code files stored as JSON string
        /// </summary>
        [MaxLength(5000000)] // 5MB max for G-code metadata
        public string? AttachedGCodeJson { get; set; }
        
        /// <summary>
        /// ID of the project this model belongs to (null if not in a project)
        /// </summary>
        public string? ProjectId { get; set; }
    }
}
