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
    }

    void PlayCurrent()
    {
        if (tracks.Length == 0) return;
        CurrentTrackIndex = playOrder[currentOrderIndex];
        audioSource.clip = tracks[CurrentTrackIndex];
        audioSource.Play();
        MusicPlayerUI.Instance?.UpdateUI();
    }

    public void Play() 
    { 
        audioSource.Play();
        MusicPlayerUI.Instance?.UpdateUI();
    }

    public void Pause()
    {
        audioSource.Pause();
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
}