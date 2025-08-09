using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace SaveAndLoadsystem.GameDataService
{
    public interface IGameDataService
    {
        public bool SaveData<T>(string RelativePath, T Data, bool Encrypted);

        public T LoadData<T>(string RelativePath, bool Encrypted);
    }

    public class JsonDataService : IGameDataService
    {
        // IMPORTANT: These keys should be loaded securely, not hardcoded.
        // For demonstration purposes, they are hardcoded here.
        private const string KEY = "YOUR_SECURE_KEY_HERE"; 
        private const string IV = "YOUR_SECURE_IV_HERE";

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
                    WriteEncryptedData(Data, stream);
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
            Aes aesProvider = Aes.Create();
            aesProvider.Key = Convert.FromBase64String(KEY);
            aesProvider.IV = Convert.FromBase64String(IV);
            ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor();
            CryptoStream cryptoStream = new CryptoStream(stream, cryptoTransform, CryptoStreamMode.Write);
            cryptoStream.Write(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(Data)));
        }

        private T ReadEncryptedData<T>(string path)
        {
            byte[] fileBytes = File.ReadAllBytes(path);
            Aes aesProvider = Aes.Create();

            aesProvider.Key = Convert.FromBase64String(KEY);
            aesProvider.IV = Convert.FromBase64String(IV);

            ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor(aesProvider.Key, aesProvider.IV);
            MemoryStream decryptionStream = new MemoryStream(fileBytes);
            CryptoStream cryptoStream = new CryptoStream(decryptionStream, cryptoTransform, CryptoStreamMode.Read);
            StreamReader reader = new StreamReader(cryptoStream);
            string Result = reader.ReadToEnd();

            Debug.Log($"Decrypted result (if the following is not legible, probably wrong key or iv: {Result})");
            return JsonConvert.DeserializeObject<T>(Result);
        }
    }
}
