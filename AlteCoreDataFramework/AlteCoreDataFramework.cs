using System;
using System.Runtime.InteropServices;

namespace Alte.Data.Core
{
    public class AlteCoreDataFramework
    {
        public static AlteCoreDataFramework Instance { get; private set; }
        public static int SceneBufferPos;
        private int[] masterBuffer;
        private int[] sceneBuffer;
        private int[] offsets;
        private int readOnlyBorder;

        public AlteCoreDataFramework(Span<int> offsetData, int border)
        {
            if(Instance == null)
            {
                Instance = this;
            }
            masterBuffer = new int[offsetData[offsetData.Length - 1]];
            offsets = new int[offsetData.Length + 1];
            offsetData.CopyTo(offsets);
            readOnlyBorder = border;
            SceneBufferPos = masterBuffer.Length;
        }

        public void SetRawData(DataPointer pointer, Span<int> fileData)
        {
            fileData.CopyTo(MasterBuffer(pointer));
        }

        public void SetIntData(DataPointer pointer, ReadOnlySpan<int> data)
        {
            if (offsets[readOnlyBorder] >= pointer.offset) throw new Exception("書き込み不可領域です");
            if (pointer.offset >= masterBuffer.Length)
            {
                data.CopyTo(sceneBuffer.AsSpan(pointer.offset - masterBuffer.Length, data.Length));
            }
            data.CopyTo(MasterBuffer(pointer));
        }

        public void SetSceneBuffer(int[] buffer)
        {
            sceneBuffer = buffer;
            offsets[offsets.Length - 1] = offsets[offsets.Length - 2] + buffer.Length;
        }

        public int GetOffset(DataChunk index)
        {
            return offsets[(int)index];
        }

        public int GetLength(DataChunk index)
        {
            return offsets[(int)index + 1] - offsets[(int)index];
        }

        public int GetMasterBufferLength()
        {
            return masterBuffer.Length;
        }

        public int GetSceneBufferLength()
        {
            return sceneBuffer.Length;
        }

        public ReadOnlySpan<int> GetData(DataPointer pointer)
        {
            if (pointer.offset >= masterBuffer.Length)
            {
                return sceneBuffer.AsSpan(pointer.offset - masterBuffer.Length, pointer.length);
            }
            return MasterBuffer(pointer);
        }

        private Span<int> MasterBuffer(DataPointer pointer)
        {
            return masterBuffer.AsSpan(pointer.offset, pointer.length);
        }

        public void SaveAll(int slotIndex)
        {
            for(int i = readOnlyBorder; i < offsets.Length - 2; i++)
            {
                Save((DataChunk)i, slotIndex);
            }
        }

        public void Save(DataChunk chunk, int slotIndex)
        {
            AlteCoreDataIO.Save((int)chunk, masterBuffer.AsSpan(GetOffset(chunk), GetLength(chunk)), slotIndex);
        }

        public void Initialize(DataChunk chunk)
        {
            AlteCoreDataIO.Initialize((int)chunk, masterBuffer.AsSpan(GetOffset(chunk), GetLength(chunk)));
        }
    }

    public readonly struct DataPointer
    {
        public readonly int offset;
        public readonly int length;

        public static DataPointer Master { get { return new DataPointer(0, AlteCoreDataFramework.Instance.GetMasterBufferLength()); } }
        public static DataPointer Scene { get { return new DataPointer(AlteCoreDataFramework.SceneBufferPos, AlteCoreDataFramework.Instance.GetSceneBufferLength()); } }
        public ReadOnlySpan<int> Data { get { return AlteCoreDataFramework.Instance.GetData(this); } }


        public DataPointer(int off, int leng)
        {
            offset = off; 
            length = leng; 
        }

        public DataPointer Point(DataChunk chunk, int index)
        {
            return new DataPointer(AlteCoreDataFramework.Instance.GetOffset(chunk) + index, 1);
        }

        public DataPointer Point(int index)
        {
            return new DataPointer(offset + index, 1);
        }

        public DataPointer Array(DataChunk chunk, int index, int length)
        {
            return new DataPointer(AlteCoreDataFramework.Instance.GetOffset(chunk) + index, length);
        }

        public DataPointer Array(int index, int length)
        {
            return new DataPointer(offset + index, length);
        }

        public DataPointer Chunk(DataChunk chunk)
        {
            return new DataPointer(AlteCoreDataFramework.Instance.GetOffset(chunk), AlteCoreDataFramework.Instance.GetLength(chunk));
        }

        public ReadOnlySpan<int> Int
        {
            get
            {
                return AlteCoreDataFramework.Instance.GetData(this);
            }
        }

        public ReadOnlySpan<float> Float
        {
            get
            {
                ReadOnlySpan<int> rawdata = AlteCoreDataFramework.Instance.GetData(this);
                return MemoryMarshal.Cast<int, float>(rawdata);
            }
        }

        public ReadOnlyBitFlags Bool
        {
            get
            {
                ReadOnlySpan<int> data = AlteCoreDataFramework.Instance.GetData(this);
                return new ReadOnlyBitFlags(data);
            }
        }

        public void Set(Span<int> value)
        {
            AlteCoreDataFramework.Instance.SetIntData(this, value);
        }

        public void Set(Span<float> value)
        {
            ReadOnlySpan<int> rawdata = MemoryMarshal.Cast<float, int>(value);
            AlteCoreDataFramework.Instance.SetIntData(this, rawdata);
        }

        public void Set(BitFlags value)
        {
            AlteCoreDataFramework.Instance.SetIntData(this, value.flags);

        }

        public void CopyTo(DataPointer destination)
        {
            AlteCoreDataFramework.Instance.SetIntData(destination, Data);
        }
    }

    public ref struct BitFlags
    {
        public readonly Span<int> flags;

        public BitFlags(Span<int> value)
        {
            flags = value;
        }

        public bool this[int index]
        {
            get => (flags[index / 32] & (1 << (index % 32))) != 0;
            set => flags[index / 32] = value ? (flags[index / 32] | (1 << (index % 32))) : (flags[index / 32] & ~(1 << (index % 32)));
        }
    }

    public ref struct ReadOnlyBitFlags
    {
        private readonly ReadOnlySpan<int> flags;

        public ReadOnlyBitFlags(ReadOnlySpan<int> value) => flags = value;

        public bool this[int index]
        {
            get => (flags[index / 32] & (1 << (index % 32))) != 0;
        }
    }

    public enum DataChunk//ユーザーさん自身が直接書き込んでください
    {
        Count//DataChunkの総数を調べるためのものにつき、書き換え不可。要素を追加したいときはこれより前に追加してください
    }
}