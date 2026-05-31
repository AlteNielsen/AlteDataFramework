using System;

namespace Alte.Data.Text
{
    public class AlteTextDataFramework
    {
        public static AlteTextDataFramework Instance { get; private set; }
        private char[] buffer;
        private int[] masterOffsets;
        private int[] sceneOffsets;
        private int border;
        private int[] masterChunkOffsets;
        private int[] sceneChunkOffsets;
        public int Language { get; private set; }
        public int Scene { get; private set; }

        public AlteTextDataFramework(Span<int> masterChunkOffsetData, Span<int> masterOffsetData, Span<char> masterData, int maxSceneChunkOffsetDataLength, int maxSceneOffsetDataLength, int maxSceneDataLength, int lang)
        {
            Instance = null;
            Instance = this;
            masterOffsets = new int[masterOffsetData.Length];
            masterOffsetData.CopyTo(masterOffsets.AsSpan());
            buffer = new char[masterOffsets[masterOffsets.Length - 1] + maxSceneDataLength];
            masterData.CopyTo(buffer);
            border = masterOffsets[masterOffsets.Length - 1];
            sceneOffsets = new int[maxSceneOffsetDataLength];
            masterChunkOffsets = new int[masterChunkOffsetData.Length];
            masterChunkOffsetData.CopyTo(masterChunkOffsets.AsSpan());
            sceneChunkOffsets = new int[maxSceneChunkOffsetDataLength];
            Language = lang;
        }

        public void SetSceneData(Span<int> chunkOffsetData, Span<int> offsetData, Span<char> sceneData, int scene)
        {
            sceneOffsets.AsSpan().Clear();
            offsetData.CopyTo(sceneOffsets.AsSpan());
            buffer.AsSpan(border, buffer.Length - border).Clear();
            sceneData.CopyTo(buffer.AsSpan(border, buffer.Length - border));
            sceneChunkOffsets.AsSpan().Clear();
            chunkOffsetData.CopyTo(sceneChunkOffsets.AsSpan());
            this.Scene = scene;
        }

        public ReadOnlySpan<char> GetText(TextPointer pointer)
        {
            if(pointer.isMaster)
            {
                int offsetIndex = masterChunkOffsets[pointer.chunk] + pointer.index;
                return buffer.AsSpan(masterOffsets[offsetIndex], masterOffsets[offsetIndex + 1] - masterOffsets[offsetIndex]);
            }
            else
            {
                int offsetIndex = sceneChunkOffsets[pointer.chunk] + pointer.index;
                return buffer.AsSpan(sceneOffsets[offsetIndex] + border, sceneOffsets[offsetIndex + 1] - sceneOffsets[offsetIndex]);
            }
        }
    }

    public readonly ref struct TextPointer
    {
        public readonly bool isMaster;
        public readonly int chunk;
        public readonly int index;

        public static TextPointer Master { get { return new TextPointer(true, 0, 0); } }
        public static TextPointer Scene { get { return new TextPointer(false, 0, 0); } }

        public TextPointer(bool isMaster, int chunk, int index)
        {
            this.isMaster = isMaster;
            this.chunk = chunk;
            this.index = index;
        }

        public TextPointer Point(int index)
        {
            return new TextPointer(isMaster, chunk, index);
        }

        public TextPointer Chunk(int chunk)
        {
            return new TextPointer(isMaster, chunk, index);
        }

        public ReadOnlySpan<char> Char
        {
            get
            {
                return AlteTextDataFramework.Instance.GetText(this);
            }
        }

        public string String
        {
            get
            {
                return Char.ToString();
            }
        }
    }
}
