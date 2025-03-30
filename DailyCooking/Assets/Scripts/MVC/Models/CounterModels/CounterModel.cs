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
namespace MVC
{
    [System.Serializable]
    public class BaseModel : Observable
    {
    }    

    public class BaseController<M> where M : BaseModel
    {

        protected M Model;

        public virtual void Setup(M model)
        {
            Model = model;
        }

    }


    public class BaseView<M, C> : MonoBehaviour
    where M : BaseModel
    where C : BaseController<M>, new()
    {
        public M Model;
        protected C Controller;

        public virtual void Awake()
        {
            Controller = new C();
            Controller.Setup(Model);
        }
    }
}
namespace Observer
{
    public enum EObserverEvent
    {
        Default,
        ModelChange,
    }
    public interface IObserver
    {
        public void OnNotify();
    }

    public abstract class Observable
    {
        private readonly SortedDictionary<EObserverEvent, List<IObserver>> SubscribersDict = new SortedDictionary<EObserverEvent, List<IObserver>>();
        public Observable()
        {
            SubscribersDict = new SortedDictionary<EObserverEvent, List<IObserver>>();
            Array.ForEach<EObserverEvent>((EObserverEvent[])Enum.GetValues(typeof(EObserverEvent)),
                EventType => SubscribersDict.Add(EventType, new List<IObserver>()));        
        }

        public void Subscribe(EObserverEvent eventType, IObserver subscriber)
        {
            SubscribersDict.GetValueOrDefault(eventType).Add(subscriber);
        }
        public void Unsubscribe(EObserverEvent eventType, IObserver subscriber)
        {
            SubscribersDict.GetValueOrDefault(eventType).Remove(subscriber);
        }
        public void NotifySubscribers(EObserverEvent eventType)
        {
            SubscribersDict.GetValueOrDefault(eventType).ForEach(subscriber => subscriber.OnNotify());
        }

    }

}
namespace Builder
{
    public interface IBuilder<T>
    {
        public T Build();
    }
}
namespace ServiceLocator
{
    public class ServiceManager
    {
        private readonly Dictionary<Type,object> services = new Dictionary<Type,object>();
        public IEnumerable<object> RegisteredServices => services.Values;

        public ServiceManager Register<T>(T serviceInstance)
        {
            if (!services.TryAdd(typeof(T), serviceInstance))
            {
                Debug.LogWarning($"Service of type {typeof(T).FullName} already registered. Replace old service instance with new instance");

                services[typeof(T)] = serviceInstance;
            }
            return this;
        }
        public ServiceManager Register(Type type, object serviceInstance)
        {
            if (!type.IsInstanceOfType(serviceInstance))
                throw new ArgumentException("Type of service does not match type of service interface",nameof(serviceInstance));

            if (!services.TryAdd(type, serviceInstance))
            {
                Debug.LogWarning($"Service of type {type.FullName} already registered. Replace old service instance with new instance");

                services[type] = serviceInstance;
            }
            return this;
        }

        public T GetService<T>() where T : class
        {
            if (services.TryGetValue(typeof(T),out object serviceObject))
            {
                return serviceObject as T;    
            }
            else
            {
                Debug.LogWarning($"Cannot get Service. Please register Service of type{typeof(T)} first");
                return default;
            }
        }
        public bool TryGetService<T>(out T service) where T : class
        {
            if(services.TryGetValue(typeof(T), out object serviceObject))
            {
                service = serviceObject as T;
                return true;
            }
            else
            {
                service = default;
                Debug.LogWarning($"Cannot get Service. Please register Service of type{typeof(T)} first");
                return false;
            }
        }
    }
    public class ServiceLocator : MonoBehaviour
    {
        static private ServiceLocator _global;
        static private Dictionary<Scene, ServiceLocator> _sceneContainers;
        readonly ServiceManager _serviceManager = new ServiceManager();

