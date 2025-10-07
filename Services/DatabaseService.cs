using SQLite;
using MauiApp3.Models;
using System.Collections.ObjectModel;

namespace MauiApp3.Services
{
    /// <summary>
    /// SQLite database service for persisting 3D model metadata
    /// </summary>
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        public DatabaseService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "models.db3");
        }

        /// <summary>
        /// Initialize database connection and create tables
        /// </summary>
        private async Task InitAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_dbPath);
            await _database.CreateTableAsync<Model3DFileDb>();
        }

        /// <summary>
        /// Get all models from database
        /// </summary>
        public async Task<List<Model3DFile>> GetAllModelsAsync()
        {
            await InitAsync();
            
            var dbModels = await _database!.Table<Model3DFileDb>().ToListAsync();
            
            return dbModels.Select(ConvertToModel).ToList();
        }

        /// <summary>
        /// Save or update a single model
        /// </summary>
        public async Task<int> SaveModelAsync(Model3DFile model)
        {
            await InitAsync();
            
            var dbModel = ConvertToDbModel(model);
            
            // Check if exists
            var existing = await _database!.Table<Model3DFileDb>()
                .Where(m => m.Id == model.Id)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return await _database.UpdateAsync(dbModel);
            }
            else
            {
                return await _database.InsertAsync(dbModel);
            }
        }

        /// <summary>
        /// Save multiple models in a transaction
        /// </summary>
        public async Task<int> SaveAllModelsAsync(IEnumerable<Model3DFile> models)
        {
            await InitAsync();
            
            var dbModels = models.Select(ConvertToDbModel).ToList();
            
            return await _database!.InsertAllAsync(dbModels, runInTransaction: true);
        }

        /// <summary>
        /// Delete a model from database
        /// </summary>
        public async Task<int> DeleteModelAsync(string modelId)
        {
            await InitAsync();
            
            return await _database!.Table<Model3DFileDb>()
                .DeleteAsync(m => m.Id == modelId);
        }

        /// <summary>
        /// Delete all models from database
        /// </summary>
        public async Task<int> DeleteAllModelsAsync()
        {
            await InitAsync();
            
            return await _database!.DeleteAllAsync<Model3DFileDb>();
        }

        /// <summary>
        /// Get a single model by ID
        /// </summary>
        public async Task<Model3DFile?> GetModelByIdAsync(string modelId)
        {
            await InitAsync();
            
            var dbModel = await _database!.Table<Model3DFileDb>()
                .Where(m => m.Id == modelId)
                .FirstOrDefaultAsync();

            return dbModel != null ? ConvertToModel(dbModel) : null;
        }

        /// <summary>
        /// Search models by name or tags
        /// </summary>
        public async Task<List<Model3DFile>> SearchModelsAsync(string searchTerm)
        {
            await InitAsync();
            
            var lowerSearch = searchTerm.ToLower();
            
            var dbModels = await _database!.Table<Model3DFileDb>()
                .Where(m => m.Name.ToLower().Contains(lowerSearch) || 
                           m.TagsString.ToLower().Contains(lowerSearch))
                .ToListAsync();

            return dbModels.Select(ConvertToModel).ToList();
        }

        /// <summary>
        /// Get models by file type
        /// </summary>
        public async Task<List<Model3DFile>> GetModelsByTypeAsync(string fileType)
        {
            await InitAsync();
            
            var dbModels = await _database!.Table<Model3DFileDb>()
                .Where(m => m.FileType == fileType)
                .ToListAsync();

            return dbModels.Select(ConvertToModel).ToList();
        }

        /// <summary>
        /// Get database statistics
        /// </summary>
        public async Task<(int totalModels, long totalSize, int totalTags)> GetStatisticsAsync()
        {
            await InitAsync();
            
            var models = await _database!.Table<Model3DFileDb>().ToListAsync();
            
            var totalModels = models.Count;
            var totalSize = models.Sum(m => m.FileSize);
            var allTags = models
                .SelectMany(m => m.TagsString.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Distinct()
                .Count();

            return (totalModels, totalSize, allTags);
        }

        /// <summary>
        /// Convert database model to app model
        /// </summary>
        private Model3DFile ConvertToModel(Model3DFileDb dbModel)
        {
            var tags = string.IsNullOrEmpty(dbModel.TagsString)
                ? new ObservableCollection<string>()
                : new ObservableCollection<string>(
                    dbModel.TagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                );

            return new Model3DFile
            {
                Id = dbModel.Id,
                Name = dbModel.Name,
                FilePath = dbModel.FilePath,
                FileType = dbModel.FileType,
                UploadedDate = dbModel.UploadedDate,
                FileSize = dbModel.FileSize,
                ThumbnailData = dbModel.ThumbnailData,
                Tags = tags
                // ParsedModel will be loaded separately when needed
            };
        }

        /// <summary>
        /// Convert app model to database model
        /// </summary>
        private Model3DFileDb ConvertToDbModel(Model3DFile model)
        {
            var tagsString = model.Tags.Any()
                ? string.Join(",", model.Tags)
                : string.Empty;

            return new Model3DFileDb
            {
                Id = model.Id,
                Name = model.Name,
                FilePath = model.FilePath,
                FileType = model.FileType,
                UploadedDate = model.UploadedDate,
                FileSize = model.FileSize,
                ThumbnailData = model.ThumbnailData,
                TagsString = tagsString
            };
        }

        /// <summary>
        /// Get database file path (for debugging)
        /// </summary>
        public string GetDatabasePath() => _dbPath;
    }
}
