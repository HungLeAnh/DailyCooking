using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

// Shared JSON settings and crash-safe file IO for save files and the join snapshot.
public static class SaveJson
{
    private const string TempSuffix = ".tmp";
    private const string BackupSuffix = ".bak";

    public static JsonSerializerSettings CreateSettings()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CustomContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Error = (sender, args) =>
            {
                // The handler fires once per nesting level; log only where the error happened.
                if (args.CurrentObject == args.ErrorContext.OriginalObject)
                    Debug.LogWarning($"[SaveJson] Skipped '{args.ErrorContext.Path}': {args.ErrorContext.Error.Message}");
                args.ErrorContext.Handled = true;
            },
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = new SaveTypeBinder(),
            Formatting = Formatting.None
        };
        settings.Converters.Add(new UniversalUnityConverter());
        return settings;
    }

    // Writes to a temp file first so a crash mid-write never corrupts the existing save.
    // The previous save is kept as <file>.bak.
    public static void WriteAtomic(string fullPath, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        string tempPath = fullPath + TempSuffix;
        string backupPath = fullPath + BackupSuffix;
        File.WriteAllText(tempPath, contents);

        if (!File.Exists(fullPath))
        {
            File.Move(tempPath, fullPath);
            return;
        }
        try
        {
            File.Replace(tempPath, fullPath, backupPath);
        }
        catch (Exception e) when (e is PlatformNotSupportedException || e is IOException)
        {
            File.Copy(fullPath, backupPath, true);
            File.Delete(fullPath);
            File.Move(tempPath, fullPath);
        }
    }

    // Reads <file>, falling back to <file>.bak when the main file is missing or unreadable.
    public static T ReadWithBackup<T>(string fullPath, JsonSerializerSettings settings) where T : class
    {
        T data = TryRead<T>(fullPath, settings);
        if (data != null)
            return data;

        string backupPath = fullPath + BackupSuffix;
        data = TryRead<T>(backupPath, settings);
        if (data != null)
            Debug.LogWarning($"[SaveJson] Main save unreadable, loaded backup: {backupPath}");
        return data;
    }

    private static T TryRead<T>(string path, JsonSerializerSettings settings) where T : class
    {
        if (!File.Exists(path))
            return null;
        try
        {
            // Parse strictly first: the lenient Error handler would otherwise turn a truncated
            // or corrupt file into a half-empty object instead of falling back to the backup.
            JToken token = JToken.Parse(File.ReadAllText(path));
            return token.ToObject<T>(JsonSerializer.Create(settings));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveJson] Error loading {path}: {e}");
            return null;
        }
    }

    // TypeNameHandling.Auto trusts "$type" from the JSON, and the join snapshot comes from
    // another device, so only game types, primitives and generic collections may be created.
    private class SaveTypeBinder : ISerializationBinder
    {
        private static readonly DefaultSerializationBinder Inner = new DefaultSerializationBinder();

        public Type BindToType(string assemblyName, string typeName)
        {
            Type type = Inner.BindToType(assemblyName, typeName);
            if (!IsAllowed(type))
                throw new JsonSerializationException($"Type not allowed in save data: {typeName}");
            return type;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            Inner.BindToName(serializedType, out assemblyName, out typeName);
        }

        private static bool IsAllowed(Type type)
        {
            if (type == null)
                return false;
            if (type.IsArray)
                return IsAllowed(type.GetElementType());
            if (type.IsGenericType)
            {
                foreach (Type argument in type.GetGenericArguments())
                {
                    if (!IsAllowed(argument))
                        return false;
                }
                type = type.GetGenericTypeDefinition();
            }
            if (type.Assembly == typeof(GameData).Assembly)
                return true;
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid))
                return true;
            if (type.Namespace == "System.Collections.Generic")
                return true;
            return type.Namespace == "UnityEngine" && type.IsValueType;
        }
    }
}
