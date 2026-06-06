using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;

namespace Alte.Data.Text
{
    public class AlteTextBinaryConverter
    {
        public static AlteTextBinaryConverter Instance;
        private SceneTextData[,] datas;

        public AlteTextBinaryConverter()
        {
            if (Instance != null) return;
            Instance = this;
            datas = new SceneTextData[(int)TextLanguage.Count, (int)TextScene.Count];
            for(int i = 0; i < (int)TextLanguage.Count; i++)
            {
                for(int j = 0; j < (int)TextScene.Count; j++)
                {
                    datas[i, j] = new SceneTextData(i, j, GetScenePath(i, j));
                }
            }
            DataConvert();
        }

        private void DataConvert()
        {
            for(int i = 0; i < datas.GetLength(0); i++)
            {
                int maxChunkNum = 0;
                int maxStringNum = 0;
                int maxCharNum = 0;
                for(int j = 0; j < datas.GetLength(1); j++)
                {
                    SceneTextData data = datas[i, j];
                    int chunkNum = WriteScene(data);
                    int stringNum = WriteChunk(data, chunkNum);
                    int charNum = WriteOffset(data, stringNum);
                    WriteChar(data, charNum);
                    maxChunkNum = GetBiggerNum(maxChunkNum, charNum);
                    maxStringNum = GetBiggerNum(maxStringNum, stringNum);
                    maxCharNum = GetBiggerNum(maxCharNum, charNum);
                }
                WriteLang(i, maxChunkNum, maxStringNum, maxCharNum);
            }
        }

        private void WriteLang(int lang, int maxChunkNum, int  maxStringNum, int maxCharNum)
        {
            Span<int> data = stackalloc int[3];
            data[0] = maxChunkNum;
            data[1] = maxStringNum;
            data[2] = maxCharNum;
            WriteBinary(AlteTextDataIO.GetFilePath(lang, -1, AlteTextDataIO.FileKinds.Lang), data);
        }

        private int WriteScene(SceneTextData target)
        {
            Span<int> data = stackalloc int[1];
            data[0] = target.ChunkNum;
            WriteBinary(AlteTextDataIO.GetFilePath(target.lang, target.scene, AlteTextDataIO.FileKinds.Scene), data);
            return data[0];
        }

        private int WriteChunk(SceneTextData target, int length)
        {
            Span<int> data = stackalloc int[length + 1];
            target.GetStringNum(data);
            WriteBinary(AlteTextDataIO.GetFilePath(target.lang, target.scene, AlteTextDataIO.FileKinds.Chunk), data);
            return data[data.Length - 1];
        }

        private int WriteOffset(SceneTextData target, int length)
        {
            Span<int> data = stackalloc int[length + 1];
            target.GetOffsets(data);
            WriteBinary(AlteTextDataIO.GetFilePath(target.lang, target.scene, AlteTextDataIO.FileKinds.Offsets), data);
            return data[data.Length - 1];
        }

        private void WriteChar(SceneTextData target, int length)
        {
            char[] data = ArrayPool<char>.Shared.Rent(length);
            int cursor = 0;
            for(int i = 0; i < target.data.Length; i++)
            {
                for(int j = 0; j < target.data[i].data.Length; j++)
                {
                    target.data[i].data[j].AsSpan().CopyTo(data.AsSpan(cursor, target.data[i].data[j].Length));
                    cursor += target.data[i].data[j].Length;
                }
            }
            WriteBinary(AlteTextDataIO.GetFilePath(target.lang, target.scene, AlteTextDataIO.FileKinds.Data), data.AsSpan());
            ArrayPool<char>.Shared.Return(data);
        }

        private void WriteBinary<T>(string path, Span<T> data) where T : struct 
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                fs.Write(MemoryMarshal.Cast<T, byte>(data));
            }
        }

        private int GetBiggerNum(int a, int b)
        {
            if(a > b)
            {
                return a;
            }
            else
            {
                return b;
            }
        }

        private Span<string> GetScenePath(int lang, int scene)
        {
            return null;//シーンごとのパスの配列を戻す必要があります。ご自身で適切な形で書き換えてください。
        }
    }

    public readonly struct SceneTextData
    {
        public readonly int lang;
        public readonly int scene;
        public readonly ChunkTextData[] data;
        public int ChunkNum { get { return data.Length; } }

        public SceneTextData(int lang, int scene, Span<string> pathes)
        {
            this.lang = lang;
            this.scene = scene;
            data = new ChunkTextData[pathes.Length];
            for (int i = 0; i < pathes.Length; i++)
            {
                data[i] = new ChunkTextData(this.lang, this.scene, i, pathes[i]);
            }
        }

        public void GetOffsets(Span<int> result)
        {
            int counter = 0;
            result[0] = 0;
            for (int i = 0; i < data.Length; i++)
            {
                int stringNum = data[i].StringNum;
                data[i].GetOffsets(result.Slice(counter, stringNum));
                counter += stringNum - 1;
            }
        }

        public void GetStringNum(Span<int> result)
        {
            result[0] = 0;
            for (int i = 0; i < data.Length; i++)
            {
                result[i + 1] = result[i] + data[i].StringNum;
            }
        }
    }

    public readonly struct ChunkTextData
    {
        public readonly int lang;
        public readonly int scene;
        public readonly int chunk;
        public readonly string[] data;
        public int StringNum { get { return data.Length; } }

        public ChunkTextData(int lang, int scene, int chunk, string path)
        {
            this.lang = lang;
            this.scene = scene;
            this.chunk = chunk;
            data = File.ReadAllLines(path);//現在は一行を一つのstringとして読み出しています。記述形式を変える際はここを弄ってください
        }

        public void GetOffsets(Span<int> result)
        {
            for (int i = 0; i < data.Length; i++)
            {
                result[i + 1] = result[i] + data[i].Length;
            }
        }
    }
}