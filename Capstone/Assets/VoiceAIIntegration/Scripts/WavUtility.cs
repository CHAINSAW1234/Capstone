using UnityEngine;
using System.IO;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] wav = ConvertToWav(samples, clip.channels, clip.frequency);
        return wav;
    }

    private static byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        // WAV 헤더 작성
        writer.Write("RIFF".ToCharArray());
        writer.Write(0); // Placeholder
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);

        writer.Write("data".ToCharArray());
        writer.Write(samples.Length * 2);

        // 샘플 데이터 쓰기
        foreach (var sample in samples)
        {
            short intSample = (short)(sample * short.MaxValue);
            writer.Write(intSample);
        }

        // 파일 크기 수정
        stream.Seek(4, SeekOrigin.Begin);
        writer.Write((int)(stream.Length - 8));

        return stream.ToArray();
    }
}
