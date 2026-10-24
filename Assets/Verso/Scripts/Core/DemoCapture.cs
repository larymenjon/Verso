using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Collections;
using UnityEditor.Media;
#endif

namespace Verso
{
    /// <summary>
    /// Grava a demo em Recordings/verso_gameplay.mp4 (1920x1080, 30 fps, com o som do jogo)
    /// usando o MediaEncoder do próprio editor, sem o pacote Unity Recorder.
    /// Só funciona no Play do editor; o DemoDirector liga isso sozinho.
    /// </summary>
    public class DemoCapture : MonoBehaviour
    {
        private const int Width = 1920, Height = 1080, Fps = 30;

        public static bool Recording { get; private set; }
        private static readonly object audioLock = new object();
        private static readonly List<float> audioBuffer = new List<float>(48000 * 4);

        public string FilePath { get; private set; }

#if UNITY_EDITOR
        private MediaEncoder encoder;
        private RenderTexture fullRt, outRt;
        private Texture2D frameTex;
        private int sampleRate, samplesPerFrame;
        private float t0;
        private long framesWritten;
        private Material flipMat;

        public void Begin(float startTime)
        {
            t0 = startTime;
            sampleRate = AudioSettings.outputSampleRate;
            samplesPerFrame = sampleRate / Fps;

            FilePath = Path.Combine(DemoDirector.OutFolder, "verso_gameplay.mp4");
            if (File.Exists(FilePath)) File.Delete(FilePath);

            var v = new VideoTrackAttributes
            {
                frameRate = new MediaRational(Fps),
                width = Width,
                height = Height,
                includeAlpha = false,
                bitRateMode = UnityEditor.VideoBitrateMode.Medium
            };
            var a = new AudioTrackAttributes
            {
                sampleRate = new MediaRational(sampleRate),
                channelCount = 2,
                language = "en"
            };
            encoder = new MediaEncoder(FilePath, v, a);

            outRt = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32);
            frameTex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            lock (audioLock) audioBuffer.Clear();
            Recording = true;
            StartCoroutine(CaptureLoop());
            Debug.Log($"[Verso Demo] Gravando {FilePath} ({sampleRate} Hz)");
        }

        private void LateUpdate()
        {
            if (!Recording) return;
            // O AudioListener muda a cada cena; garante que o ouvinte atual tenha o "grampo".
            var listener = FindAnyObjectByType<AudioListener>();
            if (listener != null && listener.GetComponent<DemoAudioTap>() == null)
                listener.gameObject.AddComponent<DemoAudioTap>();
        }

        private IEnumerator CaptureLoop()
        {
            var eof = new WaitForEndOfFrame();
            while (Recording)
            {
                yield return eof;
                if (!Recording) break;

                long due = (long)((Time.unscaledTime - t0) * Fps) + 1;
                if (due <= framesWritten) continue;

                GrabFrame();
                // se o editor engasgou, repete o quadro para manter o tempo do vídeo igual ao real
                while (framesWritten < due)
                {
                    encoder.AddFrame(frameTex);
                    WriteAudioChunk();
                    framesWritten++;
                }
            }
        }

        private void GrabFrame()
        {
            if (fullRt == null || fullRt.width != Screen.width || fullRt.height != Screen.height)
            {
                if (fullRt != null) fullRt.Release();
                fullRt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            }
            ScreenCapture.CaptureScreenshotIntoRenderTexture(fullRt);
            // Em D3D/Metal a captura vem de cabeça para baixo; desvira ao reduzir para 1080p.
            if (SystemInfo.graphicsUVStartsAtTop)
                Graphics.Blit(fullRt, outRt, new Vector2(1f, -1f), new Vector2(0f, 1f));
            else
                Graphics.Blit(fullRt, outRt);

            var prev = RenderTexture.active;
            RenderTexture.active = outRt;
            frameTex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
            RenderTexture.active = prev;
        }

        private void WriteAudioChunk()
        {
            int need = samplesPerFrame * 2;
            var chunk = new NativeArray<float>(need, Allocator.Temp, NativeArrayOptions.ClearMemory);
            try
            {
                lock (audioLock)
                {
                    // não deixa o áudio atrasar mais que 0,2 s em relação ao vídeo
                    int max = sampleRate * 2 / 5;
                    if (audioBuffer.Count > need + max) audioBuffer.RemoveRange(0, audioBuffer.Count - need - max);
                    int n = Mathf.Min(need, audioBuffer.Count);
                    for (int i = 0; i < n; i++) chunk[i] = audioBuffer[i];
                    audioBuffer.RemoveRange(0, n);
                }
                encoder.AddSamples(chunk);
            }
            finally { chunk.Dispose(); }
        }

        public void End()
        {
            if (!Recording) return;
            Recording = false;
            encoder?.Dispose();
            encoder = null;
            if (fullRt != null) fullRt.Release();
            if (outRt != null) outRt.Release();
            Debug.Log($"[Verso Demo] Vídeo salvo: {FilePath} ({framesWritten} quadros)");
        }

        private void OnDisable() { End(); }
#else
        public void Begin(float startTime) { }
        public void End() { }
#endif

        internal static void PushAudio(float[] data, int channels)
        {
            if (!Recording) return;
            lock (audioLock)
            {
                for (int i = 0; i < data.Length; i += channels)
                {
                    float l = data[i];
                    float r = channels > 1 ? data[i + 1] : l;
                    audioBuffer.Add(l);
                    audioBuffer.Add(r);
                }
            }
        }
    }

    /// <summary>Copia o som que chega no AudioListener para a gravação (não altera o áudio).</summary>
    public class DemoAudioTap : MonoBehaviour
    {
        private void OnAudioFilterRead(float[] data, int channels) => DemoCapture.PushAudio(data, channels);
    }
}
