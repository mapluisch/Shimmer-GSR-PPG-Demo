using ShimmerAPI;
using UnityEngine;
using ShimmeringUnity;
using Newtonsoft.Json;
using System.Collections;
using ShimmerDevice = ShimmeringUnity.ShimmerDevice;

public class ShimmerModule : Module
{
    [SerializeField] ShimmerDevice shimmerDevice;
    private ShimmerData currentData = new ShimmerData();
    [SerializeField] private ShimmerHeartRateMonitor shimmerHeartRateMonitor;
    private int windowSize;
    private double samplingRate;
    [SerializeField] private string comPort = "COM4";

    private DataWriter dataWriter;

    private void OnValidate()
    {
        if (shimmerDevice != null) shimmerDevice.COMPort = this.comPort;
    }

    void OnEnable()
    {
        dataWriter = FindObjectOfType<DataWriter>();
        if (dataWriter != null) samplingRate = 1.0 / dataWriter.dataFetchInterval;
        
        StartCoroutine(InitShimmerConnection());
        shimmerDevice.OnDataRecieved.AddListener(HandleData);
    }

    void HandleData(ShimmerDevice sd, ObjectCluster oc)
    {
        SensorData gsrSD = oc.GetData(ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.GSR], ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]);
        SensorData gsrConductanceSD = oc.GetData(ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.GSR_CONDUCTANCE], ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]);
        SensorData ppgSD = oc.GetData(ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.INTERNAL_ADC_A13], ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]);

        double gsr = gsrSD.GetData();
        double gsrConductance = gsrConductanceSD.GetData();
        double ppg = ppgSD.GetData();
        double ppgToHR = shimmerHeartRateMonitor.CalculateHeartRate(sd, oc);
        
        this.currentData = new ShimmerData(
            gsr, 
            gsrConductance, 
            ppg, 
            ppgToHR
        );
    }


    IEnumerator InitShimmerConnection()
    {
        SetModuleUsable(false);
        Debug.Log("Connecting to Shimmer...");
        shimmerDevice.Connect();
        while (shimmerDevice.CurrentState != ShimmerDevice.State.Connected)
        {
            yield return new WaitForSeconds(0.1f);
            if (shimmerDevice.CurrentState == ShimmerDevice.State.Disconnected)
            {
                Debug.Log("Trying again to connect to Shimmer...");
                yield return new WaitForSeconds(3f);
                shimmerDevice.Connect();
            }
        }

        Debug.Log("Successfully connected to Shimmer, starting streaming session.");
        shimmerDevice.StartStreaming();
        SetModuleUsable(true);
    }

    public override string GetDataString() => JsonConvert.SerializeObject(GetDataFrame());
    public override string GetDetails() => "Shimmer GSR+"; 
    public override object GetDataFrame() => currentData;
}

[System.Serializable]
public class ShimmerData
{
    public double gsr = 0.0;
    public double gsrConductance = 0.0;
    public double ppg = 0.0;
    public double ppgToHR = 0.0;

    public ShimmerData(double gsr = 0, double gsrConductance = 0, double ppg = 0, double ppgToHR = 0)
    {
        this.gsr = gsr;
        this.gsrConductance = gsrConductance;
        this.ppg = ppg;
        this.ppgToHR = ppgToHR;
    }
}