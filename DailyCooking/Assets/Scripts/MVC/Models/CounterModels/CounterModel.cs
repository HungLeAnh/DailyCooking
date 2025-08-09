using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Observer;
using TMPro;
//namespace MVC
//{
//    [System.Serializable]
//    public class BaseModel : Observable
//    {
//    }    

//    public class BaseController<M> where M : BaseModel
//    {

//        protected M Model;

//        public virtual void Setup(M model)
//        {
//            Model = model;
//        }

//    }


//    public class BaseView<M, C> : MonoBehaviour
//    where M : BaseModel
//    where C : BaseController<M>, new()
//    {
//        public M Model;
//        protected C Controller;

//        public virtual void Awake()
//        {
//            Controller = new C();
//            Controller.Setup(Model);
//        }
//    }
//}

//namespace Builder
//{
//    public interface IBuilder<T>
//    {
//        public T Build();
//    }
//}

//namespace SaveAndLoadsystem
//{
//    public class GameData
//    {
//        public string Name { get; set; }
//        public string CurrentLevelName { get; set; }
//    }
//    public interface ISerializer
//    {
//        string Serialize<T>(T obj);
//        T Deserialize<T>(string json);
//    }
//    public interface IDataService
//    {
//        void Save(GameData data, bool overwrite = true);
//        GameData Load(string name);
//        void Delete(string name);
//        void DeleteAll();
//        IEnumerable<string> ListSaves();    
//    }
//    public class JsonSerializer : ISerializer
//    {
//        public T Deserialize<T>(string json)
//        {
//            return JsonConvert.DeserializeObject<T>(json);
//        }

//        public string Serialize<T>(T obj)
//        {
//            return JsonConvert.SerializeObject(obj);
//        }
//    }
//    public class FileDataService : IDataService
//    {
//        public ISerializer Serializer { get; set; }
//        public string DataPath {  get; set; }
//        public string FileExtension { get; set; }
//        public class Builder
//        {
//            private ISerializer serializer;
//            string dataPath = Application.persistentDataPath;
//            string fileExtension = "json";

//            public Builder WithSerializer(ISerializer serializer)
//            {
//                this.serializer = serializer;
//                return this;
//            }
//            public Builder WithDataPath(string path)
//            {
//                this.dataPath = path;
//                return this;
//            }
//            public Builder WithFileExtension(string extension)
//            {
//                this.fileExtension = extension;
//                return this;
//            }

//            public FileDataService Build()
//            {
//                FileDataService fileDataService = new FileDataService();
//                fileDataService.Serializer = this.serializer;
//                fileDataService.DataPath = this.dataPath;
//                fileDataService.FileExtension = this.fileExtension;
//                return fileDataService;
//            }

//        }
//        string GetPathToFile(string fileName)
//        {
//            return Path.Combine(DataPath, string.Concat(fileName, ".", FileExtension));
//        }
//        public void Delete(string name)
//        {
//            string fileLocation = GetPathToFile(name);

//            if (File.Exists(fileLocation))
//            {
//                File.Delete(fileLocation);
//            }

//        }

//        public void DeleteAll()
//        {
//            foreach (string  filePath in Directory.GetFiles(DataPath))
//            {
//                File.Delete(filePath);
//            }
//        }

//        public IEnumerable<string> ListSaves()
//        {
//            foreach (string path in Directory.EnumerateFiles(DataPath))
//            {
//                if(Path.GetExtension(path) == FileExtension)
//                {
//                    yield return Path.GetFileNameWithoutExtension(path); 
//                }
//            }
//        }

//        public GameData Load(string name)
//        {
//            string fileLocation = GetPathToFile(name);

//            if (!File.Exists(fileLocation))
//            {
//                throw new ArgumentException($"No presisted GameData with name '{name}'");
//            }

//            return Serializer.Deserialize<GameData>(File.ReadAllText(fileLocation));
//        }        

//        public void Save(GameData data, bool overwrite = true)
//        {
//            string fileLocation = GetPathToFile(data.Name);
            
//            if (!overwrite && File.Exists(fileLocation)) 
//            {
//                throw new IOException($"The file '{data.Name}.{FileExtension}' already exists and cannot overwritten. ");
//            }

//            File.WriteAllText(fileLocation, this.Serializer.Serialize(data));
//        }

//    }
//    public interface ISaveable
//    {
        
