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

        private static string GetMasterChunkNumFilePath()
        {
            return Path.Combine(masterFolderPath, "chunknum.dat");
        }

        private static string GetSceneNumFilePath()
        {
            return Path.Combine(sceneFolderPath, "scenenum.dat");
        }

        private static string GetSceneChunkNumFilePath()
        {
            return Path.Combine(sceneFolderPath, "chunknum.dat");
        }

        private static void SetupFramework(int lang)
        {
            int masterchunknum = GetMasterChunkNum();
            Span<int> chunkOffsets = new int[masterchunknum + 1];// 1
            chunkOffsets[0] = 0;
            GetMasterChunkOffsets(chunkOffsets, lang);
            Span<int> dataOffsets = new int[chunkOffsets[chunkOffsets.Length - 1] + 1];// 2
            dataOffsets[0] = 0;
            GetMasterDataOffsets(dataOffsets, chunkOffsets, lang);
            Span<char> data = new char[dataOffsets[dataOffsets.Length - 1]];// 3
            GetMasterData(data, dataOffsets, chunkOffsets, lang);
            //-------------------------------------------------
            int scenenum = GetSceneNum();
            int scenechunknum = GetSceneChunkNum();// 4
            int maxDataOffsetLength = GetSceneMaxDataOffsetLength(scenenum, scenechunknum, lang);// 5
            int maxDataLength = GetSceneMaxDataLength(scenenum, scenechunknum, lang);// 6
            new AlteTextDataFramework(chunkOffsets, dataOffsets, data, scenechunknum, maxDataOffsetLength, maxDataLength);
        }

        private static int GetMasterChunkNum()
        {
            Span<int> data = stackalloc int[1];
            AlteTextDataInput.BinaryReader(GetMasterChunkNumFilePath(), data);
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

        private static int GetSceneNum()
        {
            Span<int> data = stackalloc int[1];
            AlteTextDataInput.BinaryReader(GetSceneNumFilePath(), data);
            return data[0];
        }

        private static int GetSceneChunkNum()
        {
            Span<int> data = stackalloc int[1];
            AlteTextDataInput.BinaryReader(GetSceneChunkNumFilePath(), data);
            return data[0];
        }

        private static int GetSceneMaxDataOffsetLength(int scenenum, int chunknum, int lang)
        {
            Span<int> sums = stackalloc int[scenenum];
            for (int i = 0; i < scenenum; i++)
            {
                int sceneSum = 0;
                for (int j = 0; j < chunknum; j++)
                {
                    string folder = GetSceneChunkFolderPath(i, j, lang);
                    string file = GetLengthFilePath(folder);
                    sceneSum += AlteTextDataInput.BinaryLength(file);
                }
                sums[i] = sceneSum;
            }
            return GetMaxValue(sums);
        }

        private static int GetSceneMaxDataLength(int scenenum, int chunknum, int lang)
        {
            Span<int> sums = stackalloc int[scenenum];
            for (int i = 0; i < scenenum; i++)
            {
                int sceneSum = 0;
                for (int j = 0; j < chunknum; j++)
                {
                    string folder = GetSceneChunkFolderPath(i, j, lang);
                    string file = GetLengthFilePath(folder);
                    Span<int> chunkdata = stackalloc int[AlteTextDataInput.BinaryLength(file)];
                    AlteTextDataInput.BinaryReader(file, chunkdata);
                    sceneSum += GetSum(chunkdata);
                }
                sums[i] = sceneSum;
            }
            return GetMaxValue(sums);
        }

        private static int GetMaxValue(Span<int> target)
        {
            int result = target[0];
            for (int i = 1; i < target.Length; i++)
            {
                if (result < target[i])
                {
                    result = target[i];
                }
            }
            return result;
        }

        private static int GetSum(Span<int> target)
        {
            int result = 0;
            for(int i = 0; i < target.Length; i++)
            {
                result += target[i];
            }
            return result;
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
