using TMPro;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

public class DataWriterUI : MonoBehaviour
{
    [SerializeField] DataWriter dataWriter;
    [SerializeField] TMP_InputField fetchInterval;
    [SerializeField] TMP_InputField filename;
    [SerializeField] private TMP_Dropdown compressionDropdown;
    [SerializeField] private TMP_Dropdown writeStreamDropdown;
    [SerializeField] private TMP_Dropdown encryptionDropdown;
    [SerializeField] Button startStopButton;
    [SerializeField] CanvasGroup aesKeyButtons, aesIVButtons;
    [SerializeField] TextMeshProUGUI startStopButtonLabel;
    [SerializeField] TextMeshProUGUI startTimeLabel;
    [SerializeField] TextMeshProUGUI endTimeLabel;
    [SerializeField] TextMeshProUGUI elapsedTimeLabel;
    [SerializeField] TextMeshProUGUI entriesLabel;
    [SerializeField] TextMeshProUGUI filesizeLabel;
    [SerializeField] TextMeshProUGUI aesKeyLabel;
    [SerializeField] TextMeshProUGUI aesIVLabel;
    
    int entriesCounter = 0;
    DateTime startTime, endTime;
    TimeSpan elapsedTime;

   
    void OnEnable()
    {
        EventController.OnDataEntryWritten += IncreaseEntryCounter;
        EventController.OnDataEntryWritten += UpdateFileSize;

        EventController.OnAESKeyGenerated += UpdateAESKeyLabel;
        EventController.OnAESIVGenerated += UpdateAESIVLabel;
    }

    void OnDisable()
    {
        EventController.OnDataEntryWritten -= IncreaseEntryCounter;
        EventController.OnDataEntryWritten -= UpdateFileSize;

        EventController.OnAESKeyGenerated -= UpdateAESKeyLabel;
        EventController.OnAESIVGenerated -= UpdateAESIVLabel;
    }

    void Update()
    {
        if (dataWriter.isRecording)
        {
            elapsedTime = DateTime.Now - startTime;
            elapsedTimeLabel.text = elapsedTime.ToString("hh\\:mm\\:ss");
        }
    }

    public void OnEndEditFetchInterval() => fetchInterval.text = float.Parse(fetchInterval.text.Replace(",", ".").Replace(" Hz", "")) + " Hz";
    void UpdateFileSize(string _) => filesizeLabel.text = (dataWriter.currentFileSize / 1000f).ToString("0") + " KB";
    void IncreaseEntryCounter(string _)
    {
        entriesCounter++;
        entriesLabel.text = entriesCounter.ToString();
    }
    void UpdateAESKeyLabel(string key)
    {
        aesKeyLabel.text = "<u>AES Key</u>\n" + key.Substring(0, 5) + "...";
        if (key != "")
        {
            aesKeyButtons.alpha = aesIVButtons.alpha = 1f;
            aesKeyButtons.interactable = aesIVButtons.interactable = true;
        }
    }

    void UpdateAESIVLabel(string iv)
    {
        aesIVLabel.text = "<u>AES IV</u>\n" + iv.Substring(0, 5) + "...";
        if (iv != "")
        {
            aesKeyButtons.alpha = aesIVButtons.alpha = 1f;
            aesKeyButtons.interactable = aesIVButtons.interactable = true;
        }
    }

    public void OnRefreshAESKeyButton() => AESController.GenerateRandomKey((EncryptionType)encryptionDropdown.value);
    public void OnRefreshAESIVButton() => AESController.GenerateRandomIV((EncryptionType)encryptionDropdown.value);

    public void OnCopyAESKeyButton() => GUIUtility.systemCopyBuffer = AESController.Key;
    public void OnCopyAESIVButton() => GUIUtility.systemCopyBuffer = AESController.IV;

    public void OnEncryptionDropdownChange(Int32 dropdownValue)
    {
        EncryptionType newEncryption = (EncryptionType)dropdownValue;
        if (newEncryption == EncryptionType.NoEncryption)
        {
            // clear labels? possibly more in the future
            aesKeyLabel.text = aesIVLabel.text = "";
            aesKeyButtons.alpha = aesIVButtons.alpha = 0f;
            aesKeyButtons.interactable = aesIVButtons.interactable = false;
        }
        else
        {
            AESController.GenerateRandomKey(newEncryption);
            AESController.GenerateRandomIV(newEncryption);
        }
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

    public void OnStartStopButtonClick()
    {
        if (dataWriter.isRecording)
        {
            dataWriter.StopRecording();
            startStopButton.image.color = new Color(111f/255f, 243f/255f, 135f/255f);
            startStopButtonLabel.text = "Start Writer";

            entriesCounter = 0;

            endTime = DateTime.Now;
            endTimeLabel.text = endTime.ToLongTimeString();
        }
        else
        {
            if (InputFieldsValid())
            {
                filename.text = GenerateOutputFilePath(Application.dataPath + "/" + DataWriter.outputDirectory);
                
                dataWriter.StartRecording(float.Parse(fetchInterval.text.Replace(",", ".").Replace(" Hz", ""), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture),
                    (CompressionType)compressionDropdown.value,
                    (WriteStreamType)writeStreamDropdown.value,
                    (EncryptionType)encryptionDropdown.value);

                startStopButton.image.color = Color.red;
                startStopButtonLabel.text = "Stop Writer";

                startTime = DateTime.Now;
                startTimeLabel.text = startTime.ToLongTimeString();
            }
        }
    }

    bool InputFieldsValid()
    {
        if (float.TryParse(fetchInterval.text.Replace(" Hz", ""), out float result))
        {
            if (result <= 0f)
            {
                Debug.LogError("Fetch Interval must be larger than 0f.");
                return false;
            }
        }
        else
        {
            Debug.LogError("Can't parse Fetch Interval. Please make sure it's a valid float.");
            return false;
        }

        if (!filename.text.EndsWith(".json"))
        {
            filename.text += ".json";
            return true;
        }

        return true;
    }
}