//    }
//    public class SaveLoadSystem : PersistentSingleton<SaveLoadSystem>
//    {
//        GameData data;

//        IDataService dataService;
//        protected override void Awake()
//        {
//            base.Awake();
//            dataService = new FileDataService.Builder()
//                                .WithSerializer(new JsonSerializer())
//                                .Build();
//        }
//        public void NewGame()
//        {
//            data = new GameData
//            {
//                Name = "New Game",
//                CurrentLevelName = "GamePlay"
//            };
//        }
//        public void SaveGame()
//        {
//            dataService.Save(data);
//        }
//        public void LoadGame(string name)
//        {
//            data = dataService.Load(name);
//            if (String.IsNullOrWhiteSpace(data.CurrentLevelName))
//            {
//                data.CurrentLevelName = "Demo";
//            }
//            SceneManager.LoadScene(data.CurrentLevelName);
//        }
//        public void ReLoadGame() => LoadGame(data.Name);
//        public void DeleteGame(string gameName)
//        {
//            dataService.Delete(gameName);
//        }
//    }
//    namespace GameDataService
//    {
//        public interface IGameDataService
//        {
//            public bool SaveData<T>(string RelativePath, T Data, bool Encrypted);
        
//            public T LoadData<T>(string RelativePath, bool Encrypted);
//        }
//        public class JsonDataService : IGameDataService
//        {
//            private const string KEY = "";
//            private const string IV = "";
//            public T LoadData<T>(string RelativePath, bool Encrypted)
//            {
//                string path = Application.persistentDataPath + RelativePath;
//                if (!File.Exists(path))
//                {
//                    Debug.Log($"Connot load file at path {path}. File does not exits!");
//                    throw new FileNotFoundException($"{path} does not exits!");
//                }

//                try
//                {
//                    T data;
//                    if (Encrypted)
//                    {
//                        data = ReadEncryptedData<T>(path);
//                    }
//                    else
//                    {
//                        data = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
//                    }
//                    return data;
//                }
//                catch (Exception ex)
//                {
//                    Debug.LogError($"Failed to load data due to: {ex.Message} {ex.StackTrace}");
//                    throw ex;
//                }
//            }

//            public bool SaveData<T>(string RelativePath, T Data, bool Encrypted)
//            {
//                string path = Application.persistentDataPath + RelativePath;
//                try
//                {
//                    if (File.Exists(path))
//                    {    
//                        Debug.Log("Data exits. Deleting old file and writting a new one");
//                        File.Delete(path);
//                    }
//                    else
//                    {
//                        Debug.Log("Writing file for the first time");
//                    }
//                    FileStream stream = File.Create(path);
//                    if (Encrypted)
//                    {
//                        WriteEncryptedData(Data,stream);
//                    }
//                    else
//                    {
//                        stream.Close();
//                        File.WriteAllText(path, JsonConvert.SerializeObject(Data));
//                    }
//                    return true;
//                }
//                catch (Exception ex)
//                {
//                    Debug.LogError($"Unable to save data due to:{ex.Message} {ex.StackTrace}");
//                    return false;
//                }

//            }

//            private void WriteEncryptedData<T>(T Data, FileStream stream)
//            {
//                Aes aesProvider =  Aes.Create();
//                aesProvider.Key = Convert.FromBase64String(KEY);
//                aesProvider.IV = Convert.FromBase64String(IV);
//                ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor();
//                CryptoStream cryptoStream = new CryptoStream(stream,cryptoTransform, CryptoStreamMode.Write);
//                cryptoStream.Write(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(Data)));
//            }
//            private T ReadEncryptedData<T>(string path)
//            {
//                byte[] fileBytes = File.ReadAllBytes(path);
//                Aes aesProvider = Aes.Create();

//                aesProvider.Key = Convert.FromBase64String(KEY);
//                aesProvider.IV = Convert.FromBase64String(IV);

//                ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor(aesProvider.Key,aesProvider.IV);
//                MemoryStream decryptionStream = new MemoryStream(fileBytes);
//                CryptoStream cryptoStream = new CryptoStream(decryptionStream,cryptoTransform, CryptoStreamMode.Read);
//                StreamReader reader = new StreamReader(cryptoStream);
//                string Result = reader.ReadToEnd();

//                Debug.Log($"Decrypted result (if the following is not legible, probably wrong key or iv: {Result})");
//                return JsonConvert.DeserializeObject<T>(Result);
//            }
//        }
//    }
    
//}