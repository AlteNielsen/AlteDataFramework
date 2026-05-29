using System;

public class AlteTextDataFramework
{
    public static AlteTextDataFramework Instance { get; private set; }
    private char[] buffer;
    private int[] masterOffsets;
    private int[] sceneOffsets;
    private int border;
    private int[] masterChunkOffsets;
    private int[] sceneChunkOffsets;

    public AlteTextDataFramework(Span<int> masterOffsetData, int maxSceneDataLength, int maxSceneOffsetDataLength, Span<char> masterData, Span<int> masterChunkOffsetData, int maxSceneChunkOffsetDataLength)
    {
        masterOffsets = new int[masterOffsetData.Length];
        masterOffsetData.CopyTo(masterOffsets.AsSpan());
        buffer = new char[masterOffsets[masterOffsets.Length - 1] + maxSceneDataLength];
        masterData.CopyTo(buffer);
        border = masterOffsets[masterOffsets.Length - 1];
        sceneOffsets = new int[maxSceneOffsetDataLength];
        masterChunkOffsets = new int[masterChunkOffsetData.Length];
        masterChunkOffsetData.CopyTo(masterChunkOffsets.AsSpan());
        sceneChunkOffsets = new int[maxSceneChunkOffsetDataLength];
    }

    public void SetSceneData(Span<int> offsetData, Span<char> sceneData, Span<int> chunkOffsetData)
    {
        sceneOffsets.AsSpan().Clear();
        offsetData.CopyTo(sceneOffsets.AsSpan());
        buffer.AsSpan(border, buffer.Length - border).Clear();
        sceneData.CopyTo(buffer.AsSpan(border, buffer.Length - border));
        sceneChunkOffsets.AsSpan().Clear();
        chunkOffsetData.CopyTo(sceneChunkOffsets.AsSpan());
    }
}
