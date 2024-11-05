using System;
using System.Net;

public class EventController
{
    public static event Action OnStartDataWriter; // called when data writer starts
    public static event Action<string> OnDataEntryWritten; // called when a new entry is written to the output file (i.e., every [fetch interval] seconds)
    public static event Action OnEndDataWriter; // called when data writer is stopped / ended
    public static event Action<Module> OnModuleUsable; // called by every module that is usable
    public static event Action<Module> OnModuleUnusable; // called by every module that is unusable (i.e., disconnected / unreachable)
    public static event Action OnStartDataStreamSocket; // called when server is opened
    public static event Action OnEndDataStreamSocket; // called when server is closed
    public static event Action<IPEndPoint> OnDataStreamSocketConnectionEstablished; // called when a simple connection is made (un-auth'd at this stage)
    public static event Action<IPEndPoint> OnDataStreamSocketConnectionSuccess; // called when a new auth'd connection to the streaming server is detected
    public static event Action<IPEndPoint> OnDataStreamSocketConnectionFail; // called when the socket connection has failed / ended
    public static event Action<string> OnAESKeyGenerated; // called when a new aes key is generated (b64-encoded key)
    public static event Action<string> OnAESIVGenerated; // called when a new aes iv is generated (b64-encoded iv)
    public static event Action OnAESHandshake; // called when the AES req is accepted within the UI (button press)

    public static void TriggerOnStartDataWriter() => OnStartDataWriter?.Invoke();
    public static void TriggerOnDataEntryWritten(string lastDataEntry) => OnDataEntryWritten?.Invoke(lastDataEntry);
    public static void TriggerOnEndDataWriter() => OnEndDataWriter?.Invoke();
    public static void TriggerOnModuleUsable(Module module) => OnModuleUsable?.Invoke(module);
    public static void TriggerOnModuleUnusable(Module module) => OnModuleUnusable?.Invoke(module);
    public static void TriggerOnStartDataStreamSocket() => OnStartDataStreamSocket?.Invoke();
    public static void TriggerOnEndDataStreamSocket() => OnEndDataStreamSocket?.Invoke();
    public static void TriggerOnDataStreamSocketConnectionEstablished(IPEndPoint ip) => OnDataStreamSocketConnectionEstablished?.Invoke(ip);
    public static void TriggerOnDataStreamSocketConnectionSuccess(IPEndPoint ip) => OnDataStreamSocketConnectionSuccess?.Invoke(ip);
    public static void TriggerOnDataStreamSocketConnectionFail(IPEndPoint ip) => OnDataStreamSocketConnectionFail?.Invoke(ip);
    public static void TriggerOnAESKeyGenerated(string key) => OnAESKeyGenerated?.Invoke(key);
    public static void TriggerOnAESIVGenerated(string iv) => OnAESIVGenerated?.Invoke(iv);
    public static void TriggerOnAESHandshake() => OnAESHandshake?.Invoke();

}
