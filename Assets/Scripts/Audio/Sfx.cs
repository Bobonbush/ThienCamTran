using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central SFX playback. Cues are defined by id in the SfxLibrary asset
/// (Resources/SfxLibrary) and played through a small pool of AudioSources
/// routed into the Sfx mixer group, with per-cue pitch variation, an
/// anti-repeat clip picker and a spam cooldown.
/// </summary>
public static class Sfx
{
    private const string LibraryResourcePath = "SfxLibrary";
    private const int PoolSize = 12;

    private static SfxLibrary library;
    private static bool loadAttempted;

    private static readonly Dictionary<string, SfxLibrary.Cue> cueById = new Dictionary<string, SfxLibrary.Cue>();
    private static readonly Dictionary<string, float> nextAllowedTime = new Dictionary<string, float>();
    private static readonly Dictionary<string, AudioClip> lastClipByCue = new Dictionary<string, AudioClip>();
    private static readonly List<AudioSource> pool = new List<AudioSource>(PoolSize);
    private static GameObject poolRoot;
    private static int nextVoice;

    public static AudioMixerGroup OutputGroup
    {
        get
        {
            SfxLibrary lib = GetLibrary();
            return lib != null ? lib.output : null;
        }
    }

    public static void Play(string cueId, float volumeScale = 1f)
    {
        SfxLibrary.Cue cue = ResolveCue(cueId);
        if (cue == null)
            return;

        float now = Time.unscaledTime;
        if (nextAllowedTime.TryGetValue(cueId, out float allowedAt) && now < allowedAt)
            return;

        AudioClip clip = ChooseClip(cue);
        if (clip == null)
            return;

        nextAllowedTime[cueId] = now + cue.cooldown;
        lastClipByCue[cueId] = clip;

        AudioSource voice = GetVoice();
        if (voice == null)
            return;

        voice.pitch = Random.Range(cue.minPitch, cue.maxPitch);
        voice.PlayOneShot(clip, Mathf.Clamp01(cue.volume * volumeScale));

        if (cue.layerClips != null && cue.layerClips.Length > 0)
        {
            AudioClip layer = cue.layerClips[Random.Range(0, cue.layerClips.Length)];
            if (layer != null)
                voice.PlayOneShot(layer, Mathf.Clamp01(cue.volume * cue.layerVolume * volumeScale));
        }
    }

    private static SfxLibrary.Cue ResolveCue(string cueId)
    {
        if (string.IsNullOrEmpty(cueId) || GetLibrary() == null)
            return null;

        cueById.TryGetValue(cueId, out SfxLibrary.Cue cue);
        return cue;
    }

    private static SfxLibrary GetLibrary()
    {
        if (library == null && !loadAttempted)
        {
            loadAttempted = true;
            library = Resources.Load<SfxLibrary>(LibraryResourcePath);
            if (library == null)
            {
                Debug.LogWarning($"Sfx: no SfxLibrary asset found at Resources/{LibraryResourcePath}; SFX disabled.");
                return null;
            }

            cueById.Clear();
            foreach (SfxLibrary.Cue cue in library.cues)
            {
                if (cue != null && !string.IsNullOrEmpty(cue.id))
                    cueById[cue.id] = cue;
            }
        }

        return library;
    }

    private static AudioSource GetVoice()
    {
        if (poolRoot == null)
        {
            poolRoot = new GameObject("Sfx Pool");
            Object.DontDestroyOnLoad(poolRoot);
            pool.Clear();
            nextVoice = 0;

            for (int i = 0; i < PoolSize; i++)
            {
                AudioSource source = poolRoot.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.outputAudioMixerGroup = OutputGroup;
                pool.Add(source);
            }
        }

        if (pool.Count == 0)
            return null;

        AudioSource voice = pool[nextVoice];
        nextVoice = (nextVoice + 1) % pool.Count;
        return voice;
    }

    private static AudioClip ChooseClip(SfxLibrary.Cue cue)
    {
        if (cue.clips == null || cue.clips.Length == 0)
            return null;

        lastClipByCue.TryGetValue(cue.id, out AudioClip previous);

        AudioClip picked = cue.clips[Random.Range(0, cue.clips.Length)];
        if (picked != previous || cue.clips.Length == 1)
            return picked;

        // Re-roll once so back-to-back plays don't sound identical.
        return cue.clips[Random.Range(0, cue.clips.Length)];
    }
}

/// <summary>Cue ids kept in one place so call sites don't scatter magic strings.</summary>
public static class SfxId
{
    public const string PlayerFootstep = "player.footstep";
    public const string PlayerJump = "player.jump";
    public const string PlayerLand = "player.land";
    public const string PlayerDash = "player.dash";
    public const string PlayerAttack = "player.attack";
    public const string PlayerHurt = "player.hurt";
    public const string PlayerDeath = "player.death";
    public const string PlayerHeal = "player.heal";
    public const string PlayerPickup = "player.pickup";
    public const string PlayerRevive = "player.revive";

    public const string UiHover = "ui.hover";
    public const string UiConfirm = "ui.confirm";
    public const string UiDecline = "ui.decline";
    public const string UiPause = "ui.pause";
    public const string UiUnpause = "ui.unpause";
}
