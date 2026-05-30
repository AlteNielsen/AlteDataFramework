using System.IO;
using UnityEngine;

public static class AlteTextDataIO
{
    private static readonly string masterFolderPath = Path.Combine(Application.streamingAssetsPath, "AlteDataFramework", "Text", "Master");
    private static readonly string sceneFolderPath = Path.Combine(Application.streamingAssetsPath, "AlteDataFramework", "Text", "Scene");

    private static string GetMasterChunkFolderPath(int chunk, int lang)
    {
        return Path.Combine(masterFolderPath, "chunk" + chunk + "lang" + lang);
    }

    private static string GetSceneChunkFolderPath(int scene, int chunk, int lang)
    {
        return Path.Combine(sceneFolderPath, "scene" + scene, "chunk" + chunk + "lang" + lang);
    }

    private static string GetLengthFilePath(string folderPath)
    {
        return Path.Combine(folderPath, "leng.dat");
    }

    private static string GetDataFilePath(string folderPath)
    {
        return Path.Combine(folderPath, "data.dat");
    }
}
