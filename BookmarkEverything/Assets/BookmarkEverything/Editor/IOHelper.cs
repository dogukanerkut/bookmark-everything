/*
MIT License

Copyright (c) 2018 Doğukan Erkut

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
-------------------------------------------------------------------------------

Repository: https://github.com/dogukanerkut/bookmark-everything
Contact: dogukanerkut@gmail.com
 */
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