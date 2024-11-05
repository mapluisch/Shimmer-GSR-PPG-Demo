using System;
using System.IO;
using UnityEngine;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.IO.Compression;
using System.Collections.Generic;

public class DataWriter : MonoBehaviour
{
    [HideInInspector] public float dataFetchInterval = 0.5f;
    public static string outputDirectory = "Recordings";
    private string outputFilePath = "";
    private List<Module> modules;
    private CompressionType compressionType = CompressionType.GZIPB64;
    private WriteStreamType writeStreamType = WriteStreamType.WriteAndStream;
    private EncryptionType encryptionType = EncryptionType.AES256;
    [HideInInspector] public bool isRecording = false;
    private Coroutine fetchAndWriteRoutine;
    [HideInInspector] public long currentFileSize;
    public static int CurrentEntryIndex = 0;

    private void OnEnable() => modules = GameObject.FindObjectsOfType<Module>().ToList();
    
    public void StartRecording(float dataFetchInterval, CompressionType compressionType, WriteStreamType writeStreamType, EncryptionType encryptionType)
    {
        this.dataFetchInterval = 1f / dataFetchInterval;
        this.outputFilePath = Application.dataPath + "/" + DataWriter.outputDirectory + "/" + GenerateOutputFilePath(Application.dataPath + "/" + outputDirectory);
        this.compressionType = compressionType;
        this.writeStreamType = writeStreamType;
        this.encryptionType = encryptionType;

        isRecording = true;
        fetchAndWriteRoutine = StartCoroutine(FetchAndWriteDataJSON());

        EventController.TriggerOnStartDataWriter();
    }

    private string GenerateOutputFilePath(string outputDirectory)
    {
        int newId = GetLatestRecordingId(outputDirectory) + 1;
        string date = DateTime.Now.ToString("yyyyMMdd");
        string timestamp = DateTime.Now.ToString("HHmmss");
        return $"recording_{newId}_{date}_{timestamp}.json";
    }

    private int GetLatestRecordingId(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            return 0;
        }

        var files = Directory.GetFiles(outputDirectory, "recording_*_*.json");
        if (files.Length == 0) return 0;

        var latestFile = files
            .Select(Path.GetFileName)
            .Select(name => new
            {
                FileName = name,
                Id = int.Parse(name.Split('_')[1])
            })
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        return latestFile?.Id ?? 0;
    }

    public void StopRecording() =>isRecording = false;

    IEnumerator FetchAndWriteDataJSON()
    {
        JsonSerializerSettings ignoreLoop = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
        CurrentEntryIndex = 0;

        using (StreamWriter streamWriter = new StreamWriter(outputFilePath))
        {
            // double totalElapsedTime = 0;
            if (writeStreamType is WriteStreamType.WriteAndStream) streamWriter.Write("{");
            while (isRecording)
            {
                currentFileSize = streamWriter.BaseStream.Length;
                // Stopwatch sw = new Stopwatch();
                // sw.Start();

                DataRow dataRow = new DataRow(modules);
                string addedComma = (CurrentEntryIndex != 0) ? ", " : "";
                
                string jsonData = $"{{\"time\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss:ffff}\", \"data\": {JsonConvert.SerializeObject(dataRow.ModuleData(), Formatting.Indented, ignoreLoop)}}}";
                if (compressionType is CompressionType.GZIPB64)
                {
                    byte[] dataToCompress = Encoding.UTF8.GetBytes(jsonData);
                    byte[] compressedData = CompressWithGZIP(dataToCompress);
                    if (encryptionType is not EncryptionType.NoEncryption)
                    {
                        // either encrypt (with included B64 encoding)
                        jsonData = AESController.Encrypt(compressedData);
                    }
                    else
                    {
                        // or dont encrypt and only encode as B64
                        jsonData = System.Convert.ToBase64String(compressedData);
                    }
                } else if (compressionType is CompressionType.NoCompression)
                {
                    if (encryptionType != EncryptionType.NoEncryption)
                    {
                        jsonData = AESController.Encrypt(jsonData);
                    }
                }

                if (writeStreamType is WriteStreamType.WriteAndStream)
                {
                    streamWriter.Write(addedComma + jsonData);
                    streamWriter.Flush();
                }

                // sw.Stop();
                // totalElapsedTime += sw.Elapsed.TotalMilliseconds;
                // Debug.Log($"avg elapsed time: {totalElapsedTime/currentEntryIndex} ms");

                EventController.TriggerOnDataEntryWritten(jsonData);
                CurrentEntryIndex++;
                yield return new WaitForSeconds(dataFetchInterval);
            }

            if (writeStreamType is WriteStreamType.WriteAndStream)
            {
                streamWriter.Write("}");
                streamWriter.Close();
            }
        }

        EventController.TriggerOnEndDataWriter();
        StopCoroutine(fetchAndWriteRoutine);
    }

    byte[] CompressWithGZIP(byte[] bytes)
    {
        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, System.IO.Compression.CompressionLevel.Fastest))
        {
            gzipStream.Write(bytes, 0, bytes.Length);
        }
        return memoryStream.ToArray();
    }
}

[System.Serializable]
public class DataRow
{
    public string dateTime;
    public List<Module> modules;

    public DataRow(List<Module> modules)
    {
        this.dateTime = DateTime.Now.ToLongTimeString();
        this.modules = modules;
    }

    public Dictionary<string, object> ModuleData()
    {
        Dictionary<string, object> res = new Dictionary<string, object>();
        foreach (Module module in modules)
        {
            if (!module.isModuleUsable) continue;
            res.Add(module.GetType().Name, module.GetDataFrame());
        }
        return res;
    }
}

// enum ints are important, as they are parsed directly from the dropdown as enum of CompressionType
// so, if we add other compression types in the future, make sure the dropdown index aligns with the enum int.
public enum CompressionType { GZIPB64 = 0, NoCompression = 1 };
public enum WriteStreamType { WriteAndStream = 0, StreamOnly = 1 };
public enum EncryptionType { NoEncryption = 0, AES128 = 1, AES256 = 2 };