using AudioMirror;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;

namespace AudioMirror.Audio;

/// <summary>
/// Nimmt den Ton eines einzelnen Prozessbaums auf statt der gesamten Gerätemischung.
///
/// Grundlage ist die "process loopback"-Aktivierung von Windows: über
/// <c>ActivateAudioInterfaceAsync</c> wird ein IAudioClient auf ein virtuelles Gerät geöffnet,
/// dem als Aktivierungsparameter eine Ziel-Prozess-ID mitgegeben wird. Erfasst wird der Prozess
/// samt Kindprozessen - wichtig etwa bei Browsern, die ihren Ton in einem Hilfsprozess ausgeben.
///
/// Verfügbar ab Windows 10 Build 20348 bzw. Windows 11. Auf älteren Systemen schlägt die
/// Aktivierung fehl; das wird als lesbarer Fehler an der betroffenen Zeile gemeldet.
/// </summary>
internal sealed class ProcessLoopbackCapture : IWaveIn
{
    /// <summary>Für die Prozessaufnahme wird das Format selbst gewählt - Windows rechnet um.</summary>
    public static readonly WaveFormat CaptureFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    private const string VirtualDevicePath = @"VAD\Process_Loopback";
    private const int ActivationTypeProcessLoopback = 1;
    private const int LoopbackModeIncludeTargetProcessTree = 0;
    private const int VariantTypeBlob = 65;

    private readonly int processId;
    private readonly int bufferMilliseconds;

    private Thread? captureThread;
    private ManualResetEventSlim? startSignal;
    private Exception? startError;
    private volatile bool stopping;
    private byte[] buffer = [];

    public ProcessLoopbackCapture(int processId, int bufferMilliseconds)
    {
        this.processId = processId;
        this.bufferMilliseconds = Math.Clamp(bufferMilliseconds, 10, 200);
    }

    public event EventHandler<WaveInEventArgs>? DataAvailable;

    public event EventHandler<StoppedEventArgs>? RecordingStopped;

    public WaveFormat WaveFormat
    {
        get => CaptureFormat;
        set => throw new NotSupportedException("Das Aufnahmeformat der Prozessaufnahme ist fest vorgegeben.");
    }

    public void StartRecording()
    {
        if (captureThread != null)
        {
            return;
        }

        stopping = false;
        startError = null;
        startSignal = new ManualResetEventSlim(false);

        captureThread = new Thread(Run)
        {
            IsBackground = true,
            Name = "AudioMirror ProcessLoopback",
            Priority = ThreadPriority.AboveNormal,
        };
        // Die Aktivierung meldet ihr Ergebnis auf einem MTA-Thread zurück, die Oberfläche läuft
        // dagegen im STA. Deshalb bekommt die Aufnahme einen eigenen MTA-Thread.
        captureThread.SetApartmentState(ApartmentState.MTA);
        captureThread.Start();

        if (!startSignal.Wait(5000))
        {
            stopping = true;
            captureThread = null;
            throw new InvalidOperationException("Die Aufnahme der Anwendung hat nicht rechtzeitig geantwortet.");
        }

        if (startError != null)
        {
            captureThread = null;
            throw startError;
        }
    }

    public void StopRecording()
    {
        stopping = true;
        Thread? thread = captureThread;
        captureThread = null;
        thread?.Join(1500);
    }

    private void Run()
    {
        AudioClient? client = null;
        Exception? failure = null;

        try
        {
            client = Activate();

            using var frameEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            bool useEvent = true;
            try
            {
                client.SetEventHandle(frameEvent.SafeWaitHandle.DangerousGetHandle());
            }
            catch
            {
                // Ohne Ereignisbenachrichtigung wird stattdessen kurz getaktet abgefragt.
                useEvent = false;
            }

            AudioCaptureClient capture = client.AudioCaptureClient;
            client.Start();
            startSignal!.Set();

            int bytesPerFrame = CaptureFormat.Channels * sizeof(float);

            while (!stopping)
            {
                if (useEvent)
                {
                    frameEvent.WaitOne(100);
                }
                else
                {
                    Thread.Sleep(5);
                }

                while (!stopping && capture.GetNextPacketSize() > 0)
                {
                    IntPtr data = capture.GetBuffer(out int frames, out AudioClientBufferFlags flags);
                    int bytes = frames * bytesPerFrame;

                    if (bytes > 0)
                    {
                        if (buffer.Length < bytes)
                        {
                            buffer = new byte[bytes];
                        }

                        if ((flags & AudioClientBufferFlags.Silent) != 0)
                        {
                            Array.Clear(buffer, 0, bytes);
                        }
                        else
                        {
                            Marshal.Copy(data, buffer, 0, bytes);
                        }
                    }

                    capture.ReleaseBuffer(frames);

                    if (bytes > 0)
                    {
                        DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, bytes));
                    }
                }
            }

