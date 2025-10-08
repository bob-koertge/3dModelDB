using SQLite;

namespace MauiApp3.Models
{
    /// <summary>
    /// Database model for persisting project data
    /// </summary>
    [Table("Projects")]
    public class ProjectDb
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime ModifiedDate { get; set; }
        
        public string Color { get; set; } = "#0078D4";
        
        /// <summary>
        /// Comma-separated list of model IDs belonging to this project
        /// </summary>
        public string ModelIds { get; set; } = string.Empty;
    }
}
