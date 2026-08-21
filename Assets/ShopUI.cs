using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// 自販機などの「お店」用UI。InteractSystemがisShopなInteractableObjectをF決定した時に呼ぶ。
// シーンに何も配置しなくても起動時に自分でCanvasを組み立てて常駐する（[RuntimeInitializeOnLoadMethod]）
// ゲーム画面を暗くせず、商品カードがフィールドに浮かぶような見た目にしている
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance;

    const float CardWidth = 160f;
    const float CardHeight = 240f; // アイコン+値段タグ+ボタンを縦に積むための高さ
    const float CardGap = 24f;
    const float RowWidth = CardWidth * 3f + CardGap * 2f;
    const float TopMargin = 70f;
    const float DescGap = 22f;
    const float DescHeight = 100f;

    TMP_FontAsset font;
    TMP_FontAsset descFont; // 説明帯（商品名・説明文）専用のフォント
    Sprite roundedSprite;
    Sprite leftRoundedSprite;
    Sprite glowSprite;
    Sprite keyCapSprite; // キー案内バッジ用（既存の常駐UIのキーバッジと同じ、控えめな角丸四角）
    Sprite jihankiOnSprite;  // 購入ボタン（選択中）
    Sprite jihankiOffSprite; // 購入ボタン（通常）
    Sprite jihankiBgSprite;  // ドリンクをまとめて囲う角丸フレームの背景
    Sprite descUpperSprite;  // 説明帯：商品名の行の背景
    Sprite descBottomSprite; // 説明帯：説明文の行の背景

    GameObject canvasRoot;
    CanvasGroup mainUiGroup; // カード・説明帯・お金バッジ・キー案内をまとめて持つ（入手演出中はこれごとフェードアウト）
    RectTransform cardsRow;
    TextMeshProUGUI moneyText;
    TextMeshProUGUI statusText;
    TextMeshProUGUI descNameText;
    TextMeshProUGUI descBodyText;
    Image descProductIcon;

    AudioSource audioSource;
    AudioClip buySE;   // 自販機の排出音
    AudioClip getSE;   // 入手演出のファンファーレ

    GameObject revealPanel;
    CanvasGroup revealGroup;
    Image revealGlow;
    Image revealIcon;
    TextMeshProUGUI revealText;
    Coroutine glowCoroutine;
    Coroutine mainFadeCoroutine;
    Coroutine revealFadeCoroutine;
    const float FadeDuration = 0.25f;
    bool isRevealOpen;

    readonly List<string> itemIds = new List<string>();
    readonly List<Image> buyButtonImages = new List<Image>();
    int selectedIndex = 0;

    System.Action onClose;
    bool isOpen;

    static readonly Color TextDark = new Color(0.16f, 0.16f, 0.18f, 1f);  // 明るい面（お金アイコン等）の上の文字
    static readonly Color TextLight = new Color(0.96f, 0.96f, 0.97f, 1f); // 透明黒の面の上の文字
    static readonly Color StatusColor = new Color(1f, 0.7f, 0.25f, 1f);
    static readonly Color CardColor = new Color(0.05f, 0.05f, 0.07f, 0.6f); // 透明黒

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("ShopUI");
        go.AddComponent<ShopUI>();
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

        font = Resources.Load<TMP_FontAsset>("GenEiKiwamiGo SDF");
        descFont = Resources.Load<TMP_FontAsset>("GenEiLateMinP_v2 SDF");
        roundedSprite = CreateRoundedRectSprite();
        leftRoundedSprite = CreateLeftRoundedRectSprite(48, 16);
        keyCapSprite = CreateRoundedRectSprite(32, 5);
        jihankiOnSprite = Resources.Load<Sprite>("UI/jihanki_on");
        jihankiOffSprite = Resources.Load<Sprite>("UI/jihanki_off");
        jihankiBgSprite = Resources.Load<Sprite>("UI/jihanki_bg");
        descUpperSprite = Resources.Load<Sprite>("UI/exp_bg_upper");
        descBottomSprite = Resources.Load<Sprite>("UI/exp_bg_bottom");
        glowSprite = CreateGlowSprite();

        buySE = Resources.Load<AudioClip>("Se/zihanki_output2");
        getSE = Resources.Load<AudioClip>("Se/get");
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        BuildUI();
        canvasRoot.SetActive(false);
    }

    void Update()
    {
        if (!isOpen) return;

        if (isRevealOpen)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                CloseReveal();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        if (itemIds.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            selectedIndex = Mathf.Min(selectedIndex + 1, itemIds.Count - 1);
            HighlightAndDescribe();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            selectedIndex = Mathf.Max(selectedIndex - 1, 0);
            HighlightAndDescribe();
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            BuyAt(selectedIndex);
        }
    }

    // InteractSystemから呼ばれる。ids: InteractableObject.shopItemIds（ItemDatabaseのitemId）
    public void Show(string[] ids, System.Action onCloseCallback)
    {
        onClose = onCloseCallback;
        selectedIndex = 0;
        statusText.text = "";
        isRevealOpen = false;

        // 前回開いた時の演出が中断されていた場合に備えて、表示状態をリセットしておく
        if (mainFadeCoroutine != null) { StopCoroutine(mainFadeCoroutine); mainFadeCoroutine = null; }
        if (revealFadeCoroutine != null) { StopCoroutine(revealFadeCoroutine); revealFadeCoroutine = null; }
        SetCanvasGroupVisible(mainUiGroup, true);
        SetCanvasGroupVisible(revealGroup, false);

        RefreshCards(ids);
        RefreshMoney();

        isOpen = true;
        canvasRoot.SetActive(true);
        SetFieldHudVisible(false);
    }

    static void SetCanvasGroupVisible(CanvasGroup group, bool visible)
    {
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    void Close()
    {
        isOpen = false;
        canvasRoot.SetActive(false);
        SetFieldHudVisible(true);

        var callback = onClose;
        onClose = null;
        callback?.Invoke();
    }

    // ミニマップ・時計などの通常のフィールドUI（CameraAimSystemが構え中に隠すのと同じCanvas）を
    // 自販機UIを開いている間だけ非表示にする
    CameraAimSystem cachedAimSystem;

    void SetFieldHudVisible(bool visible)
    {
        if (cachedAimSystem == null)
            cachedAimSystem = FindObjectOfType<CameraAimSystem>();

        if (cachedAimSystem != null && cachedAimSystem.uiRoot != null)
            cachedAimSystem.uiRoot.SetActive(visible);
    }

    void RefreshCards(string[] ids)
    {
        for (int i = cardsRow.childCount - 1; i >= 0; i--)
            Destroy(cardsRow.GetChild(i).gameObject);
        buyButtonImages.Clear();
        itemIds.Clear();

        var db = ItemDatabase.Instance;
        if (ids != null)
            foreach (var id in ids)
                if (db != null && db.Find(id) != null) itemIds.Add(id);

        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, itemIds.Count - 1));

        if (itemIds.Count == 0)
        {
            CreateText(cardsRow, "（うっているものがありません）", 15f, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(RowWidth, 28f), TextLight);
            RefreshDescription();
            return;
        }

        float colPitch = CardWidth + CardGap;

        // 購入ボタンをまとめて囲う白い角丸（3つ分をひとつの背景で覆う）
        var buyAreaBg = CreateImage(cardsRow, Color.white, new Vector2(0f, -82f), new Vector2(RowWidth, 70f));
        buyAreaBg.sprite = roundedSprite;
        buyAreaBg.type = Image.Type.Sliced;

        for (int i = 0; i < itemIds.Count; i++)
        {
            var def = db.Find(itemIds[i]);
            float x = (i - (itemIds.Count - 1) / 2f) * colPitch;

            // 商品ごとの位置決め用のスロット（背景は持たない。3本まとめて角丸フレーム側の背景を使う）
            var slotGO = new GameObject("DrinkSlot", typeof(RectTransform));
            var slot = (RectTransform)slotGO.transform;
            slot.SetParent(cardsRow, false);
            slot.anchorMin = slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = new Vector2(x, 0f);
            slot.sizeDelta = new Vector2(CardWidth, CardHeight);

            // 商品画像（未設定なら仮のうっすらした枠。あとでアイコンを用意すればそのまま表示される）
            var iconArea = CreateImage(slot, new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, 58f), new Vector2(130f, 108f));
            iconArea.sprite = roundedSprite;
            iconArea.type = Image.Type.Sliced;
            if (def.icon != null)
            {
                iconArea.color = Color.white; // 仮枠の薄いティントを外し、画像をそのままの濃さで前面に出す
                iconArea.sprite = def.icon;
                iconArea.type = Image.Type.Simple;
                iconArea.preserveAspect = true;
            }

            // 値段タグ：冷たい飲み物の自販機によくある「水色の中に白いタグ」の見た目
            var priceOuter = CreateImage(slot, new Color(6f / 255f, 127f / 255f, 215f / 255f, 1f), new Vector2(0f, -16f), new Vector2(120f, 30f));
            priceOuter.sprite = roundedSprite;
            priceOuter.type = Image.Type.Sliced;

            var priceInner = CreateImage(priceOuter.transform, Color.white, Vector2.zero, new Vector2(78f, 26f));
            priceInner.sprite = roundedSprite;
            priceInner.type = Image.Type.Sliced;

            CreateText(priceInner.transform, $"¥{def.price}", 17f, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(78f, 26f), TextDark);

            // 購入ボタン（背景は3つ共通のbuyAreaBgを使うので、ここではボタン画像だけ配置する）
            var buyImage = CreateImage(slot, Color.white, new Vector2(0f, -82f), new Vector2(110f, 64f));
            buyImage.sprite = jihankiOffSprite;
            buyImage.type = Image.Type.Simple;
            buyImage.preserveAspect = true;
            buyButtonImages.Add(buyImage);

            var buyButton = buyImage.gameObject.AddComponent<Button>();
            buyButton.targetGraphic = buyImage;
            int index = i;
            buyButton.onClick.AddListener(() => { selectedIndex = index; HighlightAndDescribe(); BuyAt(index); });
        }

        HighlightAndDescribe();
    }

    void HighlightAndDescribe()
    {
        // カードの背景は選択状態に関わらず一律。どれを選んでいるかは購入ボタンのon/offだけで示す
        for (int i = 0; i < buyButtonImages.Count; i++)
            buyButtonImages[i].sprite = (i == selectedIndex) ? jihankiOnSprite : jihankiOffSprite;

        RefreshDescription();
    }

    void RefreshDescription()
    {
        if (itemIds.Count == 0)
        {
            descNameText.text = "";
            descBodyText.text = "";
            descProductIcon.enabled = false;
            return;
        }

        var def = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(itemIds[selectedIndex]) : null;
        if (def == null) return;

        descNameText.text = def.displayName;
        descBodyText.text = def.description;

        descProductIcon.enabled = def.icon != null;
        descProductIcon.sprite = def.icon;
    }

    void BuyAt(int index)
    {
        if (isRevealOpen) return; // 入手演出を閉じるまでは連続購入させない
        if (index < 0 || index >= itemIds.Count) return;

        var db = ItemDatabase.Instance;
        var pa = PlayerAbilities.Instance;
        if (db == null || pa == null) return;

        var def = db.Find(itemIds[index]);
        if (def == null) return;

        if (!pa.SpendMoney(def.price))
        {
            statusText.text = "おかねが たりません";
            return;
        }

        pa.AddItem(def.itemId);
        RefreshMoney();

        if (audioSource != null && buySE != null) audioSource.PlayOneShot(buySE);
        OpenReveal(def);
    }

    // ---------- 入手演出 ----------

    void OpenReveal(ItemDefinition def)
    {
        isRevealOpen = true;

        revealIcon.enabled = def.icon != null;
        revealIcon.sprite = def.icon;

        revealText.text = $"{def.displayName}が でてきた！";

        if (glowCoroutine != null) StopCoroutine(glowCoroutine);
        glowCoroutine = StartCoroutine(PulseGlow());

        if (audioSource != null && getSE != null) audioSource.PlayOneShot(getSE);

        // 通常のショップUIをフェードアウトさせつつ、入手演出をフェードインさせる
        FadeGroup(mainUiGroup, 0f, ref mainFadeCoroutine);
        FadeGroup(revealGroup, 1f, ref revealFadeCoroutine);

        // 入手演出の間だけフィールドBGMを一時停止する
        if (MusicPlayer.Instance != null) MusicPlayer.Instance.FadeOutAndPause(FadeDuration);
    }

    void CloseReveal()
    {
        isRevealOpen = false;

        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
            glowCoroutine = null;
        }

        FadeGroup(revealGroup, 0f, ref revealFadeCoroutine);
        FadeGroup(mainUiGroup, 1f, ref mainFadeCoroutine);

        // フィールドBGMを途中から再開する
        if (MusicPlayer.Instance != null) MusicPlayer.Instance.FadeInAndResume(FadeDuration);
    }

    void FadeGroup(CanvasGroup group, float target, ref Coroutine coroutineRef)
    {
        if (coroutineRef != null) StopCoroutine(coroutineRef);
        coroutineRef = StartCoroutine(FadeCanvasGroupCo(group, target));
    }

    IEnumerator FadeCanvasGroupCo(CanvasGroup group, float target)
    {
        float start = group.alpha;
        float t = 0f;
        while (t < FadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, t / FadeDuration);
            yield return null;
        }

        group.alpha = target;
        bool visible = target > 0.5f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    IEnumerator PulseGlow()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float pulse = Mathf.Sin(t * 2.4f);
            revealGlow.rectTransform.localScale = Vector3.one * (1f + pulse * 0.08f);

            var c = revealGlow.color;
            c.a = 0.55f + pulse * 0.15f;
            revealGlow.color = c;

            yield return null;
        }
    }

    void RefreshMoney()
    {
        if (moneyText == null) return;
        var pa = PlayerAbilities.Instance;
        moneyText.text = pa != null ? $"{pa.money}" : "0";
    }

    // ---------- UI構築 ----------

    void BuildUI()
    {
        canvasRoot = new GameObject("ShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasRoot);

        var canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        var scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;

        const float DrinksPadding = 16f; // 角丸フレームの内側の余白
        float drinksFrameWidth = RowWidth + DrinksPadding * 2f;
        float drinksFrameHeight = CardHeight + DrinksPadding * 2f;

        // カード・説明帯・お金バッジ・キー案内をまとめるルート（入手演出中はこれごとフェードアウトする）
        var mainUiRoot = CreateRect(canvasRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        mainUiGroup = mainUiRoot.gameObject.AddComponent<CanvasGroup>();

        // 3本のドリンクをまとめて囲う角丸フレーム。背景はjihanki_bgをフレームいっぱいに引き伸ばして敷く
        var drinksFrame = CreateRect(mainUiRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-drinksFrameWidth / 2f, -(TopMargin + drinksFrameHeight)), new Vector2(drinksFrameWidth / 2f, -TopMargin));
        BuildMaskedBg(drinksFrame, jihankiBgSprite);

        // 商品カードの行（角丸フレームに重ねて表示する）
        cardsRow = CreateRect(drinksFrame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 購入結果などの一時的なメッセージ（カード行と説明帯の間）
        var statusRect = CreateRect(mainUiRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-RowWidth / 2f, -(TopMargin + drinksFrameHeight + DescGap / 2f) - 11f),
            new Vector2(RowWidth / 2f, -(TopMargin + drinksFrameHeight + DescGap / 2f) + 11f));
        statusText = statusRect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) statusText.font = font;
        statusText.fontSize = 14f;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = StatusColor;
        statusText.raycastTarget = false;

        // 説明帯（選択中の商品の名前と説明）：ひとつの角丸パネルの中で、上段の帯(exp_bg_upper)を
        // 下段(exp_bg_bottom)に重ねる。外枠のみ角丸で、上段帯の下側は分割せず地続きにする
        const float DescPadding = 20f;
        const float DescUpperHeight = 34f;
        const float DescIconSize = 40f; // 上段帯(34px)より大きくして少しはみ出させる
        float descContentWidth = RowWidth - DescPadding * 2f;

        var descBand = CreateRect(mainUiRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-RowWidth / 2f, -(TopMargin + drinksFrameHeight + DescGap + DescHeight)),
            new Vector2(RowWidth / 2f, -(TopMargin + drinksFrameHeight + DescGap)));

        var descMaskImage = descBand.gameObject.AddComponent<Image>();
        descMaskImage.sprite = roundedSprite;
        descMaskImage.type = Image.Type.Sliced;
        var descMask = descBand.gameObject.AddComponent<Mask>();
        descMask.showMaskGraphic = false; // 外枠の角丸切り抜き専用。自身は描画しない

        // 下段：帯いっぱいに敷く
        var descBottomRect = CreateRect(descBand, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var descBottomImage = descBottomRect.gameObject.AddComponent<Image>();
        descBottomImage.sprite = descBottomSprite;
        descBottomImage.type = Image.Type.Simple;
        descBottomImage.preserveAspect = false;

        // 上段：上端に固定した帯として下段の上に重ねる（下側は角丸にせず、そのまま下段へ続ける）
        var descTopRect = CreateRect(descBand, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -DescUpperHeight), Vector2.zero);
        var descTopImage = descTopRect.gameObject.AddComponent<Image>();
        descTopImage.sprite = descUpperSprite;
        descTopImage.type = Image.Type.Simple;
        descTopImage.preserveAspect = false;

        // 商品画像（商品名の左に表示。上段の帯より一回り大きくして少しはみ出させる）
        float descIconX = -RowWidth / 2f + DescPadding + DescIconSize / 2f;
        float descNameTextLeft = descIconX + DescIconSize / 2f + 8f;
        float descNameTextRight = RowWidth / 2f - DescPadding;
        float descNameTextWidth = descNameTextRight - descNameTextLeft;
        float descNameTextCenterX = (descNameTextLeft + descNameTextRight) / 2f;
        float descTopCenterY = DescHeight / 2f - DescUpperHeight / 2f; // 上段帯の中心Y（descBand中心基準）

        // descBandはMaskで角丸に切り抜かれているため、はみ出しを見せるにはdescBandの外
        // （mainUiRoot直下、descBandより後＝手前に描画）にアイコンを置く
        float descBandTopOffset = -(TopMargin + drinksFrameHeight + DescGap);
        float descIconCenterYFromTop = descBandTopOffset - DescUpperHeight / 2f;
        var descIconRect = CreateRect(mainUiRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(descIconX - DescIconSize / 2f, descIconCenterYFromTop - DescIconSize / 2f),
            new Vector2(descIconX + DescIconSize / 2f, descIconCenterYFromTop + DescIconSize / 2f));
        descProductIcon = descIconRect.gameObject.AddComponent<Image>();
        descProductIcon.preserveAspect = true;

        descNameText = CreateText(descBand, "", 19f, TextAlignmentOptions.MidlineLeft,
            new Vector2(descNameTextCenterX, descTopCenterY), new Vector2(descNameTextWidth, DescUpperHeight), TextLight);
        if (descFont != null) descNameText.font = descFont;

        // exp_bg_bottomは明るい背景なので、暗い文字色を使う
        float descBodyCenterY = -DescUpperHeight / 2f; // 下段領域の中心Y（descBand中心基準）
        descBodyText = CreateText(descBand, "", 14f, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, descBodyCenterY), new Vector2(descContentWidth, DescHeight - DescUpperHeight - 12f), TextDark);
        if (descFont != null) descBodyText.font = descFont;
        descBodyText.enableWordWrapping = true;

        BuildMoneyBadge(mainUiRoot);
        BuildHintBanner(mainUiRoot);
        BuildRevealPanel(canvasRoot.transform);
    }

    void BuildRevealPanel(Transform parent)
    {
        var revealRect = CreateRect(parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        revealPanel = revealRect.gameObject;
        revealGroup = revealPanel.AddComponent<CanvasGroup>();

        // 入手演出中は画面全体を少し暗くして、アイテムに視線を集める
        var dim = revealPanel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.45f);
        dim.raycastTarget = false;

        revealGlow = CreateImage(revealPanel.transform, new Color(1f, 0.85f, 0.4f, 0.6f), new Vector2(0f, 40f), new Vector2(220f, 220f));
        revealGlow.sprite = glowSprite;
        revealGlow.raycastTarget = false;

        revealIcon = CreateImage(revealPanel.transform, Color.white, new Vector2(0f, 40f), new Vector2(140f, 140f));
        revealIcon.preserveAspect = true;
        revealIcon.raycastTarget = false;

        var textFrame = CreateImage(revealPanel.transform, CardColor, new Vector2(0f, -110f), new Vector2(300f, 56f));
        textFrame.sprite = roundedSprite;
        textFrame.type = Image.Type.Sliced;

        revealText = CreateText(textFrame.transform, "", 18f, TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(280f, 44f), TextLight);

        var closeHint = CreateRect(revealPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-65f, 40f), new Vector2(65f, 66f));
        CreateKeyCap(closeHint, "Tab", new Vector2(-36f, 0f), 44f);
        CreateText(closeHint, "とじる", 13f, TextAlignmentOptions.MidlineLeft, new Vector2(24f, 0f), new Vector2(72f, 22f), TextLight);

        SetCanvasGroupVisible(revealGroup, false);
    }

    // 角丸の切り抜き（Mask）を作り、その中いっぱいに画像を敷く。写真アセットを角丸パネルとして使うための共通処理
    void BuildMaskedBg(RectTransform container, Sprite bgSprite)
    {
        var maskImage = container.gameObject.AddComponent<Image>();
        maskImage.sprite = roundedSprite;
        maskImage.type = Image.Type.Sliced;
        var mask = container.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false; // 自身は描画せず、角丸の切り抜き形状としてだけ使う

        var bgRect = CreateRect(container, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var bgImage = bgRect.gameObject.AddComponent<Image>();
        bgImage.sprite = bgSprite;
        bgImage.type = Image.Type.Simple;
        bgImage.preserveAspect = false; // アスペクト比は無視して枠いっぱいに引き伸ばす
    }

    void BuildMoneyBadge(Transform parent)
    {
        const float BadgeWidth = 150f;
        const float BadgeHeight = 32f;
        const float IconSize = 26f;
        const float BleedPastEdge = 10f;
        const float TopMargin2 = 16f;

        var shadow = CreateRect(parent, Vector2.one, Vector2.one,
            new Vector2(BleedPastEdge - BadgeWidth - 3f, -TopMargin2 - BadgeHeight - 3f),
            new Vector2(BleedPastEdge - 3f, -TopMargin2 - 3f));
        var shadowImage = shadow.gameObject.AddComponent<Image>();
        shadowImage.color = new Color(0f, 0f, 0f, 0.2f);
        shadowImage.sprite = leftRoundedSprite;
        shadowImage.type = Image.Type.Sliced;

        var badge = CreateRect(parent, Vector2.one, Vector2.one,
            new Vector2(BleedPastEdge - BadgeWidth, -TopMargin2 - BadgeHeight),
            new Vector2(BleedPastEdge, -TopMargin2));
        var badgeImage = badge.gameObject.AddComponent<Image>();
        badgeImage.color = new Color(0.3f, 0.3f, 0.33f, 0.92f);
        badgeImage.sprite = leftRoundedSprite;
        badgeImage.type = Image.Type.Sliced;

        var iconBadge = CreateImage(badge, new Color(0.95f, 0.78f, 0.3f, 1f), new Vector2(-BadgeWidth / 2f + 20f, 0f), new Vector2(IconSize, IconSize));
        iconBadge.sprite = CreateRoundedRectSprite(32, 16); // 円に近い角丸バッジ
        CreateText(iconBadge.transform, "¥", 14f, TextAlignmentOptions.Center, Vector2.zero, new Vector2(IconSize, IconSize), TextDark);

        moneyText = CreateText(badge, "0", 17f, TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(70f, BadgeHeight), Color.white);
    }

    void BuildHintBanner(Transform parent)
    {
        const float Margin = 18f;
        const float Height = 30f;
        const float Padding = 12f;
        const float Gap = 4f;
        const float GroupGap = 14f;

        const float w1 = 44f; // [Tab]
        const float w2 = 54f; // もどる
        const float w3 = 24f; // [←]
        const float w4 = 24f; // [→]
        const float w5 = 68f; // せんたく

        const float totalWidth = Padding * 2f + w1 + Gap + w2 + GroupGap + w3 + Gap + w4 + Gap + w5;

        var hintContainer = CreateRect(parent, Vector2.right, Vector2.right,
            new Vector2(-Margin - totalWidth, Margin), new Vector2(-Margin, Margin + Height));
        var hintBg = hintContainer.gameObject.AddComponent<Image>();
        hintBg.color = CardColor;
        hintBg.sprite = roundedSprite;
        hintBg.type = Image.Type.Sliced;

        float cx = -totalWidth / 2f + Padding;

        CreateKeyCap(hintContainer, "Tab", new Vector2(cx + w1 / 2f, 0f), w1);
        cx += w1 + Gap;
        CreateText(hintContainer, "もどる", 13f, TextAlignmentOptions.MidlineLeft, new Vector2(cx + w2 / 2f, 0f), new Vector2(w2, 22f), TextLight);
        cx += w2 + GroupGap;
        CreateKeyCap(hintContainer, "←", new Vector2(cx + w3 / 2f, 0f), w3);
        cx += w3 + Gap;
        CreateKeyCap(hintContainer, "→", new Vector2(cx + w4 / 2f, 0f), w4);
        cx += w4 + Gap;
        CreateText(hintContainer, "せんたく", 13f, TextAlignmentOptions.MidlineLeft, new Vector2(cx + w5 / 2f, 0f), new Vector2(w5, 22f), TextLight);
    }

    // 既存の常駐UIのキーバッジ（音楽プレイヤーの「N」等）と同じデザイン：
    // 控えめな角丸四角（角の丸みは小さめ）＋縁取りなし・うっすら塗り＋中央のラベルだけのミニマルな見た目
    void CreateKeyCap(Transform parent, string label, Vector2 anchoredPos, float width)
    {
        var badge = CreateImage(parent, new Color(1f, 1f, 1f, 0.08f), anchoredPos, new Vector2(width, 20f));
        badge.sprite = keyCapSprite;
        badge.type = Image.Type.Sliced;

        CreateText(badge.transform, label, 12f, TextAlignmentOptions.Center, Vector2.zero, new Vector2(width, 20f), TextLight);
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

    Sprite CreateRoundedRectSprite(int size = 64, int radius = 16)
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
                float alpha = Mathf.Clamp01((radius - dist) / 1.25f + 0.5f);
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
                float dx = px < radius ? Mathf.Max(0f, radius - px) : 0f;
                float dy = px < radius ? Mathf.Max(0f, Mathf.Max(radius - py, py - (size - radius))) : 0f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((radius - dist) / 1.25f + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    // 中心から外側へなめらかに薄くなる光彩スプライト。入手演出のアイテム背後に使う
    Sprite CreateGlowSprite(int size = 128)
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
                float t = Mathf.Clamp01(1f - dist / r);
                float alpha = t * t;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }
}
