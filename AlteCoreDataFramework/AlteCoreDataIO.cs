using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Alte.Data.Core
{
    public static class AlteCoreDataIO
    {
        public static readonly string masterArrayLengthFilePath = Path.Combine(Application.streamingAssetsPath, "AlteDataFramework", "Core", "Master", "leg,dat");
        public static readonly string masterFolderPath = Path.Combine(Application.streamingAssetsPath, "AlteDataFramework", "Core", "Master");

        public static readonly string saveArrayLengthFilePath = Path.Combine(Application.streamingAssetsPath, "AlteDataFramework", "Core", "Save", "leg,dat");
        private static readonly string saveFolderPath = Path.Combine(Application.persistentDataPath, "AlteDataFramework", "Core", "Save", "Save");
        public static readonly string originalSaveFolderPath = Path.Combine(Application.streamingAssetsPath, "AlteDataFramework", "Core", "Save", "Original");

        private const int SaveSlotNum = 0;
        private static int[] chunkOffsets;
        private static int[] allData;
        private static int border;
        private static int saveSlot;

        public static void CleanLoad(int savedataSlotNum)
        {
            #if UNITY_EDITOR
            new AlteCoreBinaryConverter();
            AlteCoreBinaryConverter.Instance = null;
            #endif
            Load(savedataSlotNum);
        }

        public static void Load(int savedataSlotNum)
        {
            saveSlot = savedataSlotNum;
            if(chunkOffsets == null)
            {
                Setup();
            }
            LoadSaveData(savedataSlotNum);
            if(AlteCoreDataFramework.Instance == null)
            {
                new AlteCoreDataFramework(chunkOffsets, border);
            }
            AlteCoreDataFramework.Instance.SetRawData(DataPointer.Master, allData);
        }

        private static void Setup()
        {
            int master = AlteCoreFileInput.BinaryReader(masterArrayLengthFilePath, null, false);
            int save = AlteCoreFileInput.BinaryReader(saveArrayLengthFilePath, null, false);
            chunkOffsets = new int[master + save + 1];
            Span<int> lengthes = stackalloc int[master + save];
            border = master;
            AlteCoreFileInput.BinaryReader(masterArrayLengthFilePath, lengthes.Slice(0, master - 1), false);
            AlteCoreFileInput.BinaryReader(saveArrayLengthFilePath, lengthes.Slice(master, save), false);
            chunkOffsets[0] = 0;
            for(int i = 1; i < chunkOffsets.Length; i++)
            {
                chunkOffsets[i] = chunkOffsets[i - 1] + lengthes[i - 1];
            }
            allData = new int[chunkOffsets[chunkOffsets.Length - 1]];

            LoadMasterData();
        }

        private static void LoadMasterData()
        {
            for(int i = 0; i < border;  i++)
            {
                AlteCoreFileInput.BinaryReader(GetMasterDataFilePath(i), allData.AsSpan(chunkOffsets[i], chunkOffsets[i + 1] - chunkOffsets[i]), false);
            }
        }

        private static void LoadSaveData(int slotIndex)
        {
            if(SaveSlotNum <= slotIndex) throw new Exception("そのスロットはありません。");
            for(int i = border; i < chunkOffsets.Length - 1; i++)
            {
                string fileName = GetSaveDataFilePath(slotIndex, i);
                int datalength = AlteCoreFileInput.BinaryReader(fileName, null, false);
                if (datalength == -1)
                {
                    datalength = AlteCoreFileInput.BinaryReader(fileName, null, true);
                    if(datalength == -1)
                    {
                        string original = GetOriginalSaveDataFilePath(i);
                        if (!Directory.Exists(saveFolderPath + slotIndex))
                        {
                            Directory.CreateDirectory(saveFolderPath + slotIndex);
                        }
                        File.Copy(original, fileName, overwrite: true);
                        datalength = AlteCoreFileInput.BinaryReader(fileName, null, false);
                        LoadData(fileName, datalength, i, false);
                    }
                    else
                    {
                        LoadData(fileName, datalength, i, true);
                    }
                }
                else
                {
                    LoadData(fileName, datalength, i, false);
                }
            }
        }

        private static void LoadData(string fileName, int datalength, int index, bool bak)
        {
            Span<int> data = stackalloc int[datalength];
            AlteCoreFileInput.BinaryReader(fileName, data, bak);
            if (datalength >= chunkOffsets[index + 1] - chunkOffsets[index])
            {
                data.Slice(0, chunkOffsets[index + 1] - chunkOffsets[index]).CopyTo(allData.AsSpan(chunkOffsets[index], chunkOffsets[index + 1] - chunkOffsets[index]));
            }
            else
            {
                data.CopyTo(allData.AsSpan(chunkOffsets[index], chunkOffsets[index + 1] - chunkOffsets[index]));
            }
        }

        public static void Save(int chunk, Span<int> data, int slotIndex)
        {
            if(saveSlot == slotIndex)
            {
                Span<int> oldData = allData.AsSpan(chunkOffsets[chunk], chunkOffsets[chunk + 1] - chunkOffsets[chunk]);
                if (oldData.SequenceEqual(data)) return;
                data.CopyTo(oldData);
            }
            if (!Directory.Exists(saveFolderPath + slotIndex))
            {
                Directory.CreateDirectory(saveFolderPath + slotIndex);
            }
            AlteCoreFileOutput.SaveFile(GetSaveDataFilePath(slotIndex, chunk), data);
        }

        public static void Initialize(int chunk, Span<int> result)
        {
            string original = GetOriginalSaveDataFilePath(chunk);
            string fileName = GetSaveDataFilePath(saveSlot, chunk);
            File.Copy(original, fileName, overwrite: true);
            int datalength = AlteCoreFileInput.BinaryReader(fileName, null, false);
            Span<int> data = stackalloc int[datalength];
            AlteCoreFileInput.BinaryReader(fileName, data, false);
            if (datalength >= result.Length)
            {
                data.Slice(0, result.Length).CopyTo(result);
                data.Slice(0, result.Length).CopyTo(allData.AsSpan(chunkOffsets[chunk], result.Length));
            }
            else
            {
                data.CopyTo(result);
                data.CopyTo(allData.AsSpan(chunkOffsets[chunk], result.Length));
            }
        }

        public static string GetMasterDataFilePath(int chunk)
        {
            return Path.Combine(masterFolderPath, "md" + chunk + ".dat");
        }

        private static string GetSaveDataFilePath(int slotIndex, int chunk)
        {
            return Path.Combine(saveFolderPath + slotIndex, "sd" + (chunk - border) + ".dat");
        }

        public static string GetOriginalSaveDataFilePath(int chunk)
        {
            return Path.Combine(originalSaveFolderPath, "sd" + (chunk - border) + ".dat");
        }
    }

    public static class AlteCoreFileInput
    {
        public static int BinaryReader(string path, Span<int> result, bool bak)
        {
            byte[] rawdata;
            try
            {
                if(bak && File.Exists(path + ".bak"))
                {
                    rawdata = File.ReadAllBytes(path + ".bak");
                }
                else
                {
                    rawdata = File.ReadAllBytes(path);
                }
            }
            catch
            {
                return -1;
            }
            Span<int> data = MemoryMarshal.Cast<byte, int>(rawdata);
            if (result != null)
            {
                data.CopyTo(result);
            }
            return data.Length;
        }
    }

    public static class AlteCoreFileOutput
    { 
        public static void SaveFile(string path, Span<int> data)
        {
            if(File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }
            using (var fs = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write))
            {
                fs.Write(MemoryMarshal.Cast<int, byte>(data));
            }
            if(File.Exists(path))
            {
                File.Delete(path);
            }
            File.Move(path + ".tmp", path);
        }
    }
}