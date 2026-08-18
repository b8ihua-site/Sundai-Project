using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Tabキーで開くフィールドメニュー。レベル/経験値・所持金・持ち物・セーブをここにまとめる。
// シーンに何も配置しなくても起動時に自分でCanvasを組み立てて常駐する（[RuntimeInitializeOnLoadMethod]）
// 操作はすべてキーボード（矢印/WASD + Enter/Space + Backspace/Escape）
public class MenuController : MonoBehaviour
{
    public static MenuController Instance;

    const string MainSceneName = "mainscene";
    const int GridCols = 3;
    const float AnimDuration = 0.15f;

    // レイアウト：パネル四隅からの共通余白と、左右カードのサイズ・上下位置決めに使う定数
    const float PanelMargin = 48f;   // 左右の外側余白
    const float PanelMarginY = 24f;  // 上下の外側余白（左右より狭くする）
    const float TitleHeight = 36f;
    const float StatusHeight = 28f;
    const float RowGap = 12f;
    const float LeftCardWidth = 360f;
    const float RightCardWidth = 380f;

    enum Mode { Grid, Inventory, Settings, Collection }
    Mode mode = Mode.Grid;

    TMP_FontAsset font;
    Sprite roundedSprite;
    Sprite leftRoundedSprite; // 左側だけ角丸（右側は直角）。画面端に張り付くお金バッジ用
    Sprite circleSprite;
    Sprite ringSprite;
    Sprite menuBgSprite;

    GameObject canvasRoot;
    CanvasGroup canvasGroup;
    GameObject panel;
    RectTransform panelRect;
    Coroutine animCoroutine;

    // 左側：角丸ボタン
    readonly List<Image> gridButtons = new List<Image>();
    readonly List<string> gridLabels = new List<string> { "もちもの", "日記", "設定", "図鑑", "タイトルへ", "とじる" };
    readonly List<string> gridIconNames = new List<string> { "mochimono", "save", "settei", "zukan", "home", "modoru" };
    readonly List<string> gridIconGlyphs = new List<string> { "品", "存", "設", "図", "戻", "×" }; // 画像が読み込めなかった時のフォールバック
    Sprite[] gridIconSprites;
    readonly List<string> gridDescriptions = new List<string>
    {
        "もちものを確認できます",
        "これまでの出来事を記録できます",
        "音量などを調整できます",
        "たおした知識の風を確認できます",
        "タイトル画面にもどります",
        "メニューをとじます",
    };
    TextMeshProUGUI descriptionText;
    int gridIndex = 0;

    // 右側：プレイヤー情報
    TextMeshProUGUI levelText;
    TextMeshProUGUI hpText;
    TextMeshProUGUI moneyText;
    Image mathFill, scienceFill, ichigayaFill;
    TextMeshProUGUI mathCountText, scienceCountText, ichigayaCountText;

    // 持ち物パネル
    GameObject inventoryPanel;
    RectTransform inventoryContent;
    readonly List<PlayerAbilities.InventoryEntry> inventoryList = new List<PlayerAbilities.InventoryEntry>();
    readonly List<Image> inventoryRowBgs = new List<Image>();
    int inventoryIndex = 0;

    // 設定パネル（BGM/こうかおん おんりょう）
    GameObject settingsPanel;
    readonly List<Image> settingsRowBgs = new List<Image>();
    readonly List<TextMeshProUGUI> settingsValueTexts = new List<TextMeshProUGUI>();
    int settingsIndex = 0;

    // 図鑑パネル（倒した知識の風の数、読み取り専用）
    GameObject collectionPanel;
    readonly List<TextMeshProUGUI> collectionCountTexts = new List<TextMeshProUGUI>();

    TextMeshProUGUI statusText;

    bool isOpen;

