using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BookmarkEverything
{
    public static class IOHelper
    {
        public static void WriteToDisk(string fileName, object serializeObject)
        {
            string path = Application.persistentDataPath + "/" + fileName + ".dat";
#if UNITY_5_4_OR_NEWER
			string str = JsonUtility.ToJson(serializeObject);
            File.AppendAllText(path, str + Environment.NewLine);
#else
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
			bf.Serialize(fs, serializeObject);
			fs.Close();
#endif
		}
        public static T ReadFromDisk<T>(string fileName)
        {
            string path = Application.persistentDataPath + "/" + fileName + ".dat";
            T returnObject = default(T);
            if (File.Exists(path))
            {
#if UNITY_5_4_OR_NEWER
				using (StreamReader streamReader = new StreamReader(path))
                {
                    string line;
                    while (!string.IsNullOrEmpty(line = streamReader.ReadLine()))
                    {
                        returnObject = Deserialize<T>(line);
                    }
                }
#else
				FileStream fs = new FileStream(path, FileMode.Open);
				System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				fs.Seek(0, SeekOrigin.Begin);
				returnObject = (T)bf.Deserialize(fs);
				fs.Close();
#endif
			}
			return returnObject;
        }
#if UNITY_5_4_OR_NEWER
		private static T Deserialize<T>(string text)
        {
            text = text.Trim();
            Type typeFromHandle = typeof(T);
            object obj = null;
            try
            {
                obj = JsonUtility.FromJson<T>(text);
            }
            catch (Exception ex)
            {
                Debug.LogError("Cannot deserialize to type " + typeFromHandle.ToString() + ": " + ex.Message + ", Json string: " + text);
            }
            if (obj != null && obj.GetType() == typeFromHandle)
            {
                return (T)obj;
            }
            return default(T);
        }
#endif
		public static void ClearData(string fileName)
        {
            string path = Application.persistentDataPath + "/" + fileName + ".dat";
            if (File.Exists(path))
            {
                using (FileStream fileStream = File.Open(path, FileMode.Open))
                {
                    fileStream.SetLength(0L);
                }
            }
        }
        
        public static bool Exists(string value, ExistentialCheckStrategy strategy =  ExistentialCheckStrategy.Path)
        {
            if (strategy == ExistentialCheckStrategy.GUID)
            {
                value = AssetDatabase.GUIDToAssetPath(value);
            }
            bool existsDir = Directory.Exists(value);
            bool existsFile = File.Exists(value);
            return existsDir || existsFile;
        }
        public static bool IsFolder(string value, ExistentialCheckStrategy strategy = ExistentialCheckStrategy.Path)
        {
             if (strategy == ExistentialCheckStrategy.GUID)
            {
                value = AssetDatabase.GUIDToAssetPath(value);
            }
            return Directory.Exists(value);
        }

    }
public enum ExistentialCheckStrategy // :)
{
    Path,
    GUID
}
}