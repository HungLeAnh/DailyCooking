using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace SaveAndLoadsystem
{
    public class GameData
    {
        public string Name { get; set; }
        public string CurrentLevelName { get; set; }
    }

    public interface ISerializer
    {
        string Serialize<T>(T obj);
        T Deserialize<T>(string json);
    }

    public class JsonSerializer : ISerializer
    {
        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }

    public interface IDataService
    {
        void Save(GameData data, bool overwrite = true);
        GameData Load(string name);
        void Delete(string name);
        void DeleteAll();
        IEnumerable<string> ListSaves();
    }

    public class FileDataService : IDataService
    {
        public ISerializer Serializer { get; set; }
        public string DataPath { get; set; }
        public string FileExtension { get; set; }

        public class Builder
        {
            private ISerializer serializer;
            string dataPath = Application.persistentDataPath;
            string fileExtension = "json";

            public Builder WithSerializer(ISerializer serializer)
            {
                this.serializer = serializer;
                return this;
            }

            public Builder WithDataPath(string path)
            {
                this.dataPath = path;
                return this;
            }

            public Builder WithFileExtension(string extension)
            {
                this.fileExtension = extension;
                return this;
            }

            public FileDataService Build()
            {
                FileDataService fileDataService = new FileDataService();
                fileDataService.Serializer = this.serializer;
                fileDataService.DataPath = this.dataPath;
                fileDataService.FileExtension = this.fileExtension;
                return fileDataService;
            }
        }

        string GetPathToFile(string fileName)
        {
            return Path.Combine(DataPath, string.Concat(fileName, ".", FileExtension));
        }

        public void Delete(string name)
        {
            string fileLocation = GetPathToFile(name);

            if (File.Exists(fileLocation))
            {
                File.Delete(fileLocation);
            }
        }

        public void DeleteAll()
        {
            foreach (string filePath in Directory.GetFiles(DataPath))
            {
                File.Delete(filePath);
            }
        }

        public IEnumerable<string> ListSaves()
        {
            foreach (string path in Directory.EnumerateFiles(DataPath))
            {
                if (Path.GetExtension(path) == FileExtension)
                {
                    yield return Path.GetFileNameWithoutExtension(path);
                }
            }
        }

        public GameData Load(string name)
        {
            string fileLocation = GetPathToFile(name);

            if (!File.Exists(fileLocation))
            {
                throw new ArgumentException($"No presisted GameData with name '{name}'");
            }

            return Serializer.Deserialize<GameData>(File.ReadAllText(fileLocation));
        }

        public void Save(GameData data, bool overwrite = true)
        {
            string fileLocation = GetPathToFile(data.Name);

            if (!overwrite && File.Exists(fileLocation))
            {
                throw new IOException($"The file '{data.Name}.{FileExtension}' already exists and cannot overwritten. ");
            }

            File.WriteAllText(fileLocation, this.Serializer.Serialize(data));
        }
    }

    public class SaveLoadSystem : PersistentSingleton<SaveLoadSystem>
    {
        GameData data;

        IDataService dataService;
        protected override void Awake()
        {
            base.Awake();
            dataService = new FileDataService.Builder()
                                .WithSerializer(new JsonSerializer())
                                .Build();
        }
        public void NewGame()
        {
            data = new GameData
            {
                Name = "New Game",
                CurrentLevelName = "GamePlay"
            };
        }
        public void SaveGame()
        {
            dataService.Save(data);
        }
        public void LoadGame(string name)
        {
            data = dataService.Load(name);
            if (String.IsNullOrWhiteSpace(data.CurrentLevelName))
            {
                data.CurrentLevelName = "Demo";
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(data.CurrentLevelName);
        }
        public void ReLoadGame() => LoadGame(data.Name);
        public void DeleteGame(string gameName)
        {
            dataService.Delete(gameName);
        }
    }
}
