using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer設定")]
    [Tooltip("BGMやSFXを管理するAudioMixer")]
    public AudioMixer mainMixer;

    [Tooltip("BGM音量パラメータ名（AudioMixerでExposeした名前）")]
    public string bgmVolumeParam = "BGMVolume";

    [Tooltip("SFX音量パラメータ名（AudioMixerでExposeした名前）")]
    public string sfxVolumeParam = "SFXVolume";

    [Header("Audio Sources")]
    [Tooltip("BGM再生用AudioSource（ループ設定を有効にしてください）")]
    public AudioSource bgmSource;

    [Tooltip("単発SFX用AudioSource（ボタン音など）")]
    public AudioSource sfxSource;

    [Tooltip("同時再生SFX用AudioSource（攻撃・爆発など）")]
    public AudioSource sfxOneShotSource;

    [Header("Audio Clips")]
    [Tooltip("BGM用AudioClipリスト（名前でアクセス）")]
    public List<AudioClip> bgmClips = new List<AudioClip>();

    [Tooltip("SFX用AudioClipリスト（名前でアクセス）")]
    public List<AudioClip> sfxClips = new List<AudioClip>();

    [Header("Clip個別音量 (0~1)")]
    [Tooltip("BGM Clipごとの個別音量。Clipリスト順に設定")]
    public List<float> bgmClipVolumes = new List<float>();

    [Tooltip("SFX Clipごとの個別音量。Clipリスト順に設定")]
    public List<float> sfxClipVolumes = new List<float>();

    [Header("UIスライダー")]
    [Tooltip("BGM全体音量調整用スライダー")]
    public Slider bgmSlider;

    [Tooltip("SFX全体音量調整用スライダー")]
    public Slider sfxSlider;

    [Header("Clip個別音量スライダー")]
    [Tooltip("BGM Clip 個別音量スライダー（インスペクタで設定）")]
    public List<Slider> bgmClipSliders = new List<Slider>();

    [Tooltip("SFX Clip 個別音量スライダー（インスペクタで設定）")]
    public List<Slider> sfxClipSliders = new List<Slider>();

    private Dictionary<string, AudioClip> bgmDict;
    private Dictionary<string, AudioClip> sfxDict;
    private Dictionary<string, float> bgmVolumeDict;
    private Dictionary<string, float> sfxVolumeDict;

    private Coroutine currentBGMFadeCoroutine;
    private Coroutine currentSFXLoopFadeCoroutine;

#if UNITY_WEBGL
    private bool hasUserInteracted = false;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDictionaries();
            InitializeSliders();
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if UNITY_WEBGL
    private void Start()
    {
        StartCoroutine(WaitForUserGesture());
        StartCoroutine(DelayedInitializeClipSliders());
    }
#else
    private void Start()
    {
        StartCoroutine(DelayedInitializeClipSliders());
    }
#endif

    private IEnumerator DelayedInitializeClipSliders()
    {
        yield return new WaitForSeconds(0.3f); // UI・Audioロード待ち
        InitializeClipVolumeSliders();
        Debug.Log("Clipスライダー初期化完了");
    }

#if UNITY_WEBGL
    private IEnumerator WaitForUserGesture()
    {
        while (!hasUserInteracted)
        {
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                hasUserInteracted = true;
            yield return null;
        }
    }