        private const string GlobalServiceLocatorName = "ServiceLocator [Global]";  
        private const string SceneServiceLocatorName = "ServiceLocator [Scene]";
        private void OnDestroy()
        {
            if (this == _global)
            {
                _global = null;
            }
            else if (_sceneContainers.ContainsValue(this))
            {
                _sceneContainers.Remove(gameObject.scene);
            }
        }
        internal void ConfigureAsGlobal(bool dontDestroyOnLoad)
        {
            if(_global == this)
            {
                Debug.LogWarning("ServiceLocator.ConfigureAsGlobal : Already config as global", this);
            }
            else if(_global != null)
            {
                Debug.LogWarning("ServiceLocator.ConfigureAsGlobal : Another ServiceLocator is already config as global", this);
            }
            else
            {
                _global = this;
                if(dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            }
        }
        internal void ConfigureForScene()
        {
            Scene scene = gameObject.scene;
            if (_sceneContainers.ContainsKey(scene))
            {
                Debug.LogError("ServiceLocator.ConfigureForScene : Another ServiceLocator is already configure for this scene", this);
                return;
            }

            _sceneContainers.Add(scene,this);

        }
        public static ServiceLocator Global
        {
            get
            {
                if (_global != null) return _global;

                //bootstrap or initialize the new instance of global as necessary
                if(FindObjectOfType<ServiceLocatorGlobalBootstrapper>() is { } found)
                {
                    found.BootstrapOnDemand();
                    return _global;
                }

                var container = new GameObject(GlobalServiceLocatorName, typeof(ServiceLocator));
                container.AddComponent<ServiceLocatorGlobalBootstrapper>().BootstrapOnDemand();

                return _global;
            }
        }

        private static List<GameObject> tmpSceneGameObjects;
        public static ServiceLocator For(MonoBehaviour mb)
        {
            return mb.GetComponentInParent<ServiceLocator>() != null ? Global : ForSceneOf(mb);
        }
        public static ServiceLocator ForSceneOf(MonoBehaviour mb)
        {
            Scene scene = mb.gameObject.scene;

            if (_sceneContainers.TryGetValue(scene, out ServiceLocator container) && container != mb)
            {
                return container;
            }

            tmpSceneGameObjects.Clear();
            scene.GetRootGameObjects(tmpSceneGameObjects);

            foreach (GameObject go in tmpSceneGameObjects.Where(go => go.GetComponent<ServiceLocatorSceneBootstrapper>() != null))
            {
                if(go.TryGetComponent(out ServiceLocatorSceneBootstrapper bootstrapper) &&
                    bootstrapper.Container != mb)
                {
                    bootstrapper.BootstrapOnDemand();
                    return bootstrapper.Container;
                }
            }
            return Global;
        }
        public ServiceLocator Register<T>(T service)
        {
            _serviceManager.Register(service);
            return this;
        }
        public ServiceLocator Register(Type type,object service)
        {
            _serviceManager.Register(type, service);
            return this;
        }
        public ServiceLocator Get<T>(out T service) where T : class 
        {
            if (TryGetService(out service)) return this;
            if(TryGetNextInHierarchy(out ServiceLocator container))
            {
                container.Get(out service);
                return this;
            }

            throw new ArgumentException($"ServiceLocator.Get : Service of type {typeof(T).FullName} not register");
        }
        bool TryGetService<T>(out T service) where T : class
        {
            return _serviceManager.TryGetService(out service);
        }
        bool TryGetNextInHierarchy(out ServiceLocator container)
        {
            if(this == _global)
            {
                container = null;
                return false;
            }
            container = transform.parent 
                ? (transform.parent.GetComponentInParent<ServiceLocator>() == null
                    ? ForSceneOf(this)
                    : null) 
                : null;
            return container != null;       
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _global = null;
            _sceneContainers = new Dictionary<Scene, ServiceLocator>();
            tmpSceneGameObjects = new List<GameObject>();
        }


#if UNITY_EDITOR
        [MenuItem("GameObject/ServiceLocator/Add Global")]
        static void AddGlobal()
        {
            var go = new GameObject(GlobalServiceLocatorName, typeof(ServiceLocatorGlobalBootstrapper));
        }

        [MenuItem("GameObject/ServiceLocator/Add Scene")]
        static void AddScene()
        {
            var go = new GameObject(SceneServiceLocatorName, typeof(ServiceLocatorSceneBootstrapper));
        }
#endif
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(ServiceLocator))]
    public abstract class Bootstrapper : MonoBehaviour
    {
        ServiceLocator container;
        internal ServiceLocator Container => container != null ? container : (container = GetComponent<ServiceLocator>());

        private bool _hasBeenBootstrapped;

        private void Awake()
        {
            BootstrapOnDemand();
        }

        public void BootstrapOnDemand()
        {
            if(_hasBeenBootstrapped) return;
            _hasBeenBootstrapped = true;
            Bootstrap();
        }

        protected abstract void Bootstrap();
    }

