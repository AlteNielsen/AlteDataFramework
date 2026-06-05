using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Alte.Data.Core
{
    public class AlteCoreBinaryConverter
    {
        public static AlteCoreBinaryConverter Instance;
        private List<int>[] datas;

        public AlteCoreBinaryConverter()
        {
            if (Instance != null) return;
            Instance = this;
            datas = new List<int>[(int)DataChunk.Count];
            DataChunkRegistry();
            DataConvert();
        }

        public void SetData<T>(DataChunk chunk, List<T> data) where T : struct 
        {
            Span<T> dataSpan = new T[data.Count];
            for(int i = 0; i < data.Count; i++)
            {
                dataSpan[i] = data[i];
            }
            Span<int> rawdata = MemoryMarshal.Cast<T, int>(dataSpan);
            datas[(int)chunk].AddRange(rawdata.ToArray());
        }

        private void Register<T, U>() where T : AlteNormalDataChunk<U>, new() where U : struct
        {
            var (chunk, data) = new T().Load();
            SetData(chunk, data);
        }

        private void DataConvert()
        {
            if(!Directory.Exists(AlteCoreDataIO.masterFolderPath))
            {
                Directory.CreateDirectory(AlteCoreDataIO.masterFolderPath);
            }
            if(!Directory.Exists(AlteCoreDataIO.originalSaveFolderPath))
            {
                Directory.CreateDirectory(AlteCoreDataIO .originalSaveFolderPath);
            }
            int border = (int)DataBorder();
            WriteMasterLength(border);
            WriteMasterFiles(border);
            WriteSaveLength(border);
            WriteSaveFiles(border);
        }

        private void WriteMasterLength(int border)
        {
            Span<int> leg = stackalloc int[border];
            for(int i = 0;i < border; i++)
            {
                leg[i] = datas[i].Count;
            }
            WriteBinary(AlteCoreDataIO.masterArrayLengthFilePath, leg);
        }

        private void WriteMasterFiles(int border)
        {
            for(int i = 0; i < border; i++)
            {
                WriteBinary(AlteCoreDataIO.GetMasterDataFilePath(i), datas[i].ToArray());
            }
        }

        private void WriteSaveLength(int border)
        {
            Span<int> leg = stackalloc int[datas.Length - border];
            for (int i = border; i < datas.Length; i++)
            {
                leg[i] = datas[i].Count;
            }
            WriteBinary(AlteCoreDataIO.saveArrayLengthFilePath, leg);
        }

        private void WriteSaveFiles(int border)
        {
            for (int i = border; i < datas.Length; i++)
            {
                WriteBinary(AlteCoreDataIO.GetOriginalSaveDataFilePath(i), datas[i].ToArray());
            }
        }

        private void WriteBinary(string path, Span<int> data)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                fs.Write(MemoryMarshal.Cast<int, byte>(data));
            }
        }

        private void DataChunkRegistry()//ユーザーさん自身が直接書き込んでください。例 → Register<TestDataChunk, int>();
        {

        }

        private DataChunk DataBorder()//ユーザーさん自身が直接書き換えてください。ここに書いたチャンクまでがマスターデータとして扱われます。
        {
            return DataChunk.Count;
        }
    }

    public abstract class AlteNormalDataChunk<U> where U : struct
    {
        public abstract (DataChunk chunk, List<U> data) Load();

        protected static T LoadJSON<T>(string path)
        {
            using StreamReader reader = new StreamReader(path, Encoding.UTF8);
            string json = reader.ReadToEnd();
            return JsonUtility.FromJson<T>(json);
        }

        protected static void LoadCSV(string path, List<string[]> csvData)
        {
            string[] lines = File.ReadAllLines(path);
            for(int i = 0; i < lines.Length; i++)
            {
                if(string.IsNullOrEmpty(lines[i])) continue;
                string[] words = lines[i].Split(',');
                csvData.Add(words);
            }
        }
    }
}