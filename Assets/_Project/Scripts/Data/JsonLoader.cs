using System.IO;
using UnityEngine;

namespace TheLastCup.Data
{
    public class JsonLoader
    {
        public T LoadFromStreamingAssets<T>(string relativePath) where T : class
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, "Data", relativePath);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[JsonLoader] Config not found: {fullPath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(fullPath);
                return JsonUtility.FromJson<T>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[JsonLoader] Failed to parse {relativePath}: {ex.Message}");
                return null;
            }
        }
    }
}