    [AddComponentMenu("ServiceLocator/ServiceLocator Global")]
    public class ServiceLocatorGlobalBootstrapper : Bootstrapper
    {
        [SerializeField] private bool _isDontDestroyOnLoad = true;

        protected override void Bootstrap()
        {
            Container.ConfigureAsGlobal(_isDontDestroyOnLoad);
        }
    }
    [AddComponentMenu("ServiceLocator/ServiceLocator Scene")]
    public class ServiceLocatorSceneBootstrapper : Bootstrapper
    {
        protected override void Bootstrap()
        {
            Container.ConfigureForScene();
        }
    }
}
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
    public interface IDataService
    {
        void Save(GameData data, bool overwrite = true);
        GameData Load(string name);
        void Delete(string name);
        void DeleteAll();
        IEnumerable<string> ListSaves();    
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
    public class FileDataService : IDataService
    {
        public ISerializer Serializer { get; set; }
        public string DataPath {  get; set; }
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
            foreach (string  filePath in Directory.GetFiles(DataPath))
            {
                File.Delete(filePath);
            }
        }

        public IEnumerable<string> ListSaves()
        {
            foreach (string path in Directory.EnumerateFiles(DataPath))
            {
                if(Path.GetExtension(path) == FileExtension)
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
    public interface ISaveable
    {
        
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
            SceneManager.LoadScene(data.CurrentLevelName);
        }
        public void ReLoadGame() => LoadGame(data.Name);
        public void DeleteGame(string gameName)
        {
            dataService.Delete(gameName);
        }
    }
    namespace GameDataService
    {
        public interface IGameDataService
        {
            public bool SaveData<T>(string RelativePath, T Data, bool Encrypted);
        
            public T LoadData<T>(string RelativePath, bool Encrypted);
        }
        public class JsonDataService : IGameDataService
        {
            private const string KEY = "";
            private const string IV = "";
            public T LoadData<T>(string RelativePath, bool Encrypted)
            {
                string path = Application.persistentDataPath + RelativePath;
                if (!File.Exists(path))
                {
                    Debug.Log($"Connot load file at path {path}. File does not exits!");
                    throw new FileNotFoundException($"{path} does not exits!");
                }

                try
                {
                    T data;
                    if (Encrypted)
                    {
                        data = ReadEncryptedData<T>(path);
                    }
                    else
                    {
                        data = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
                    }
                    return data;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load data due to: {ex.Message} {ex.StackTrace}");
                    throw ex;
                }
            }

            public bool SaveData<T>(string RelativePath, T Data, bool Encrypted)
            {
                string path = Application.persistentDataPath + RelativePath;
                try
                {
                    if (File.Exists(path))
                    {    
                        Debug.Log("Data exits. Deleting old file and writting a new one");
                        File.Delete(path);
                    }
                    else
                    {
                        Debug.Log("Writing file for the first time");
                    }
                    FileStream stream = File.Create(path);
                    if (Encrypted)
                    {
                        WriteEncryptedData(Data,stream);
                    }
                    else
                    {
                        stream.Close();
                        File.WriteAllText(path, JsonConvert.SerializeObject(Data));
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unable to save data due to:{ex.Message} {ex.StackTrace}");
                    return false;
                }

            }

            private void WriteEncryptedData<T>(T Data, FileStream stream)
            {
                Aes aesProvider =  Aes.Create();
                aesProvider.Key = Convert.FromBase64String(KEY);
                aesProvider.IV = Convert.FromBase64String(IV);
                ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor();
                CryptoStream cryptoStream = new CryptoStream(stream,cryptoTransform, CryptoStreamMode.Write);
                cryptoStream.Write(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(Data)));
            }
            private T ReadEncryptedData<T>(string path)
            {
                byte[] fileBytes = File.ReadAllBytes(path);
                Aes aesProvider = Aes.Create();

                aesProvider.Key = Convert.FromBase64String(KEY);
                aesProvider.IV = Convert.FromBase64String(IV);

                ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor(aesProvider.Key,aesProvider.IV);
                MemoryStream decryptionStream = new MemoryStream(fileBytes);
                CryptoStream cryptoStream = new CryptoStream(decryptionStream,cryptoTransform, CryptoStreamMode.Read);
                StreamReader reader = new StreamReader(cryptoStream);
                string Result = reader.ReadToEnd();

                Debug.Log($"Decrypted result (if the following is not legible, probably wrong key or iv: {Result})");
                return JsonConvert.DeserializeObject<T>(Result);
            }
        }
    }
    
}