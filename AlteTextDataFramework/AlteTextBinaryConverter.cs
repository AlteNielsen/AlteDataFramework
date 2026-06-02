using System;
using UnityEngine;
using System.Collections.Generic;

public class AlteTextBinaryConverter
{
    public static AlteTextBinaryConverter Instance;

}

public readonly struct SceneTextData
{
    public readonly int lang;
    public readonly int scene;
    public readonly List<ChunkTextData> data;
    public int ChunkNum { get {  return data.Count; } }

    public SceneTextData(int lang, int scene, List<ChunkTextData> data)
    {
        this.lang = lang;
        this.scene = scene;
        this.data = data;
    }

    public int[] GetOffsets()
    {
        int[] result = new int[GetStringNum() + 1];
        int counter = 0;
        for(int i = 0; i < data.Count; i++)
        {
            int stringNum = data[i].StringNum;
            data[i].GetOffsets(result.AsSpan(counter, stringNum));
            counter += stringNum - 1;
        }
        return result;
    }

    private int GetStringNum()
    {
        int result = 0;
        for (int i = 0; i < data.Count; i++)
        {
            result += data[i].StringNum;
        }
        return result;
    }
}

public readonly struct ChunkTextData
{
    public readonly int lang;
    public readonly int scene;
    public readonly int chunk;
    public readonly string[] data;
    public int StringNum { get { return data.Length; } }

    public ChunkTextData(int lang, int scene, int chunk, string[] data)
    {
        this.lang = lang;
        this.scene = scene;
        this.chunk = chunk;
        this.data = data;
    }

    public void GetOffsets(Span<int> result)
    {
        for(int i = 0; i < data.Length; i++)
        {
            result[i + 1] = result[i] + data[i].Length;
        }
    }
}