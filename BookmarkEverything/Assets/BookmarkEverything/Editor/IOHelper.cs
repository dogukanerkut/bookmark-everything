using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BookmarkEverything
{
    public static class IOHelper
    {
        public static void WriteToDisk(string fileName, object serializeObject)
        {

#if UNITY_5_4_OR_NEWER
            string json = JsonUtility.ToJson(serializeObject);
            StoreData(json);
#else
            string path = Application.persistentDataPath + "/" + fileName + ".dat";
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
			bf.Serialize(fs, serializeObject);
			fs.Close();
#endif
		}
        public static T ReadFromDisk<T>(string fileName)
        {
#if UNITY_5_4_OR_NEWER
            var userData = GetStoredData();
            return JsonUtility.FromJson<T>(userData);
#else
            string path = Application.persistentDataPath + "/" + fileName + ".dat";
            T returnObject = default(T);
            if (File.Exists(path))
            {
				FileStream fs = new FileStream(path, FileMode.Open);
				System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				fs.Seek(0, SeekOrigin.Begin);
				returnObject = (T)bf.Deserialize(fs);
				fs.Close();
			}
			return returnObject;
#endif
        }

        static void StoreData(string bookmarkData)
        {
            var dir = GetDataPath();
            var imp = AssetImporter.GetAtPath(dir);
            imp.userData = bookmarkData;
            imp.SaveAndReimport();
        }

        static string GetStoredData()
        {
            var dir = GetDataPath();
            var imp = AssetImporter.GetAtPath(dir);
            return imp.userData;
        }

        static string GetDataPath()
        {
            var path = AssetDatabase.FindAssets("BookmarkEverything")
                .ToList()
                .ConvertAll(p => AssetDatabase.GUIDToAssetPath(p))
                .Where(file => Directory.Exists(file))
                .FirstOrDefault();
            return path;
        }


#if UNITY_5_4_OR_NEWER
        /// <summary>
        /// filename arg kept in newer version to ensure compatibility with older builds
        /// </summary>
        /// <param name="fileName"></param>
        public static void ClearData(string fileName)
        {
            var dir = GetDataPath();
            var imp = AssetImporter.GetAtPath(dir);
            imp.userData = string.Empty;
            imp.SaveAndReimport();
        }
#else
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
#endif
        
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