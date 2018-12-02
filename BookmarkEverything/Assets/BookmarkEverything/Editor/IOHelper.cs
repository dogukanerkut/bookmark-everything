using System;
using System.IO;
using UnityEngine;

namespace BookmarkEverything
{
    public static class IOHelper
    {
        public static void WriteToDisk(string fileName, object serializeObject)
        {
            string str = JsonUtility.ToJson(serializeObject);
            string path = Application.persistentDataPath + "/" + fileName + ".dat";
            File.AppendAllText(path, str + Environment.NewLine);
        }
        public static T ReadFromDisk<T>(string fileName)
        {
            string path = Application.persistentDataPath + "/" + fileName + ".dat";
            T returnObject = default(T);
            if (File.Exists(path))
            {
                using (StreamReader streamReader = new StreamReader(path))
                {
                    string line;
                    while (!string.IsNullOrEmpty(line = streamReader.ReadLine()))
                    {
                        returnObject = Deserialize<T>(line);
                    }
                }
            }
            return returnObject;
        }
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
    }
}