    static readonly Color ButtonSelectedColor = new Color(0.45f, 0.55f, 0.9f, 1f); // 持ち物/設定パネルの行ハイライト用
    static readonly Color GridButtonSelectedColor = new Color(0.85f, 0.85f, 0.87f, 1f); // メニューボタンの選択ハイライト（薄いグレー）
    static readonly Color CardColor = new Color(0.4f, 0.4f, 0.42f, 0.22f); // 透明感のあるグレー（明るい背景に馴染む枠）
    static readonly Color TextDark = new Color(0.16f, 0.16f, 0.18f, 1f);  // 明るい背景の上に置く文字色
    static readonly Color TextLight = Color.white;                        // 暗い背景（ボタン・持ち物パネル）の上に置く文字色
    static readonly Color MoneyBadgeColor = new Color(0.3f, 0.3f, 0.33f, 0.92f); // お金バッジのグレー（独立表示なので他より少し濃くする）

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("MenuController");
        go.AddComponent<MenuController>();
    }

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
            return;
        }

        font = Resources.Load<TMP_FontAsset>("瀞ノグリッチ黒体H2 SDF"); // 試しにフォントを変更中
        menuBgSprite = Resources.Load<Sprite>("menu_bg");

        gridIconSprites = new Sprite[gridIconNames.Count];
        for (int i = 0; i < gridIconNames.Count; i++)
            gridIconSprites[i] = Resources.Load<Sprite>("UI/" + gridIconNames[i]);
        roundedSprite = CreateRoundedRectSprite();
        leftRoundedSprite = CreateLeftRoundedRectSprite(48, 16);
        circleSprite = CreateCircleSprite();
        ringSprite = CreateRingSprite();

        BuildUI();
        canvasRoot.SetActive(false);
        canvasGroup.alpha = 0f;

        SceneManager.sceneLoaded += (scene, mode2) =>
        {
            if (isOpen) SetOpen(false); // シーン遷移中に開きっぱなしにならないようにする
        };
    }

    void Update()
    {
        if (!isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Tab) && SceneManager.GetActiveScene().name == MainSceneName)
            {
                var controller = FindPlayerController();
                if (controller == null || !controller.movementLocked)
                    SetOpen(true);
            }
            return;
        }

        // メニューが開いている間はTabでも閉じられる
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SetOpen(false);
            return;
        }

        switch (mode)
        {
            case Mode.Grid: UpdateGridInput(); break;
            case Mode.Inventory: UpdateInventoryInput(); break;
            case Mode.Settings: UpdateSettingsInput(); break;
            case Mode.Collection: UpdateCollectionInput(); break;
        }
    }

    StarterAssets.ThirdPersonController FindPlayerController()
    {
        var player = GameObject.FindWithTag("Player");
        return player != null ? player.GetComponentInChildren<StarterAssets.ThirdPersonController>() : null;
    }

    void SetOpen(bool open)
    {
        isOpen = open;

        var controller = FindPlayerController();
        if (controller != null)
        {
            controller.movementLocked = open;
            controller.LockCameraPosition = open;
        }

        if (open)
        {
            mode = Mode.Grid;
            gridIndex = 0;
            inventoryPanel.SetActive(false);
            settingsPanel.SetActive(false);
            collectionPanel.SetActive(false);
            statusText.text = "";
            RefreshPlayerInfo();
            HighlightGrid();
        }

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimatePanel(open));
    }

    IEnumerator AnimatePanel(bool opening)
    {
        if (opening) canvasRoot.SetActive(true);

        float startScale = opening ? 0.85f : 1f;
        float endScale = opening ? 1f : 0.85f;
        float startAlpha = opening ? 0f : 1f;
        float endAlpha = opening ? 1f : 0f;

        float t = 0f;
        while (t < AnimDuration)
        {
            t += Time.deltaTime;
            float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / AnimDuration), 3f); // ease-out
            panelRect.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, p);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, p);
            yield return null;
        }

        panelRect.localScale = Vector3.one * endScale;
        canvasGroup.alpha = endAlpha;

        if (!opening) canvasRoot.SetActive(false);
    }

    // ---------- グリッド（左側ボタン）の操作 ----------

    void UpdateGridInput()
    {
        int dx = 0, dy = 0;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) dx = 1;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) dx = -1;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) dy = 1;
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) dy = -1;

        if (dx != 0 || dy != 0) MoveGrid(dx, dy);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            ActivateGrid(gridIndex);
    }

    void MoveGrid(int dx, int dy)
    {
        int col = gridIndex % GridCols;
        int row = gridIndex / GridCols;

        if (dx != 0)
        {
            int newCol = col + dx;
            if (newCol >= 0 && newCol < GridCols && row * GridCols + newCol < gridLabels.Count)
                col = newCol;
        }
        if (dy != 0)
        {
            int newRow = row + dy;
            if (newRow >= 0 && newRow * GridCols + col < gridLabels.Count)
                row = newRow;
        }

        gridIndex = Mathf.Clamp(row * GridCols + col, 0, gridLabels.Count - 1);
        HighlightGrid();
    }

    void HighlightGrid()
    {
        for (int i = 0; i < gridButtons.Count; i++)
            gridButtons[i].color = (i == gridIndex) ? GridButtonSelectedColor : Color.white;

        if (descriptionText != null && gridIndex >= 0 && gridIndex < gridDescriptions.Count)
            descriptionText.text = gridDescriptions[gridIndex];
    }

    void ActivateGrid(int index)
    {
        if (SEManager.Instance != null) SEManager.Instance.Play("select");

        switch (gridLabels[index])
        {
            case "もちもの": OpenInventory(); break;
            case "日記": DoSave(); break;
            case "設定": OpenSettings(); break;
            case "図鑑": OpenCollection(); break;
            case "タイトルへ": ReturnToTitle(); break;
            case "とじる": SetOpen(false); break;
        }
    }

    const float TitleFadeDuration = 1.0f;

    void ReturnToTitle()
    {
        if (MusicPlayer.Instance != null)
        {
            MusicPlayer.Instance.FadeOutAndPause(TitleFadeDuration);
            StartCoroutine(LoadSceneAfterDelay("TitleScene", TitleFadeDuration));
        }
        else
        {
            SceneManager.LoadScene("TitleScene");
        }
    }

    IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    void DoSave()
    {
        var pa = PlayerAbilities.Instance;
        var player = GameObject.FindWithTag("Player");
        if (pa == null || player == null) return;

        SaveSystem.Save(pa, player.transform);
        statusText.text = "セーブしました";
    }

    // ---------- 持ち物パネルの操作 ----------

    void OpenInventory()
    {
        mode = Mode.Inventory;
        inventoryIndex = 0;
        inventoryPanel.SetActive(true);
        RefreshInventory();
    }

    void CloseInventory()
    {
        mode = Mode.Grid;
        inventoryPanel.SetActive(false);
        HighlightGrid();
    }

    void UpdateInventoryInput()
    {
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
            return;
        }

        if (inventoryList.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            inventoryIndex = Mathf.Min(inventoryIndex + 1, inventoryList.Count - 1);
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            inventoryIndex = Mathf.Max(inventoryIndex - 1, 0);

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            HighlightInventory();

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            UseSelectedItem();
    }

    void HighlightInventory()
    {
        for (int i = 0; i < inventoryRowBgs.Count; i++)
            inventoryRowBgs[i].color = (i == inventoryIndex) ? ButtonSelectedColor : new Color(1f, 1f, 1f, 0.08f);
    }

    void UseSelectedItem()
    {
        if (inventoryIndex < 0 || inventoryIndex >= inventoryList.Count) return;

        var pa = PlayerAbilities.Instance;
        var db = ItemDatabase.Instance;
        if (pa == null || db == null) return;

        string itemId = inventoryList[inventoryIndex].itemId;
        var def = db.Find(itemId);
        if (def == null) return;
        if (!pa.ConsumeItem(itemId)) return;

        if (def.healAmount > 0)
            pa.currentHP = Mathf.Clamp(pa.currentHP + def.healAmount, 0, pa.maxHP);

        if (SEManager.Instance != null) SEManager.Instance.Play("select");

        RefreshPlayerInfo();
        RefreshInventory();
    }

    // ---------- 設定パネルの操作 ----------

    void OpenSettings()
    {
        mode = Mode.Settings;
        settingsIndex = 0;
        settingsPanel.SetActive(true);
        RefreshSettings();
        HighlightSettings();
    }

    void CloseSettings()
    {
        mode = Mode.Grid;
        settingsPanel.SetActive(false);
        HighlightGrid();
    }

    void UpdateSettingsInput()
    {
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettings();
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            settingsIndex = Mathf.Min(settingsIndex + 1, settingsRowBgs.Count - 1);
            HighlightSettings();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            settingsIndex = Mathf.Max(settingsIndex - 1, 0);
            HighlightSettings();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            AdjustSettingsValue(0.1f);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            AdjustSettingsValue(-0.1f);
        }
    }

    void HighlightSettings()
    {
        for (int i = 0; i < settingsRowBgs.Count; i++)
            settingsRowBgs[i].color = (i == settingsIndex) ? ButtonSelectedColor : new Color(1f, 1f, 1f, 0.08f);
    }

    void AdjustSettingsValue(float delta)
    {
        if (settingsIndex == 0 && MusicPlayer.Instance != null)
        {
            MusicPlayer.Instance.SetVolume(MusicPlayer.Instance.volume + delta);
        }
        else if (settingsIndex == 1 && SEManager.Instance != null)
        {
            SEManager.Instance.SetVolume(SEManager.Instance.masterVolume + delta);
            SEManager.Instance.Play("select"); // 変更後の音量を確認できるように鳴らす
        }

        RefreshSettings();
    }

    void RefreshSettings()
    {
        float bgm = MusicPlayer.Instance != null ? MusicPlayer.Instance.volume : 1f;
        float se = SEManager.Instance != null ? SEManager.Instance.masterVolume : 1f;

        if (settingsValueTexts.Count > 0) settingsValueTexts[0].text = $"{Mathf.RoundToInt(bgm * 100f)}%";
        if (settingsValueTexts.Count > 1) settingsValueTexts[1].text = $"{Mathf.RoundToInt(se * 100f)}%";
    }

    // ---------- 図鑑パネルの操作 ----------

    void OpenCollection()
    {
        mode = Mode.Collection;
        collectionPanel.SetActive(true);
        RefreshCollection();
    }

    void CloseCollection()
    {
        mode = Mode.Grid;
        collectionPanel.SetActive(false);
        HighlightGrid();
    }

    void UpdateCollectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
            CloseCollection();
    }

    void RefreshCollection()
    {
        var pa = PlayerAbilities.Instance;
        if (pa == null) return;

        if (collectionCountTexts.Count > 0) collectionCountTexts[0].text = $"{pa.mathDefeats}体";
        if (collectionCountTexts.Count > 1) collectionCountTexts[1].text = $"{pa.scienceDefeats}体";
        if (collectionCountTexts.Count > 2) collectionCountTexts[2].text = $"{pa.ichigayaDefeats}体";
    }

    // ---------- 表示更新 ----------

    void RefreshPlayerInfo()
    {
        var pa = PlayerAbilities.Instance;
        if (pa == null) return;

        levelText.text = $"Lv. {pa.level}";
        hpText.text = $"HP {pa.currentHP} / {pa.maxHP}";
        moneyText.text = $"{pa.money}";

        mathFill.fillAmount     = Mathf.Clamp01((float)pa.mathPower / pa.abilityThreshold);
        scienceFill.fillAmount  = Mathf.Clamp01((float)pa.sciencePower / pa.abilityThreshold);
        ichigayaFill.fillAmount = Mathf.Clamp01((float)pa.ichigayaPower / pa.abilityThreshold);

        mathCountText.text     = $"{pa.mathPower}/{pa.abilityThreshold}";
        scienceCountText.text  = $"{pa.sciencePower}/{pa.abilityThreshold}";
        ichigayaCountText.text = $"{pa.ichigayaPower}/{pa.abilityThreshold}";
    }

    void RefreshInventory()
    {
        var pa = PlayerAbilities.Instance;
        var db = ItemDatabase.Instance;

        for (int i = inventoryContent.childCount - 1; i >= 0; i--)
            Destroy(inventoryContent.GetChild(i).gameObject);
        inventoryRowBgs.Clear();
        inventoryList.Clear();

        if (pa == null) return;

        foreach (var entry in pa.inventory)
            if (entry.count > 0) inventoryList.Add(entry);

        if (inventoryList.Count == 0)
        {
            CreateText(inventoryContent, "（持ち物はありません）", 16f, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, new Vector2(340f, 28f), TextLight);
            return;
        }

        inventoryIndex = Mathf.Clamp(inventoryIndex, 0, inventoryList.Count - 1);

        foreach (var entry in inventoryList)
        {
            var def = db != null ? db.Find(entry.itemId) : null;
            string label = def != null ? $"{def.displayName} x{entry.count}" : $"{entry.itemId} x{entry.count}";

            var rowBg = CreateImage(inventoryContent, new Color(1f, 1f, 1f, 0.08f), Vector2.zero, new Vector2(340f, 34f));
            rowBg.gameObject.name = "ItemRow";
            rowBg.sprite = roundedSprite;
            rowBg.type = Image.Type.Sliced;

            CreateText(rowBg.transform, label, 16f, TextAlignmentOptions.MidlineLeft,
                new Vector2(14f, 0f), new Vector2(310f, 28f), TextLight);

            inventoryRowBgs.Add(rowBg);
        }

        HighlightInventory();
    }

    // ---------- UI構築 ----------

    void BuildUI()
    {
        canvasRoot = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasRoot);

        var canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasRoot.AddComponent<CanvasGroup>();

        var dimRect = CreateImage(canvasRoot.transform, new Color(0f, 0f, 0f, 0.6f), Vector2.zero, Vector2.zero).rectTransform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.sizeDelta = Vector2.zero;

        var panelImage = CreateImage(canvasRoot.transform, Color.white, Vector2.zero, new Vector2(640f, 440f));
        if (menuBgSprite != null)
        {
            panelImage.sprite = menuBgSprite;
            panelImage.type = Image.Type.Simple;
        }
        else
        {
            panelImage.color = new Color(0.08f, 0.08f, 0.1f, 1f);
            panelImage.sprite = roundedSprite;
            panelImage.type = Image.Type.Sliced;
        }
        panel = panelImage.gameObject;
        panelRect = panelImage.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        // 上＝タイトル、下＝ステータス、中央＝左右カードの3段構成。
        // 左右カードは同じ上下位置（PanelMargin基準）を共有するので高さが必ず揃い、
        // それぞれパネルの左端/右端からの余白も同じPanelMarginになる。
        var titleBar = CreateRect(panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(PanelMargin, -(PanelMarginY + TitleHeight)), new Vector2(-PanelMargin, -PanelMarginY));
        CreateTextStretch(titleBar, "メニュー", 26f, TextAlignmentOptions.TopLeft, TextDark);

        var leftCard = CreateRect(panel.transform,
            Vector2.zero, new Vector2(0f, 1f),
            new Vector2(PanelMargin, PanelMarginY + StatusHeight + RowGap),
            new Vector2(PanelMargin + LeftCardWidth, -(PanelMarginY + TitleHeight + RowGap)));
        var rightCard = CreateRect(panel.transform,
            new Vector2(1f, 0f), Vector2.one,
            new Vector2(-(PanelMargin + RightCardWidth), PanelMarginY + StatusHeight + RowGap),
            new Vector2(-PanelMargin, -(PanelMarginY + TitleHeight + RowGap)));

        BuildGrid(leftCard);
        BuildPlayerInfo(rightCard);
        BuildInventoryPanel(panel.transform);
        BuildSettingsPanel(panel.transform);
        BuildCollectionPanel(panel.transform);

        var statusBar = CreateRect(panel.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(PanelMargin, PanelMarginY), new Vector2(-PanelMargin, PanelMarginY + StatusHeight));

        var messageContainer = CreateRect(statusBar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, -StatusHeight / 2f), new Vector2(380f, StatusHeight / 2f));
        statusText = CreateTextStretch(messageContainer, "", 15f, TextAlignmentOptions.MidlineLeft, TextDark);

        // 右下の操作案内（キー案内）
        var hintContainer = CreateRect(statusBar, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-300f, -StatusHeight / 2f), new Vector2(0f, StatusHeight / 2f));
        CreateTextStretch(hintContainer,
            "<color=#C98F00>↑↓←→</color> いどう    <color=#C98F00>Enter</color> けってい",
            13f, TextAlignmentOptions.MidlineRight, TextDark);

        // お金は独立したバッジとして画面右上に表示する（最後に作ることで一番手前に描画される）
        BuildMoneyBadge(panel.transform);
    }

    void BuildMoneyBadge(Transform parent)
    {
        const float BadgeWidth = 150f;
        const float BadgeHeight = 32f;   // ¥アイコンと同じくらいの厚みにする
        const float IconSize = 26f;
        const float BleedPastEdge = 10f; // 右側は画面端から少しはみ出させる
        const float TopMargin = 16f;     // 上端が画面ぴったりにならないよう余白を持たせる

        // 影（浮いている感じを出す）
        var shadow = CreateRect(parent, Vector2.one, Vector2.one,
            new Vector2(BleedPastEdge - BadgeWidth - 3f, -TopMargin - BadgeHeight - 3f),
            new Vector2(BleedPastEdge - 3f, -TopMargin - 3f));
        var shadowImage = shadow.gameObject.AddComponent<Image>();
        shadowImage.color = new Color(0f, 0f, 0f, 0.25f);
        shadowImage.sprite = leftRoundedSprite;
        shadowImage.type = Image.Type.Sliced;

        var badge = CreateRect(parent, Vector2.one, Vector2.one,
            new Vector2(BleedPastEdge - BadgeWidth, -TopMargin - BadgeHeight),
            new Vector2(BleedPastEdge, -TopMargin));
        var badgeImage = badge.gameObject.AddComponent<Image>();
        badgeImage.color = MoneyBadgeColor;
        badgeImage.sprite = leftRoundedSprite;
        badgeImage.type = Image.Type.Sliced;

        var iconBadge = CreateImage(badge, new Color(0.95f, 0.78f, 0.3f, 1f), new Vector2(-BadgeWidth / 2f + 20f, 0f), new Vector2(IconSize, IconSize));
        iconBadge.sprite = circleSprite;
        CreateText(iconBadge.transform, "¥", 14f, TextAlignmentOptions.Center, Vector2.zero, new Vector2(IconSize, IconSize), TextDark);

        // 数字はバナー全体の中央に配置（「えん」は付けない）
        moneyText = CreateText(badge, "0", 17f, TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(70f, BadgeHeight), TextLight);
    }

    // ボタン1個のサイズと行/列の間隔。ボタン領域の高さは項目数から行数を割り出して決める
    // （項目数が増えても自動でレイアウトされ、帯はその下の残りスペースを全部使うので重ならない）
    const float ButtonWidth = 100f;
    const float ButtonHeight = 100f;
    const float ButtonRowGap = 16f;
    const float ButtonColGap = 16f;
    const float BandGap = 16f;

    void BuildGrid(RectTransform card)
    {
        int rows = Mathf.CeilToInt(gridLabels.Count / (float)GridCols);
        float buttonAreaHeight = rows * ButtonHeight + Mathf.Max(0, rows - 1) * ButtonRowGap;

        var buttonArea = CreateRect(card, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -buttonAreaHeight), Vector2.zero);

        float colPitch = ButtonWidth + ButtonColGap;
        float rowPitch = ButtonHeight + ButtonRowGap;

        for (int i = 0; i < gridLabels.Count; i++)
        {
            int col = i % GridCols;
            int row = i / GridCols;
            float x = (col - (GridCols - 1) / 2f) * colPitch;
            float y = buttonAreaHeight / 2f - ButtonHeight / 2f - row * rowPitch;

            // グレーの枠線（少し大きい灰色の角丸を下に敷き、内側に白の角丸を重ねて枠線に見せる）
            var btnBorder = CreateImage(buttonArea, new Color(0.6f, 0.6f, 0.63f, 0.5f), new Vector2(x, y), new Vector2(ButtonWidth, ButtonHeight));
            btnBorder.sprite = roundedSprite;
            btnBorder.type = Image.Type.Sliced;

            var btn = CreateImage(btnBorder.transform, Color.white, Vector2.zero, new Vector2(ButtonWidth - 2f, ButtonHeight - 2f));
            btn.sprite = roundedSprite;
            btn.type = Image.Type.Sliced;

            var button = btnBorder.gameObject.AddComponent<Button>();
            button.targetGraphic = btnBorder;
            int index = i;
            button.onClick.AddListener(() => { gridIndex = index; ActivateGrid(index); });

            // アイコン画像（Assets/Resources/UI/ から読み込み。見つからなければグリフ文字にフォールバック）
            var iconSprite = i < gridIconSprites.Length ? gridIconSprites[i] : null;
            if (iconSprite != null)
            {
                var iconImage = CreateImage(btn.transform, Color.white, new Vector2(0f, 16f), new Vector2(56f, 56f));
                iconImage.sprite = iconSprite;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
            }
            else
            {
                CreateText(btn.transform, gridIconGlyphs[i], 30f, TextAlignmentOptions.Center,
                    new Vector2(0f, 16f), new Vector2(100f, 40f), TextDark);
            }

            CreateText(btn.transform, gridLabels[i], 15f, TextAlignmentOptions.Center,
                new Vector2(0f, -25f), new Vector2(100f, 26f), TextDark);

            gridButtons.Add(btn);
        }

        // 左側の下の帯：選択中の項目の説明。ボタン領域より下の残りスペースを全部使う
        var descBand = CreateRect(card, Vector2.zero, Vector2.one,
            Vector2.zero, new Vector2(0f, -(buttonAreaHeight + BandGap)));
        var descImage = descBand.gameObject.AddComponent<Image>();
        descImage.color = CardColor;
        descImage.sprite = roundedSprite;
        descImage.type = Image.Type.Sliced;

        var descInner = CreateRect(descBand, Vector2.zero, Vector2.one, new Vector2(16f, 6f), new Vector2(-16f, -6f));
        descriptionText = CreateTextStretch(descInner, gridDescriptions[0], 15f, TextAlignmentOptions.Center, TextDark);
        descriptionText.enableWordWrapping = true;
    }

    void BuildPlayerInfo(RectTransform card)
    {
        // 背景カード（レイヤー感を出す）
        var cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = CardColor;
        cardImage.sprite = roundedSprite;
        cardImage.type = Image.Type.Sliced;

        levelText = CreateText(card, "Lv. 1", 22f, TextAlignmentOptions.MidlineLeft,
            new Vector2(-70f, 140f), new Vector2(200f, 30f), TextDark);

        hpText = CreateText(card, "HP 0 / 0", 18f, TextAlignmentOptions.MidlineLeft,
            new Vector2(-70f, 108f), new Vector2(200f, 26f), TextDark);

        (mathFill, mathCountText)         = CreateCircleGauge(card, "数学", new Color(0.4f, 0.6f, 1f), new Vector2(-120f, 0f));
        (scienceFill, scienceCountText)   = CreateCircleGauge(card, "理科", new Color(0.4f, 0.9f, 0.5f), new Vector2(0f, 0f));
        (ichigayaFill, ichigayaCountText) = CreateCircleGauge(card, "市ヶ谷", new Color(1f, 0.5f, 0.5f), new Vector2(120f, 0f));
    }

    // 円状の棒グラフ（リング）。中央の空きスペースにラベルと達成数を表示する
    (Image fill, TextMeshProUGUI countText) CreateCircleGauge(Transform parent, string label, Color color, Vector2 anchoredPos)
    {
        const float RingSize = 78f;

        // 光彩（グロー）：色付きの少し大きい円を薄く後ろに敷く
        CreateImage(parent, new Color(color.r, color.g, color.b, 0.25f), anchoredPos, new Vector2(RingSize + 14f, RingSize + 14f)).sprite = circleSprite;

        // 背景リング（未達成分）
        CreateImage(parent, new Color(1f, 1f, 1f, 0.12f), anchoredPos, new Vector2(RingSize, RingSize)).sprite = ringSprite;

        // 進捗リング（達成分）：リング形のスプライトにRadial360フィルをかけて円状の棒グラフにする
        var fill = CreateImage(parent, color, anchoredPos, new Vector2(RingSize, RingSize));
        fill.sprite = ringSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.fillAmount = 0f;

        // リング中央の空きスペースにラベルと数値をまとめて表示
        CreateText(parent, label, 14f, TextAlignmentOptions.Center,
            anchoredPos + new Vector2(0f, 9f), new Vector2(RingSize, 20f), TextDark);
        var countText = CreateText(parent, "0/0", 15f, TextAlignmentOptions.Center,
            anchoredPos + new Vector2(0f, -10f), new Vector2(RingSize, 20f), TextDark);

        return (fill, countText);
    }

    void BuildInventoryPanel(Transform parent)
    {
        var panelImage = CreateImage(parent, new Color(0.05f, 0.05f, 0.07f, 0.98f), Vector2.zero, new Vector2(560f, 340f));
        panelImage.sprite = roundedSprite;
        panelImage.type = Image.Type.Sliced;
        inventoryPanel = panelImage.gameObject;

        CreateText(inventoryPanel.transform, "もちもの（Backspaceでもどる）", 18f, TextAlignmentOptions.Center,
            new Vector2(0f, 145f), new Vector2(500f, 28f), TextLight);

        var scrollArea = new GameObject("InventoryArea", typeof(RectTransform));
        var scrollRect = scrollArea.GetComponent<RectTransform>();
        scrollRect.SetParent(inventoryPanel.transform, false);
        scrollRect.anchorMin = scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 1f);
        scrollRect.anchoredPosition = new Vector2(0f, 110f);
        scrollRect.sizeDelta = new Vector2(340f, 260f);

        inventoryContent = scrollRect;
        var layout = scrollArea.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 6f;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        inventoryPanel.SetActive(false);
    }

    void BuildSettingsPanel(Transform parent)
    {
        var panelImage = CreateImage(parent, new Color(0.05f, 0.05f, 0.07f, 0.98f), Vector2.zero, new Vector2(480f, 220f));
        panelImage.sprite = roundedSprite;
        panelImage.type = Image.Type.Sliced;
        settingsPanel = panelImage.gameObject;

        CreateText(settingsPanel.transform, "設定（↑↓で選択・←→で調整・Backspaceでもどる）", 14f, TextAlignmentOptions.Center,
            new Vector2(0f, 85f), new Vector2(440f, 40f), TextLight);

        string[] labels = { "BGM おんりょう", "こうかおん おんりょう" };
        float[] rowY = { 20f, -40f };

        for (int i = 0; i < labels.Length; i++)
        {
            var rowBg = CreateImage(settingsPanel.transform, new Color(1f, 1f, 1f, 0.08f), new Vector2(0f, rowY[i]), new Vector2(420f, 50f));
            rowBg.sprite = roundedSprite;
            rowBg.type = Image.Type.Sliced;
            settingsRowBgs.Add(rowBg);

            CreateText(rowBg.transform, labels[i], 16f, TextAlignmentOptions.MidlineLeft,
                new Vector2(-170f, 0f), new Vector2(220f, 30f), TextLight);

            var valueText = CreateText(rowBg.transform, "100%", 18f, TextAlignmentOptions.MidlineRight,
                new Vector2(170f, 0f), new Vector2(100f, 30f), TextLight);
            settingsValueTexts.Add(valueText);
        }

        settingsPanel.SetActive(false);
    }

    void BuildCollectionPanel(Transform parent)
    {
        var panelImage = CreateImage(parent, new Color(0.05f, 0.05f, 0.07f, 0.98f), Vector2.zero, new Vector2(480f, 260f));
        panelImage.sprite = roundedSprite;
        panelImage.type = Image.Type.Sliced;
        collectionPanel = panelImage.gameObject;

        CreateText(collectionPanel.transform, "図鑑：たおした知識の風（Backspaceでもどる）", 14f, TextAlignmentOptions.Center,
            new Vector2(0f, 105f), new Vector2(440f, 26f), TextLight);

        string[] labels = { "数学の風（青）", "理科の風（緑）", "市ヶ谷の風（赤）" };
        float[] rowY = { 50f, 0f, -50f };

        for (int i = 0; i < labels.Length; i++)
        {
            var rowBg = CreateImage(collectionPanel.transform, new Color(1f, 1f, 1f, 0.08f), new Vector2(0f, rowY[i]), new Vector2(420f, 42f));
            rowBg.sprite = roundedSprite;
            rowBg.type = Image.Type.Sliced;

            CreateText(rowBg.transform, labels[i], 16f, TextAlignmentOptions.MidlineLeft,
                new Vector2(-170f, 0f), new Vector2(240f, 26f), TextLight);

            var countText = CreateText(rowBg.transform, "0体", 16f, TextAlignmentOptions.MidlineRight,
                new Vector2(170f, 0f), new Vector2(100f, 26f), TextLight);
            collectionCountTexts.Add(countText);
        }

        collectionPanel.SetActive(false);
    }

    // ---------- 汎用ヘルパー ----------

    TextMeshProUGUI CreateText(Transform parent, string content, float fontSize, TextAlignmentOptions align, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;

        return text;
    }

    Image CreateImage(Transform parent, Color color, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject("Image", typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    // 親の矩形からの余白(offsetMin/Max)でエッジに固定配置する矩形を作る（角丸パネルの土台など、
    // 画面サイズが変わっても親との余白を一定に保ちたい要素に使う）
    RectTransform CreateRect(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject("Rect", typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    // 親の矩形いっぱいに広がるテキストを作る（親側のサイズが可変でもレイアウトが崩れない）
    TextMeshProUGUI CreateTextStretch(Transform parent, string content, float fontSize, TextAlignmentOptions align, Color color)
    {
        var rect = CreateRect(parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    // 角丸の9-sliceスプライトを生成する（Unity標準のUISpriteは角の丸みがほぼ見えないため自前で作る）
    // アンチエイリアス帯の半幅（px）。境界からこの距離の範囲でアルファを滑らかに0〜1へ遷移させる
    const float EdgeAA = 1.25f;

    Sprite CreateRoundedRectSprite(int size = 64, int radius = 20)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float dx = Mathf.Max(0f, Mathf.Max(radius - px, px - (size - radius)));
                float dy = Mathf.Max(0f, Mathf.Max(radius - py, py - (size - radius)));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((radius - dist) / EdgeAA + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    // 左側だけ角丸、右側は直角の9-sliceスプライトを生成する（画面端に張り付くお金バッジ用）
    Sprite CreateLeftRoundedRectSprite(int size = 64, int radius = 20)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                // 右側（px >= radius）は角丸判定に含めない＝常に不透明で直角のまま
                float dx = px < radius ? Mathf.Max(0f, radius - px) : 0f;
                float dy = px < radius ? Mathf.Max(0f, Mathf.Max(radius - py, py - (size - radius))) : 0f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((radius - dist) / EdgeAA + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    Sprite CreateCircleSprite(int size = 128)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        float r = size / 2f;
        Vector2 center = new Vector2(r, r);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01((r - dist) / EdgeAA + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    // リング（ドーナツ状）のスプライトを生成する。Radial360フィルと組み合わせて円状の棒グラフに使う
    Sprite CreateRingSprite(int size = 128, float thicknessRatio = 0.22f)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        float outerR = size / 2f;
        float innerR = outerR * (1f - thicknessRatio);
        Vector2 center = new Vector2(outerR, outerR);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float outerAlpha = Mathf.Clamp01((outerR - dist) / EdgeAA + 0.5f);
                float innerAlpha = Mathf.Clamp01((dist - innerR) / EdgeAA + 0.5f);
                float alpha = Mathf.Min(outerAlpha, innerAlpha);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }
}
