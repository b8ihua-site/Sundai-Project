using UnityEngine;
using System.Collections.Generic;

public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance;

    [Header("楽曲リスト")]
    public AudioClip[] tracks;

    [Header("設定")]
    public bool shuffle = true;
    public bool repeat = false;

    private AudioSource audioSource;
    private List<int> playOrder = new List<int>();
    private int currentOrderIndex = 0;
    public int CurrentTrackIndex { get; private set; } = 0;
    public bool IsPlaying => audioSource.isPlaying;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
    }

private bool isWaiting = true; // ★追加
private bool isPaused = false;
private bool isOverridden = false; // バトルBGM等で通常プレイリストを一時退避中か
private AudioClip savedClip;
private float savedTime;

void Start()
{
    BuildPlayOrder();
    StartCoroutine(PlayAfterDelay());
}

System.Collections.IEnumerator PlayAfterDelay()
{
    yield return new WaitForSeconds(2f);
    isWaiting = false; // ★待機終了
    PlayCurrent();
}

void Update()
{
    if (isWaiting) return; // ★待機中は何もしない
    if (isPaused) return; // 一時停止中は自動送りしない
    if (isOverridden) return; // バトルBGM再生中は通常プレイリストを進めない

    if (!audioSource.isPlaying && tracks.Length > 0)
    {
        Next();
    }
}

    void BuildPlayOrder()
    {
        playOrder.Clear();
        for (int i = 0; i < tracks.Length; i++)
            playOrder.Add(i);

        if (shuffle)
        {
            for (int i = playOrder.Count - 1; i > 0; i--)
            {
                int r = Random.Range(0, i + 1);
                int tmp = playOrder[i];
                playOrder[i] = playOrder[r];
                playOrder[r] = tmp;
            }
        }

        currentOrderIndex = 0;
        if (playOrder.Count > 0)
            CurrentTrackIndex = playOrder[currentOrderIndex];
    }

    // 現在再生中の曲を除いた「次に再生される順」の曲インデックス一覧（先頭が次に再生される曲）
    // maxCountを指定すると、先頭からその件数だけに絞って返す（-1で全件）
    public List<int> GetUpcomingTrackOrder(int maxCount = -1)
    {
        var result = new List<int>();
        if (playOrder.Count == 0) return result;

        int count = maxCount < 0 ? playOrder.Count : Mathf.Min(maxCount, playOrder.Count);
        for (int step = 1; step <= count; step++)
        {
            int idx = (currentOrderIndex + step) % playOrder.Count;
            result.Add(playOrder[idx]);
        }
        return result;
    }

    void PlayCurrent()
    {
        if (tracks.Length == 0) return;
        CurrentTrackIndex = playOrder[currentOrderIndex];
        audioSource.clip = tracks[CurrentTrackIndex];
        audioSource.Play();
        isPaused = false;
        MusicPlayerUI.Instance?.UpdateUI();
    }

    public void Play()
    {
        audioSource.Play();
        isPaused = false;
        MusicPlayerUI.Instance?.UpdateUI();
    }

    public void Pause()
    {
        audioSource.Pause();
        isPaused = true;
        MusicPlayerUI.Instance?.UpdateUI();
    }

    public void Next()
    {
        currentOrderIndex = (currentOrderIndex + 1) % playOrder.Count;
        PlayCurrent();
    }

    public void Prev()
    {
        // 3秒以上経過していたら曲の最初に戻る
        if (audioSource.time > 3f)
        {
            audioSource.time = 0f;
            return;
        }
        currentOrderIndex = (currentOrderIndex - 1 + playOrder.Count) % playOrder.Count;
        PlayCurrent();
    }

    public void ToggleShuffle()
    {
        shuffle = !shuffle;
        BuildPlayOrder();
        MusicPlayerUI.Instance?.UpdateUI();
    }

    public void ToggleRepeat()
    {
        repeat = !repeat;
        audioSource.loop = repeat;
        MusicPlayerUI.Instance?.UpdateUI();
    }

    public void PlayTrack(int index)
    {
        currentOrderIndex = playOrder.IndexOf(index);
        if (currentOrderIndex < 0) currentOrderIndex = 0;
        PlayCurrent();
    }

    public AudioClip GetCurrentClip() => tracks.Length > 0 ? tracks[CurrentTrackIndex] : null;

    // 通常のプレイリスト再生を一時退避して、指定した曲をループ再生する（バトルBGM等）
    public void PlayOverride(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        if (!isOverridden)
        {
            savedClip = audioSource.clip;
            savedTime = audioSource.time;
        }

        isOverridden = true;
        audioSource.loop = loop;
        audioSource.clip = clip;
        audioSource.Play();
        MusicPlayerUI.Instance?.UpdateUI();
    }

    // PlayOverride前の状態に戻して、通常のプレイリスト再生を再開する
    public void StopOverride()
    {
        if (!isOverridden) return;
        isOverridden = false;
        audioSource.loop = repeat;

        if (savedClip != null)
        {
            audioSource.clip = savedClip;
            audioSource.time = savedTime;
            audioSource.Play();
        }
        else
        {
            PlayCurrent();
        }

        MusicPlayerUI.Instance?.UpdateUI();
    }
}