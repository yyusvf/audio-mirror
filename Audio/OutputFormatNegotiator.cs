using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioMirror.Audio;

/// <summary>
/// Sucht ein Ausgabeformat, das ein Geraet im Shared Mode tatsaechlich akzeptiert.
///
/// Normalerweise ist das schlicht das MixFormat des Geraets. Es gibt jedoch Treiber, die ihr
/// eigenes MixFormat ablehnen oder ungewoehnliche Kanalzahlen melden. Statt in so einem Fall
/// mit einer COM-Fehlermeldung auszusteigen, wird eine Reihe zunehmend konservativer Formate
/// durchprobiert und zuletzt Windows' eigener Gegenvorschlag uebernommen.
/// </summary>
internal static class OutputFormatNegotiator
{
    public static WaveFormat Negotiate(AudioClient client)
    {
        WaveFormat mix = client.MixFormat;

        foreach (WaveFormat candidate in BuildCandidates(mix))
        {
            if (IsSupported(client, candidate))
            {
                return candidate;
            }
        }

        // Windows nennt bei nicht unterstuetztem Format das naechstgelegene passende.
        try
        {
            if (!client.IsFormatSupported(AudioClientShareMode.Shared, mix, out WaveFormatExtensible? closest)
                && closest != null)
            {
                return closest;
            }
        }
        catch
        {
            // Manche Treiber werfen hier statt "false" zurueckzugeben.
        }

        return mix;
    }

    private static IEnumerable<WaveFormat> BuildCandidates(WaveFormat mix)
    {
        yield return mix;

        // Gleiche Abtastrate und Kanalzahl, aber explizit angeforderte Bittiefen.
        yield return new WaveFormatExtensible(mix.SampleRate, 32, mix.Channels);
        yield return new WaveFormatExtensible(mix.SampleRate, 24, mix.Channels);
        yield return new WaveFormatExtensible(mix.SampleRate, 16, mix.Channels);

        // Auf Stereo zurueckfallen, falls die gemeldete Kanalzahl nicht bedient wird.
        if (mix.Channels != 2)
        {
            yield return new WaveFormatExtensible(mix.SampleRate, 32, 2);
            yield return new WaveFormatExtensible(mix.SampleRate, 16, 2);
        }

        // Zuletzt die ueblichen Standardraten.
        foreach (int rate in new[] { 48000, 44100 })
        {
            if (rate == mix.SampleRate)
            {
                continue;
            }
            yield return new WaveFormatExtensible(rate, 32, 2);
            yield return new WaveFormatExtensible(rate, 16, 2);
        }
    }

    private static bool IsSupported(AudioClient client, WaveFormat format)
    {
        try
        {
            return client.IsFormatSupported(AudioClientShareMode.Shared, format);
        }
        catch
        {
            return false;
        }
    }
}
