using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Alte.Data.Text
{
    public static class AlteTextDataIO
    {
        private static readonly string rootFolderPath = Path.Combine(Application.streamingAssetsPath, "AlteDataFramework", "Text");

        private static string GetFilePath(int lang, int scene, FileKinds kinds)
        {
            string s = string.Empty;
            if(scene < 0)
            {
                s = "Master";
            }
            else
            {
                s = "Scene" + scene;
            }

            switch (kinds)
            {
                case FileKinds.Lang:
                    return Path.Combine(rootFolderPath, "Lang" + lang, "lang.dat");//最大シーンチャンク数・最大シーンチャンク内string個数・最大シーンcharデータ数
                case FileKinds.Scene:
                    return Path.Combine(rootFolderPath, "Lang" + lang, s, "scene.dat");//シーン単位(チャンク個数)
                case FileKinds.Chunk:
                    return Path.Combine(rootFolderPath, "Lang" + lang, s, "chunk.dat");//チャンク単位(string個数)
                case FileKinds.Offsets:
                    return Path.Combine(rootFolderPath, "Lang" + lang, s, "offsets.dat");//string単位(char個数)
                case FileKinds.Data:
                    return Path.Combine(rootFolderPath, "Lang" + lang, s, "data.dat");//char単位(char単体)
            }
            return null;
        }

        public static void Initialize(int lang)
        {
            SceneDataStruct data = LoadSceneData(lang, -1);
            try
            {
                Span<int> scenedata = stackalloc int[3];
                AlteTextDataInput.BinaryReader(GetFilePath(lang, 0, FileKinds.Lang), scenedata);
                new AlteTextDataFramework(data.Chunk, data.Offset, data.Data, scenedata[0] + 1, scenedata[1] + 1, scenedata[2], lang);
            }
            finally
            {
                data.Dispose();
            }
        }

        public static void LoadScene(int scene)
        {
            SceneDataStruct data = LoadSceneData(AlteTextDataFramework.Instance.Language, scene);
            try
            {
                AlteTextDataFramework.Instance.SetSceneData(data.Chunk, data.Offset, data.Data, scene);
            }
            finally
            {
                data.Dispose();
            }
        }

        public static void ReloadLanguage(int lang)
        {
            int scene = AlteTextDataFramework.Instance.Scene;
            Initialize(lang);
            LoadScene(scene);
        }

        private static SceneDataStruct LoadSceneData(int lang, int scene)
        {
            int chunkLength = AlteTextDataInput.PointBinaryReader(GetFilePath(lang, scene, FileKinds.Scene)) + 1;
            int[] Chunk = ArrayPool<int>.Shared.Rent(chunkLength);
            AlteTextDataInput.BinaryReader(GetFilePath(lang, scene, FileKinds.Chunk), Chunk.AsSpan(0, chunkLength));

            int offsetLength = Chunk[chunkLength - 1] + 1;
            int[] Offset = ArrayPool<int>.Shared.Rent(offsetLength);
            AlteTextDataInput.BinaryReader(GetFilePath(lang, scene, FileKinds.Offsets), Offset.AsSpan(0, offsetLength));

            int dataLength = Offset[offsetLength - 1];
            char[] Data = ArrayPool<char>.Shared.Rent(dataLength);
            AlteTextDataInput.BinaryReader(GetFilePath(lang, scene, FileKinds.Data), Data.AsSpan(0, dataLength));

            return new SceneDataStruct(Chunk, chunkLength, Offset, offsetLength, Data, dataLength);
        }

        private enum FileKinds
        {
            Lang,
            Scene,
            Chunk,
            Offsets,
            Data
        }

        private readonly ref struct SceneDataStruct
        {
            private readonly int[] chunkArray;
            private readonly int[] offsetArray;
            private readonly char[] dataArray;

            public readonly Span<int> Chunk;
            public readonly Span<int> Offset;
            public readonly Span<char> Data;

            public SceneDataStruct(int[] chunk, int chunkLength, int[] offset, int offsetLength, char[] data, int dataLength)
            {
                chunkArray = chunk;
                Chunk = chunkArray.AsSpan(0, chunkLength);
                offsetArray = offset;
                Offset = offsetArray.AsSpan(0, offsetLength);
                dataArray = data;
                Data = dataArray.AsSpan(0, dataLength);
            }

            public void Dispose()
            {
                if (chunkArray != null) ArrayPool<int>.Shared.Return(chunkArray);
                if (offsetArray != null) ArrayPool<int>.Shared.Return(offsetArray);
                if (dataArray != null) ArrayPool<char>.Shared.Return(dataArray);
            }
        }
    }

    public static class AlteTextDataInput
    {
        public static int PointBinaryReader(string path)
        {
            Span<int> data = stackalloc int[1];
            BinaryReader(path, data);
            return data[0];
        }

        public static void BinaryReader<T>(string path, Span<T> result) where T : struct
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fs.Read(MemoryMarshal.AsBytes(result));
            }
        }
    }
}