            client.Stop();
        }
        catch (Exception ex)
        {
            failure = ex;
            startError ??= ex;
        }
        finally
        {
            startSignal?.Set();
            try
            {
                client?.Dispose();
            }
            catch
            {
                // Beim Aufräumen unkritisch.
            }
            RecordingStopped?.Invoke(this, new StoppedEventArgs(failure));
        }
    }

    private AudioClient Activate()
    {
        // AUDIOCLIENT_ACTIVATION_PARAMS: { Typ, { Ziel-PID, Modus } } - drei 32-Bit-Werte.
        const int parameterSize = sizeof(int) * 3;
        IntPtr parameters = Marshal.AllocHGlobal(parameterSize);
        IntPtr variant = Marshal.AllocHGlobal(32);

        try
        {
            Marshal.WriteInt32(parameters, 0, ActivationTypeProcessLoopback);
            Marshal.WriteInt32(parameters, 4, processId);
            Marshal.WriteInt32(parameters, 8, LoopbackModeIncludeTargetProcessTree);

            // PROPVARIANT als BLOB verpackt: Typ bei 0, Länge bei 8, Zeiger bei 16 (x64-Layout).
            for (int offset = 0; offset < 32; offset += 8)
            {
                Marshal.WriteInt64(variant, offset, 0);
            }
            Marshal.WriteInt16(variant, 0, VariantTypeBlob);
            Marshal.WriteInt32(variant, 8, parameterSize);
            Marshal.WriteIntPtr(variant, 16, parameters);

            var handler = new ActivationHandler();
            ActivateAudioInterfaceAsync(VirtualDevicePath, typeof(IAudioClient).GUID, variant, handler, out _);

            if (!handler.Completed.Wait(4000))
            {
                throw new InvalidOperationException("Windows hat die Anwendungsaufnahme nicht bestätigt.");
            }
            if (handler.Result != 0)
            {
                throw Marshal.GetExceptionForHR(handler.Result)
                    ?? new InvalidOperationException(Strings.CaptureFailed("?"));
            }

            var client = new AudioClient((IAudioClient)handler.Interface!);
            client.Initialize(
                AudioClientShareMode.Shared,
                AudioClientStreamFlags.Loopback | AudioClientStreamFlags.EventCallback,
                bufferMilliseconds * 10000L,
                0,
                CaptureFormat,
                Guid.Empty);
            return client;
        }
        finally
        {
            Marshal.FreeHGlobal(variant);
            Marshal.FreeHGlobal(parameters);
        }
    }

    public void Dispose() => StopRecording();

    [DllImport("Mmdevapi.dll", ExactSpelling = true, PreserveSig = false)]
    private static extern void ActivateAudioInterfaceAsync(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        IntPtr activationParams,
        IActivateAudioInterfaceCompletionHandler completionHandler,
        out IActivateAudioInterfaceAsyncOperation operation);

    [ComImport, Guid("41D949AB-9862-444A-80F6-C261334DA5EB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivateAudioInterfaceCompletionHandler
    {
        void ActivateCompleted(IActivateAudioInterfaceAsyncOperation operation);
    }

    [ComImport, Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivateAudioInterfaceAsyncOperation
    {
        void GetActivateResult(
            [MarshalAs(UnmanagedType.Error)] out int activateResult,
            [MarshalAs(UnmanagedType.IUnknown)] out object activatedInterface);
    }

    private sealed class ActivationHandler : IActivateAudioInterfaceCompletionHandler
    {
        public readonly ManualResetEventSlim Completed = new(false);
        public int Result = -1;
        public object? Interface;

        public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation operation)
        {
            try
            {
                operation.GetActivateResult(out Result, out Interface);
            }
            catch (Exception ex)
            {
                Result = ex.HResult;
            }
            finally
            {
                Completed.Set();
            }
        }
    }
}
