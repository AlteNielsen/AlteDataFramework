using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Alte.Data.Text
{
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

        private static string GetMasterChunkNumFilePath(int lang)
        {
            return Path.Combine(masterFolderPath, "chunknum" + lang + ".dat");
        }

        private static string GetSceneChunkNumFilePath(int lang)
        {
            return Path.Combine(sceneFolderPath, "chunknum" + lang + ".dat");
        }

        private static void LoadMasterTexts(int lang)
        {
            int chunknum = GetMasterChunkNum(lang);
            Span<int> chunkOffsets = new int[chunknum + 1];
            chunkOffsets[0] = 0;
            GetMasterChunkOffsets(chunkOffsets, lang);
            Span<int> dataOffsets = new int[chunkOffsets[chunkOffsets.Length - 1] + 1];
            dataOffsets[0] = 0;
            GetMasterDataOffsets(dataOffsets, chunkOffsets, lang);
            Span<char> data = new char[dataOffsets[dataOffsets.Length - 1]];
            GetMasterData(data, dataOffsets, chunkOffsets, lang);
        }

        private static int GetMasterChunkNum(int lang)
        {
            Span<int> data = stackalloc int[1];
            AlteTextDataInput.BinaryReader(GetMasterChunkNumFilePath(lang), data);
            return data[0];
        }

        private static void GetMasterChunkOffsets(Span<int> result, int lang)
        {
            for(int i = 0; i < result.Length - 1; i++)
            {
                string folder = GetMasterChunkFolderPath(i, lang);
                string file = GetLengthFilePath(folder);
                result[i + 1] = result[i] + AlteTextDataInput.BinaryLength(file);
            }
        }

        private static void GetMasterDataOffsets(Span<int> result, Span<int> chunkOffsets, int lang)
        {
            for(int i = 0; i < chunkOffsets.Length - 1; ++i)
            {
                string folder = GetMasterChunkFolderPath(i, lang);
                string file = GetLengthFilePath(folder);
                Span<int> lengthes = stackalloc int[chunkOffsets[i + 1] - chunkOffsets[i]];
                AlteTextDataInput.BinaryReader(file, lengthes);
                for(int j = 0; j < lengthes.Length; j++)
                {
                    result[chunkOffsets[i] + j + 1] = result[chunkOffsets[i] + j] + lengthes[j];
                }
            }
        }

        private static void GetMasterData(Span<char> result, Span<int> dataOffsets, Span<int> chunkOffsets, int lang)
        {
            for(int i = 0; i < chunkOffsets.Length - 1;  i++)
            {
                string folder = GetMasterChunkFolderPath(i, lang);
                string file = GetDataFilePath(folder);
                int start = dataOffsets[chunkOffsets[i]];
                int end = dataOffsets[chunkOffsets[i + 1]];
                AlteTextDataInput.BinaryReader(file, result.Slice(start, end - start));
            }
        }
    }

    public static class AlteTextDataInput
    {
        public static void BinaryReader<T>(string path, Span<T> result) where T : struct
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fs.Read(MemoryMarshal.AsBytes(result));
            }
        }

        public static int BinaryLength(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return (int)(fs.Length / 4);
            }
        }
    }
}