#endif

    private void InitializeDictionaries()
    {
        bgmDict = new Dictionary<string, AudioClip>();
        bgmVolumeDict = new Dictionary<string, float>();
        for (int i = 0; i < bgmClips.Count; i++)
        {
            var clip = bgmClips[i];
            if (clip != null && !bgmDict.ContainsKey(clip.name))
            {
                bgmDict.Add(clip.name, clip);
                float volume = (i < bgmClipVolumes.Count) ? bgmClipVolumes[i] : 1f;
                bgmVolumeDict.Add(clip.name, Mathf.Clamp01(volume));
            }
        }

        sfxDict = new Dictionary<string, AudioClip>();
        sfxVolumeDict = new Dictionary<string, float>();
        for (int i = 0; i < sfxClips.Count; i++)
        {
            var clip = sfxClips[i];
            if (clip != null && !sfxDict.ContainsKey(clip.name))
            {
                sfxDict.Add(clip.name, clip);
                float volume = (i < sfxClipVolumes.Count) ? sfxClipVolumes[i] : 1f;
                sfxVolumeDict.Add(clip.name, Mathf.Clamp01(volume));
            }
        }
    }

    private void InitializeSliders()
    {
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    private void InitializeClipVolumeSliders()
    {
        // BGM側
        if (bgmClipSliders == null || bgmClips == null) return;

        for (int i = 0; i < Mathf.Min(bgmClipSliders.Count, bgmClips.Count); i++)
        {
            if (bgmClipSliders[i] == null || bgmClips[i] == null)
            {
                Debug.LogWarning($"BGMスライダーまたはクリップがnullです (index {i})");
                continue;
            }

            int index = i;

            // bgmClipVolumesの要素数チェック
            float defaultVolume = (i < bgmClipVolumes.Count) ? bgmClipVolumes[i] : 1f;
            bgmClipSliders[i].value = defaultVolume;

            bgmClipSliders[i].onValueChanged.AddListener((v) =>
            {
                string name = bgmClips[index].name;
                bgmVolumeDict[name] = Mathf.Clamp01(v);

                if (bgmSource.isPlaying && bgmSource.clip != null && bgmSource.clip.name == name)
                    bgmSource.volume = v;
            });
        }

        // SFX側
        if (sfxClipSliders == null || sfxClips == null) return;

        for (int i = 0; i < Mathf.Min(sfxClipSliders.Count, sfxClips.Count); i++)
        {
            if (sfxClipSliders[i] == null || sfxClips[i] == null)
            {
                Debug.LogWarning($"SFXスライダーまたはクリップがnullです (index {i})");
                continue;
            }

            int index = i;

            float defaultVolume = (i < sfxClipVolumes.Count) ? sfxClipVolumes[i] : 1f;
            sfxClipSliders[i].value = defaultVolume;

            sfxClipSliders[i].onValueChanged.AddListener((v) =>
            {
                string name = sfxClips[index].name;
                sfxVolumeDict[name] = Mathf.Clamp01(v);
            });
        }
    }


    // =============================
    // BGM再生（フェードイン対応）
    // =============================
    public void PlayBGM(string name, bool loop = true, float fadeDuration = 0f)
    {
        if (!bgmDict.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning($"指定されたBGMが見つかりません: {name}");
            return;
        }

    #if UNITY_WEBGL
        StartCoroutine(WaitForUserGestureThenPlayBGM(clip, name, loop, fadeDuration));
    #else
        StartCoroutine(WaitAndPlayBGM(clip, name, loop, fadeDuration));
    #endif
    }

    private IEnumerator WaitAndPlayBGM(AudioClip clip, string name, bool loop, float fadeDuration)
    {
        int safety = 0;
        while (clip.loadState != AudioDataLoadState.Loaded && safety < 100)
        {
            safety++;
            yield return new WaitForSeconds(0.05f); // 0.05秒間隔で再チェック
        }
        PlayBGMInternal(clip, name, loop, fadeDuration);
    }

    #if UNITY_WEBGL
    private IEnumerator WaitForUserGestureThenPlayBGM(AudioClip clip, string name, bool loop, float fadeDuration)
    {
        while (!hasUserInteracted)
            yield return null;

        yield return WaitAndPlayBGM(clip, name, loop, fadeDuration);
    }
    #endif

    private void PlayBGMInternal(AudioClip clip, string name, bool loop, float fadeDuration)
    {
        float targetVolume = bgmVolumeDict.ContainsKey(name) ? bgmVolumeDict[name] : 1f;

        if (currentBGMFadeCoroutine != null)
            StopCoroutine(currentBGMFadeCoroutine);

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = 0f;
        bgmSource.Play();

        if (fadeDuration > 0f)
            currentBGMFadeCoroutine = StartCoroutine(FadeAudioSource(bgmSource, targetVolume, fadeDuration));
        else
            bgmSource.volume = targetVolume;
    }

    public void StopBGM(float fadeDuration = 0f)
    {
        if (fadeDuration > 0f)
        {
            if (currentBGMFadeCoroutine != null)
                StopCoroutine(currentBGMFadeCoroutine);
            currentBGMFadeCoroutine = StartCoroutine(FadeAudioSource(bgmSource, 0f, fadeDuration, true));
        }
        else
        {
            bgmSource.Stop();
        }
    }

    // =============================
    // ループSFX再生
    // =============================
    public void PlaySFXLoop(string name)
    {
        if (!sfxDict.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning($"SFXが見つかりません: {name}");
            return;
        }

        float volume = sfxVolumeDict.ContainsKey(name) ? sfxVolumeDict[name] : 1f;

        sfxOneShotSource.clip = clip;
        sfxOneShotSource.loop = true;
        sfxOneShotSource.volume = volume;
        sfxOneShotSource.Play();
    }

    public void StopSFXLoop(float fadeDuration = 0f)
    {
        if (!sfxOneShotSource.isPlaying) return;

        if (fadeDuration > 0f)
        {
            if (currentSFXLoopFadeCoroutine != null)
                StopCoroutine(currentSFXLoopFadeCoroutine);
            currentSFXLoopFadeCoroutine = StartCoroutine(FadeAudioSource(sfxOneShotSource, 0f, fadeDuration, true));
        }
        else
        {
            sfxOneShotSource.Stop();
        }
    }

    // =============================
    // 単発SFX再生
    // =============================
    public void PlaySFXSingle(string name)
    {
        if (!sfxDict.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning($"指定されたSFXが見つかりません: {name}");
            return;
        }

    #if UNITY_WEBGL
        StartCoroutine(WaitForUserGestureThenPlaySFX(clip, name));
    #else
        StartCoroutine(WaitAndPlaySFX(clip, name));
    #endif
    }

    private IEnumerator WaitAndPlaySFX(AudioClip clip, string name)
    {
        int safety = 0;
        while (clip.loadState != AudioDataLoadState.Loaded && safety < 100)
        {
            safety++;
            yield return new WaitForSeconds(0.05f);
        }

        PlaySFXInternal(clip, name);
    }

    #if UNITY_WEBGL
    private IEnumerator WaitForUserGestureThenPlaySFX(AudioClip clip, string name)
    {
        while (!hasUserInteracted)
            yield return null;

        yield return WaitAndPlaySFX(clip, name);
    }
    #endif

    private void PlaySFXInternal(AudioClip clip, string name)
    {
        float volume = sfxVolumeDict.ContainsKey(name) ? sfxVolumeDict[name] : 1f;
        sfxSource.volume = volume;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXMultiple(string name)
    {
        if (!sfxDict.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning($"SFXが見つかりません: {name}");
            return;
        }

        float volume = sfxVolumeDict.ContainsKey(name) ? sfxVolumeDict[name] : 1f;
        sfxOneShotSource.PlayOneShot(clip, volume);
    }

    // =============================
    // AudioMixer連携音量調整
    // =============================
    public void SetBGMVolume(float value)
    {
        mainMixer.SetFloat(bgmVolumeParam, Mathf.Log10(Mathf.Clamp(value, 0.001f, 1f)) * 20);
    }

    public void SetSFXVolume(float value)
    {
        mainMixer.SetFloat(sfxVolumeParam, Mathf.Log10(Mathf.Clamp(value, 0.001f, 1f)) * 20);
    }

    // =============================
    // フェード処理 (Time.unscaledDeltaTime対応)
    // =============================
    private IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float duration, bool stopAfterFade = false)
    {
        float startVolume = source.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, t / duration);
            yield return null;
        }

        source.volume = targetVolume;
        if (stopAfterFade)
            source.Stop();
    }
}
