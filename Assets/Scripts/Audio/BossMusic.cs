using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Boss fight theme: plays the intro clip once, then hands over to the loop
/// clip sample-accurately on the DSP clock. Routed through the Music mixer
/// group so the MusicVolume slider applies. Bossu starts it on Release()
/// and fades it out on the killing blow.
/// </summary>
public class BossMusic : MonoBehaviour
{
    public static BossMusic Instance { get; private set; }

    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip loopClip;
    [Tooltip("Music mixer group, so the MusicVolume settings slider controls the theme.")]
    [SerializeField] private AudioMixerGroup output;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.6f;
    [Tooltip("Fade-out length once the boss dies, so the theme doesn't cut off mid-note.")]
    [SerializeField] private float fadeOutTime = 2.5f;

    private AudioSource introSource;
    private AudioSource loopSource;
    private Coroutine fadeRoutine;
    private bool started;

    private void Awake()
    {
        Instance = this;

        introSource = CreateSource();
        loopSource = CreateSource();
        loopSource.loop = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.outputAudioMixerGroup = output;
        return source;
    }

    public void StartTheme()
    {
        if (started)
            return;
        started = true;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        introSource.volume = volume;
        loopSource.volume = volume;

        // Schedule both on the DSP clock so the loop takes over seamlessly
        double startTime = AudioSettings.dspTime + 0.1;
        double loopStart = startTime;

        if (introClip != null)
        {
            introSource.clip = introClip;
            introSource.PlayScheduled(startTime);
            loopStart += (double)introClip.samples / introClip.frequency;
        }

        if (loopClip != null)
        {
            loopSource.clip = loopClip;
            loopSource.PlayScheduled(loopStart);
        }
    }

    public void StopTheme()
    {
        if (!started)
            return;
        started = false;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        float timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            float remaining = 1f - Mathf.Clamp01(timer / fadeOutTime);
            introSource.volume = volume * remaining;
            loopSource.volume = volume * remaining;
            yield return null;
        }

        introSource.Stop();
        loopSource.Stop();
        fadeRoutine = null;
    }
}
