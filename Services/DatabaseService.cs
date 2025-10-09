using SQLite;
using MauiApp3.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace MauiApp3.Services
{
    /// <summary>
    /// SQLite database service for persisting 3D model metadata
    /// </summary>
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public DatabaseService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "models.db3");
        }

        private async Task InitAsync()
        {
            if (_database != null)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_database != null) // Double-check after acquiring lock
                    return;

                _database = new SQLiteAsyncConnection(_dbPath);
                await _database.CreateTableAsync<Model3DFileDb>();
                await _database.CreateTableAsync<ProjectDb>();
            }
            finally
            {
                _initLock.Release();
            }
        }

        #region Model Operations

        public async Task<List<Model3DFile>> GetAllModelsAsync()
        {
            await InitAsync();
            var dbModels = await _database!.Table<Model3DFileDb>().ToListAsync();
            return dbModels.Select(ConvertToModel).ToList();
        }

        public async Task<Model3DFile?> GetModelByIdAsync(string modelId)
        {
            await InitAsync();
            var dbModel = await _database!.Table<Model3DFileDb>()
                .Where(m => m.Id == modelId)
                .FirstOrDefaultAsync();

            return dbModel != null ? ConvertToModel(dbModel) : null;
        }

        public async Task<int> SaveModelAsync(Model3DFile model)
        {
            await InitAsync();
            var dbModel = ConvertToDbModel(model);
            
            var existing = await _database!.Table<Model3DFileDb>()
                .Where(m => m.Id == model.Id)
                .FirstOrDefaultAsync();

            return existing != null 
                ? await _database.UpdateAsync(dbModel)
                : await _database.InsertAsync(dbModel);
        }

        public async Task<int> DeleteModelAsync(string modelId)
        {
            await InitAsync();
            return await _database!.Table<Model3DFileDb>()
                .DeleteAsync(m => m.Id == modelId);
        }

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

        public async Task<List<Model3DFile>> GetModelsByTypeAsync(string fileType)
        {
            await InitAsync();
            var dbModels = await _database!.Table<Model3DFileDb>()
                .Where(m => m.FileType == fileType)
                .ToListAsync();

            return dbModels.Select(ConvertToModel).ToList();
        }

        #endregion

        #region Project Operations

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            await InitAsync();
            var dbProjects = await _database!.Table<ProjectDb>().ToListAsync();
            return dbProjects.Select(ConvertToProject).ToList();
        }

        public async Task<Project?> GetProjectByIdAsync(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
                return null;

            await InitAsync();
            var dbProject = await _database!.Table<ProjectDb>()
                .Where(p => p.Id == projectId)
                .FirstOrDefaultAsync();

            return dbProject != null ? ConvertToProject(dbProject) : null;
        }

        public async Task<int> SaveProjectAsync(Project project)
        {
            await InitAsync();
            project.ModifiedDate = DateTime.Now;
            var dbProject = ConvertToDbProject(project);
            
            var existing = await _database!.Table<ProjectDb>()
                .Where(p => p.Id == project.Id)
                .FirstOrDefaultAsync();

            return existing != null
                ? await _database.UpdateAsync(dbProject)
                : await _database.InsertAsync(dbProject);
        }

        public async Task<int> DeleteProjectAsync(string projectId)
        {
            await InitAsync();
            
            // Remove project reference from models
            var modelsInProject = await _database!.Table<Model3DFileDb>()
                .Where(m => m.ProjectId == projectId)
                .ToListAsync();

            foreach (var model in modelsInProject)
            {
                model.ProjectId = null;
                await _database.UpdateAsync(model);
            }
            
            return await _database!.DeleteAsync<ProjectDb>(projectId);
        }

        public async Task<List<Model3DFile>> GetModelsByProjectIdAsync(string projectId)
        {
            await InitAsync();
            var dbModels = await _database!.Table<Model3DFileDb>()
                .Where(m => m.ProjectId == projectId)
                .ToListAsync();

            return dbModels.Select(ConvertToModel).ToList();
        }

        public async Task<int> AssignModelToProjectAsync(string modelId, string projectId)
        {
            await InitAsync();
            var model = await _database!.Table<Model3DFileDb>()
                .Where(m => m.Id == modelId)
                .FirstOrDefaultAsync();

            if (model != null)
            {
                model.ProjectId = projectId;
                return await _database.UpdateAsync(model);
            }
            
            return 0;
        }

        public async Task<int> RemoveModelFromProjectAsync(string modelId)
        {
            await InitAsync();
            var model = await _database!.Table<Model3DFileDb>()
                .Where(m => m.Id == modelId)
                .FirstOrDefaultAsync();

            if (model != null)
            {
                model.ProjectId = null;
                return await _database.UpdateAsync(model);
            }
            
            return 0;
        }

        #endregion

        #region Database Management

        public async Task ResetDatabaseAsync()
        {
            if (_database != null)
            {
                await _database.CloseAsync();
                _database = null;
            }
            
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
            
            await InitAsync();
        }

        public string GetDatabasePath() => _dbPath;

        #endregion

        #region Conversion Methods

        private Model3DFile ConvertToModel(Model3DFileDb dbModel)
        {
            var tags = ParseCommaSeparatedString(dbModel.TagsString);
            var attachedImages = DeserializeJson<AttachedImage>(dbModel.AttachedImagesJson);
            var attachedGCode = DeserializeJson<AttachedGCode>(dbModel.AttachedGCodeJson);

            return new Model3DFile
            {
                Id = dbModel.Id,
                Name = dbModel.Name,
                FilePath = dbModel.FilePath,
                FileType = dbModel.FileType,
                UploadedDate = dbModel.UploadedDate,
                FileSize = dbModel.FileSize,
                ThumbnailData = dbModel.ThumbnailData,
                Tags = tags,
                AttachedImages = attachedImages,
                AttachedGCodeFiles = attachedGCode,
                ProjectId = dbModel.ProjectId
            };
        }

        private Model3DFileDb ConvertToDbModel(Model3DFile model)
        {
            return new Model3DFileDb
            {
                Id = model.Id,
                Name = model.Name,
                FilePath = model.FilePath,
                FileType = model.FileType,
                UploadedDate = model.UploadedDate,
                FileSize = model.FileSize,
                ThumbnailData = model.ThumbnailData,
                TagsString = string.Join(",", model.Tags),
                AttachedImagesJson = SerializeJson(model.AttachedImages),
                AttachedGCodeJson = SerializeJson(model.AttachedGCodeFiles),
                ProjectId = model.ProjectId
            };
        }

        private Project ConvertToProject(ProjectDb dbProject)
        {
            var modelIds = ParseCommaSeparatedString(dbProject.ModelIds);

            return new Project
            {
                Id = dbProject.Id,
                Name = dbProject.Name,
                Description = dbProject.Description,
                CreatedDate = dbProject.CreatedDate,
                ModifiedDate = dbProject.ModifiedDate,
                Color = dbProject.Color,
                ModelIds = modelIds
            };
        }

        private ProjectDb ConvertToDbProject(Project project)
        {
            return new ProjectDb
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedDate = project.CreatedDate,
                ModifiedDate = project.ModifiedDate,
                Color = project.Color,
                ModelIds = string.Join(",", project.ModelIds)
            };
        }

        #endregion

        #region Helper Methods

        private ObservableCollection<string> ParseCommaSeparatedString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new ObservableCollection<string>();

            return new ObservableCollection<string>(
                value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
            );
        }

        private ObservableCollection<T> DeserializeJson<T>(string? json)
        {
            if (string.IsNullOrEmpty(json))
                return new ObservableCollection<T>();

            try
            {
                var list = JsonSerializer.Deserialize<List<T>>(json);
                return list != null ? new ObservableCollection<T>(list) : new ObservableCollection<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing {typeof(T).Name}: {ex.Message}");
                return new ObservableCollection<T>();
            }
        }

        private string? SerializeJson<T>(ObservableCollection<T> collection)
        {
            if (collection == null || !collection.Any())
                return null;

            try
            {
                return JsonSerializer.Serialize(collection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
