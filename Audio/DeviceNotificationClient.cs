using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AudioMirror.Audio;

/// <summary>
/// Leitet die WASAPI-Geraeteereignisse (Spec 4.4) als schlichte .NET-Events weiter.
/// Die Callbacks kommen von einem beliebigen Systemthread - Abonnenten muessen selbst marshallen.
/// </summary>
internal sealed class DeviceNotificationClient : IMMNotificationClient
{
    public event Action? DeviceListChanged;

    public event Action? DefaultRenderDeviceChanged;

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => DeviceListChanged?.Invoke();

    public void OnDeviceAdded(string pwstrDeviceId) => DeviceListChanged?.Invoke();

    public void OnDeviceRemoved(string deviceId) => DeviceListChanged?.Invoke();

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && role == Role.Multimedia)
        {
            DefaultRenderDeviceChanged?.Invoke();
        }
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        // Lautstaerke-/Namensaenderungen sind fuer die Spiegelung irrelevant.
    }
}
