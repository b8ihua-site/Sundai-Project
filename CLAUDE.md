# CLAUDE.md

このファイルは、Claude Code がこの Unity プロジェクトを継続的に開発する際に、毎回プロジェクト全体を調査しなくても済むよう、既存の構造・設計・重要な仕様をまとめたものです。

> **表記ルール**: 「確認済み」= 実際にファイルを読んで確認した事実。「推測」= ファイル名・命名・一般的な構成から推測した内容（明示的にそう記載）。区別のない記述はすべて確認済みの事実です。

---

## 1. Unityバージョン / レンダーパイプライン

- Unity Editor: **6000.3.3f1**（`ProjectSettings/ProjectVersion.txt`）
- レンダーパイプライン: **URP (Universal Render Pipeline) 17.3.0**
  - `Packages/manifest.json` に `com.unity.render-pipelines.universal: 17.3.0`
  - `ProjectSettings/GraphicsSettings.asset` の `m_CustomRenderPipeline` が URP のアセット（guid: `4b83569d67af61e458304325a23e5dfd`）を指している
  - `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` が存在

## 2. 使用している主要パッケージ（`Packages/manifest.json` 確認済み）

| パッケージ | バージョン |
|---|---|
| com.cesium.unity | 1.23.3（スコープ付きレジストリ `unity.pkg.cesium.com` 経由） |
| com.unity.render-pipelines.universal | 17.3.0 |
| com.unity.inputsystem | 1.17.0 |
| com.unity.cinemachine | 3.1.6 |
| com.unity.ai.navigation | 2.0.9 |
| com.unity.timeline | 1.8.10 |
| com.unity.visualscripting | 1.9.9 |
| com.unity.collections | 2.6.2 |
| com.vrmc.vrm | GitHub参照: `vrm-c/UniVRM` `#v0.131.0`（VRM10パッケージ） |

さらに、UPM のローカル/git パッケージとして `Packages/com.vrmc.gltf`、`Packages/com.vrmc.univrm` が存在（UniVRM本体の依存パッケージ群）。
TextMesh Pro は `Assets/TextMesh Pro`（TMP Essentials）としてプロジェクトに展開済み。

## 3. Scene構成とScene遷移

### ビルドに含まれるScene（`ProjectSettings/EditorBuildSettings.asset` 確認済み、上から順）

1. `Assets/Scenes/SampleScene.unity`（Unity標準テンプレートのデフォルトScene。ゲーム内では未使用と思われる＝**推測**）
2. `Assets/LogoScene.unity`
3. `Assets/TitleScene.unity`
4. `Assets/mainscene.unity` ← **ファイル名は小文字**
5. `Assets/BattleScene.unity`

これ以外にも `Assets/_Recovery/0.unity` というScene（後述）や、アセットストア素材付属のサンプルScene（Starter Assets, SkySeries Freebie, Anime Trees, Mellow Fox studios, Tree9 の Playground/Demo系）が多数存在するが、いずれもビルド設定には含まれていない。

### Scene遷移ロジック（`SceneManager.LoadScene`/`LoadSceneAsync` を grep して確認済み）

- **LogoScene → TitleScene**: `LogoFade.cs`（`LogoSequence`）でフェード演出のみ。Scene遷移自体はこのファイルには無い（別トリガー、または `BuildSettings` の連番自動遷移用スクリプトが別途ある可能性。深追いしていないため断定はしない）。
- **TitleScene → mainscene**: `Assets/Scripts/SceneLoader.cs:61` — `SceneManager.LoadSceneAsync(mainSceneName)`。`mainSceneName` のデフォルト値は `"mainscene"`（`SceneLoader.cs:23`、**小文字で正しい**）。「Press Any Key」入力をトリガーに非同期ロード。
- **mainscene → BattleScene**（探索中に敵と接触）: `Assets/Scripts/BattleSystem/EnemyEncounter.cs:40` — `battleSceneName` のデフォルトは `"BattleScene"`（正しい）。プレイヤーとの距離が `encounterRange` 以内になったら自動で戦闘開始。
- **mainscene → BattleScene**（インタラクトで「たたかう」選択）: `Assets/Scripts/BattleChoiceUI.cs:64` — `battleSceneName` のデフォルトは `"BattleScene"`（正しい）。`InteractSystem.cs` 経由でFキー押下→`InteractableObject.isEnemy` がtrueなら選択肢UIを表示。
- **BattleScene → mainscene**（勝利/敗北/逃走）: `Assets/Scripts/BattleSystem/BattleManager.cs:236` `ReturnToMainScene()` — `SceneManager.LoadScene(mainSceneName)`。

### ⚠️ 既知の未修正バグ（今回は修正しない）

`Assets/Scripts/BattleSystem/BattleManager.cs:39` の `mainSceneName` フィールドの**デフォルト値が `"MainScene"`**（大文字M・大文字S）になっている。

```csharp
[Header("シーン")]
public string mainSceneName = "MainScene";
```

一方、実際のScene名（ファイル名）は `Assets/mainscene.unity`（すべて小文字）であり、`EditorBuildSettings.asset` にも `path: Assets/mainscene.unity` として登録されている。`SceneLoader.cs` 側の同名フィールドは `"mainscene"`（正しい値）になっており、**BattleManager.cs だけ大文字小文字が食い違っている**。

- Windows/Editor上では大文字小文字を区別しない場合があり気づきにくいが、大文字小文字を区別する環境（Linux版ビルド等）では `SceneManager.LoadScene("MainScene")` がシーンを見つけられず遷移に失敗する可能性がある。
- ただし、Inspector上でこの値が `"mainscene"` に上書きされている場合は実害が出ない。BattleScene内のBattleManagerコンポーネントのInspector値を実機で確認しないと実際に問題が発生しているかは断定できない（**この一文は推測を含む**）。
- **今回のタスクではこの問題は修正しない。** 今後修正する際は、BattleScene.unity内のBattleManagerコンポーネントのInspector設定値（シリアライズされた値）を先に確認し、スクリプトのデフォルト値だけでなくScene側のオーバーライド値も合わせて直すこと。

## 4. 主要C#スクリプトと役割

スクリプトは大きく2箇所に分かれている（**命名規則の使い分けが明確でない点に注意**）:

- `Assets/Scripts/`（ロゴ演出・シーン遷移・戦闘システム本体）
- `Assets/` 直下（探索・インタラクト・UI・カメラ演出などの雑多なスクリプト群）

### `Assets/Scripts/`（ロゴ・遷移系）

| ファイル | クラス | 役割 |
|---|---|---|
| `LogoFade.cs` | `LogoSequence`（**ファイル名とクラス名不一致**） | ロゴのフェードイン→維持→フェードアウト（CanvasGroup操作） |
| `LogoBGFade.cs` | `WhiteBackgroundFade`（**ファイル名とクラス名不一致**） | ロゴ背景の白フェード |
| `SceneFadeIn.cs` | `SceneFadeIn` | Scene開始時に白パネルをフェードアウトさせる汎用演出 |
| `SceneLoader.cs` | `SceneLoader` | TitleScene→mainscene の非同期ロード、ローディングバー・BGMフェード制御。`mainSceneName = "mainscene"`（正しい） |
| `ImageBreathing.cs` | `ImageBreathing` | 背景画像の明滅・微揺れ演出（Perlin Noise） |

### `Assets/Scripts/BattleSystem/`（戦闘システム本体。詳細は5章）

`BattleManager.cs`, `BattleUI.cs`, `BattleContext.cs`, `EnemyEncounter.cs`, `QuizDatabase.cs`, `LetterGrid.cs`, `SEManager.cs`, `Shaker.cs`, `PopupSpawner.cs`, `DamagePopup.cs`

### `Assets/Scripts/` 直下その他

- `BattleChoiceUI.cs` — 探索中に敵NPCへインタラクトした際の「たたかう/やめる」選択パネル（`BattleSystem`フォルダの外に置かれている点に注意）

### `Assets/` 直下（探索・インタラクト・UI系。**`Assets/Scripts/` には無い**）

| ファイル | クラス | 役割 |
|---|---|---|
| `InteractSystem.cs` | `InteractSystem` | 探索中のメインのインタラクトシステム。Rayでインタラクト対象を検出、Fキーで実行。NPC会話/読み物/敵エンカウント選択を分岐処理。`StarterAssets.ThirdPersonController` の `movementLocked`/`LockCameraPosition` を操作してプレイヤーを一時的にロックする |
| `InteractableObject.cs` | `InteractableObject` | インタラクト可能オブジェクトが持つデータ（読み物ページ、NPC情報、敵エンカウント情報）。`InteractSystem`/`BattleChoiceUI`が参照 |
| `InspectUI.cs` | `InspectUI` | 読み物・NPC会話のテキスト表示UI（タイプライター演出、話者名、ボイス再生、ページ送り） |
| `PlayerInteract.cs` | `PlayerInteract` | **`InteractSystem`とは別系統の、より単純なインタラクト実装**（`Interactable`クラスを参照）。用途重複の可能性あり（8章参照） |
| `Exp.cs` | `Interactable`（**ファイル名とクラス名不一致**） | `PlayerInteract`用の単純なメッセージ表示コンポーネント |
| `HPBar.cs` | `HPBar` | HP増減アニメーション付きゲージ（遅延バー演出）。`DamageCube`や`BattleUI`から参照 |
| `DamageCube.cs` | `DamageCube` | トリガー内に居るPlayerに継続ダメージを与えるオブジェクト（`HPBar`と連携） |
| `AreaNameTrigger.cs` | `AreaNameTrigger` | エリア進入時に地名を表示するトリガー |
| `ClockSystem.cs` | `ClockSystem` | 現実時刻（`DateTime.Now`）を表示する時計UI |
| `MiniMapScroll.cs` / `MinimapIcon.cs` / `Arrow.cs` / `CompassBar.cs` / `CameraRangeRotator.cs` | 各種 | ミニマップ・コンパスUI一式（プレイヤー追従、アイコン表示、方位表示） |
| `MusicPlayer.cs` | `MusicPlayer` | BGMプレイヤー本体。**シングルトン + `DontDestroyOnLoad`**（Scene跨ぎで永続化） |
| `MusicPlayerUI.cs` | `MusicPlayerUI` | BGMプレイヤーのUI操作（シングルトン） |
| `camerashaking.cs` | `HandheldShake`（**ファイル名とクラス名不一致**） | カメラの手持ち風ノイズ揺れ |
| `camerabreathing.cs` | `CinematicBreathing`（**ファイル名とクラス名不一致**） | URP Volume（露出・被写界深度）をゆっくり揺らす演出 |

### `Assets/Scripts/` 直下（探索カメラ関連）

- `SimpleFPS.cs` — 独自実装のFPS風視点+Rigidbody移動（Starter Assetsとは別の独自コントローラー。使用箇所は要確認＝**推測混じり**）
- `MinimapFollow.cs` — ミニマップカメラのプレイヤー追従
- `CameraZoom.cs` — マウスホイールでCinemachine 3rd Person Followの距離を調整
- `NPCLookAt.cs` — NPCがプレイヤーの方を向く

**セーブシステム / インベントリ / GameManager的な統括シングルトンは、今回grepした範囲では見つからなかった。** `BattleContext`（static純データクラス）が唯一のScene間データ受け渡し役。

## 5. バトルシステムの構造

`Assets/Scripts/BattleSystem/BattleManager.cs` が中心。**ターン制**、`BattleState` enumで状態管理:

```csharp
public enum BattleState { Intro, Command, Question, Answering, PlayerAttack, EnemyTurn, Win, Lose }
```

### フロー

1. **Intro**: 敵出現演出（`enemyName が あらわれた！`）→ `EnterCommand()`
2. **Command**: 「たたかう/どうぐ/にげる」を選択（`BattleUI`のボタン経由）
   - たたかう → `QuestionSequence()`
   - どうぐ → 未実装（プレースホルダーメッセージのみ）
   - にげる → 常に成功してmainsceneへ戻る（仮実装、コード内コメントに明記あり）
3. **Question / Answering**: `QuizDatabase`（ScriptableObject、`subject`名で問題セットを引く）からランダムに問題を取得し、`LetterGrid`（3×3のひらがなグリッド、テンキー1〜9または画面クリックで正解文字を選ぶタイピングゲーム）で回答。コンボ数と制限時間で成否が決まる。
4. **PlayerAttack**: 正解率（combo/文字数）に応じたダメージを敵に与える。`PopupSpawner`でダメージポップアップ、`Shaker`で敵オブジェクトを揺らす。
5. **EnemyTurn**: 敵が固定ダメージ（±variance）でプレイヤーを攻撃。`Shaker`でカメラを揺らす。
6. HP0以下で `WinSequence`/`LoseSequence` へ。いずれも `ReturnToMainScene()` でmainsceneへ戻る（**敗北時の扱いは「要相談」とコード内コメントに明記されており未確定仕様**）。

### 補助クラス

- `BattleContext`（static） — mainscene側（`EnemyEncounter`/`BattleChoiceUI`）からBattleSceneへ渡す敵情報（名前・HP・攻撃力・科目）
- `SEManager`（シングルトン） — SE再生。`PlayScaleNote`でコンボ数に応じてドレミの音階を鳴らす仕組みあり
- `PopupSpawner`（シングルトン） — ダメージ/コンボのポップアップ生成
- `Shaker`（シングルトン） — Transformを揺らす汎用コルーチン
- `BattleUI` — HPバー・コマンドパネル・メッセージ表示のUIラッパー（MonoBehaviour、シングルトンではない）

### UIとの連携

`BattleManager` → `BattleUI`（メッセージ/HP表示）、`LetterGrid`（回答UI）を直接参照（Inspectorでアサイン）。`LetterGrid.FadeOutAndFinish()` の最後で `BattleManager.Instance.OnAnswerFinished(combo, ...)` を呼び返す双方向連携。

## 6. 探索・インタラクションシステム

- プレイヤー移動は **Unity公式 Starter Assets の `ThirdPersonController`**（`Assets/Starter Assets/Runtime/ThirdPersonController/Scripts/ThirdPersonController.cs`）を**直接改造**（サブクラス化ではなくオリジナルファイルに手を入れている）。追加されたフィールド:
  - `[HideInInspector] public bool movementLocked`（`ThirdPersonController.cs:52`) — インタラクト/会話/戦闘選択中に移動を止めるためのフラグ
  - `public bool LockCameraPosition`（`ThirdPersonController.cs:77`) — 同様に視点固定用（本来Starter Assetsに元々ある機能を流用）
  - これらは `Assets/InteractSystem.cs` の `LockPlayer()` から操作される。
- 一方 `Assets/SimpleFPS.cs` という**独自のFPS風コントローラーも別途存在**する。どちらがどのSceneで実際に使われているかは、今回のコード調査（grep/read）だけでは断定できない（**推測**：ファイル名的にFPS風は別モード/未使用テスト実装の可能性）。
- インタラクトは**2系統が並存**している（8章「注意点」参照）:
  - `InteractSystem` + `InteractableObject`（Ray検出、F キー、NPC会話/読み物/戦闘導線を持つ、より作り込まれた方）
  - `PlayerInteract` + `Interactable`（Ray検出、F キー、単純なメッセージ表示のみ）
- NPC会話・読み物は `InspectUI` がページ送り式のタイプライターUIを担当（音声再生対応）。
- 敵との会話/戦闘導線は `InteractableObject.isEnemy` フラグで分岐し、`BattleChoiceUI` の選択パネル（たたかう/やめる）を経由してBattleSceneへ遷移。
- 自動エンカウント（選択肢なし即戦闘）は `EnemyEncounter.cs` が別途用意されており、プレイヤーとの距離だけで発火する。

## 7. 重要なPrefab

`Assets/Prefabs/` 配下（プロジェクト独自のPrefabはここに集約されている数少ない場所）:

- `HPBar.prefab` — `HPBar.cs` を持つHPゲージUI
- `DamagePopup.prefab` — `DamagePopup.cs` を持つダメージ数値ポップアップ（`PopupSpawner`が`Instantiate`）
- `PlaylistItem.prefab` — `MusicPlayerUI`のプレイリスト項目

`Assets/Scripts/BattleSystem/Prefabs/CellButton.prefab` — `LetterGrid`が生成する9マスのひらがなセル（`cellPrefab`）。

その他、Player/Enemy/NPC用のプレイヤブルPrefabは `Assets/Prefabs/` には見当たらない。Starter Assets由来のプレイヤーPrefab（`Assets/Starter Assets/Runtime/ThirdPersonController/Prefabs/PlayerArmature.prefab`, `PlayerCapsule.prefab`）が実質的なプレイヤーPrefabと思われる（**推測**、Sceneでの実際の使用確認はしていない）。VRMアバター（`AvatarSample_M.vrm` / `AvatarSample_P.vrm`）は `Assets/mainscene.unity` 内に直接PrefabInstanceとして配置されていることをgrepで確認済み（プレイヤー用かNPC用かは今回未確認）。

## 8. 重要なシングルトン・staticデータ

`public static X Instance` パターンまたは `DontDestroyOnLoad` を確認したクラス:

| クラス | ファイル | 備考 |
|---|---|---|
| `BattleManager` | `Assets/Scripts/BattleSystem/BattleManager.cs:7` | BattleScene内でのみ生存（`DontDestroyOnLoad`なし） |
| `SEManager` | `Assets/Scripts/BattleSystem/SEManager.cs:6` | 同上 |
| `PopupSpawner` | `Assets/Scripts/BattleSystem/PopupSpawner.cs:5` | 同上 |
| `Shaker` | `Assets/Scripts/BattleSystem/Shaker.cs:6` | 同上 |
| `BattleChoiceUI` | `Assets/Scripts/BattleChoiceUI.cs:9` | 同上（mainscene内） |
| `MusicPlayer` | `Assets/MusicPlayer.cs:6, 26` | **`DontDestroyOnLoad`あり**。Scene跨ぎで唯一永続化するオブジェクト |
| `MusicPlayerUI` | `Assets/MusicPlayerUI.cs:7` | `DontDestroyOnLoad`なし（Sceneごとに再生成される想定と思われる＝推測） |
| `BattleContext` | `Assets/Scripts/BattleSystem/BattleContext.cs` | シングルトンではなく**static純データクラス**（Instanceパターンではない）。mainscene→BattleSceneへの唯一のデータ受け渡し経路 |

`BattleManager`/`SEManager`等はDontDestroyOnLoadしていないため、BattleSceneがロードされるたびに新しいInstanceが作られる設計（意図的と思われる＝推測）。

## 9. Starter Assetsとの関係

- `Assets/Starter Assets/` は Unity公式アセット一式（ThirdPersonController, FirstPersonController, Mobile入力, 環境Prefab等）がほぼそのまま入っている。
- **例外**: `ThirdPersonController.cs` 本体に `movementLocked` / `LockCameraPosition` 関連の改造が直接入っている（6章参照）。サブクラスや拡張コンポーネントによる差分ではなく、**原本ファイルの直接編集**。今後Starter Assetsパッケージを再インポート・アップデートする場合はこの改造が失われる点に注意。
- `FirstPersonController.cs` 側は今回改造の有無を確認していない。
- `Assets/Starter Assets/Sample/` 配下はUnity公式のサンプルScene（Playground等）で、ゲーム本編とは無関係と思われる（**推測**）。

## 10. Input Systemの現状

- `com.unity.inputsystem: 1.17.0` が導入済み（`Packages/manifest.json`）。
- **新Input System（Starter Assets経由）と旧Input Manager（`Input.GetKey`/`Input.GetAxis`）が混在している**:
  - 新方式: `StarterAssetsInputs.cs` が `PlayerInput` コンポーネントからの `OnMove`/`OnLook`/`OnJump`/`OnSprint` メッセージ（`#if ENABLE_INPUT_SYSTEM` ガード付き）を受けてプレイヤー移動を制御。アクションアセットは `Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions`。
  - 旧方式（`Input.GetKeyDown`/`Input.GetAxis`を直接呼んでいる箇所、grep確認済み）: `LetterGrid.cs`（テンキー入力）, `InteractSystem.cs`（Fキー）, `PlayerInteract.cs`（Fキー）, `SimpleFPS.cs`（マウス/WASD）, `SceneLoader.cs`（`Input.anyKeyDown`）, `MusicPlayerUI.cs`（M/N/Bキー）
- `Assets/InputSystem_Actions.inputactions` という**プロジェクトルート直下の別のアクションアセット**も存在する（Unity新規プロジェクト作成時に自動生成されるデフォルトのProject-wide Input Actions。Starter Assets付属の`StarterAssets.inputactions`とは別物で、実際に使われているかは未確認＝**推測混じり**）。
- `ProjectSettings/Packages` 内に `com.unity.input.settings.actions` の設定があり、`EditorBuildSettings.asset` の `m_configObjects` からも参照されている。

## 11. Cesium / VRMの現状

- **Cesium**: `com.cesium.unity 1.23.3` を導入済み、`Assets/CesiumSettings/`（`CesiumRuntimeSettings.asset`, `CesiumIonServers/ion.cesium.com.asset`）も設定済み。**しかし、プロジェクト内5つのゲームScene（SampleScene, LogoScene, TitleScene, mainscene, BattleScene）のいずれにも `Cesium3DTileset` や `CesiumGeoreference` 等のCesiumコンポーネントは配置されていないことをgrepで確認済み**。また、CesiumのAPIを呼び出しているスクリプトも `Assets/**/*.cs` には見つからなかった。→ **Cesiumはパッケージ導入・設定のみで、実際のゲーム内では未使用の状態**。
- **VRM**: `com.vrmc.vrm`（UniVRM 10, v0.131.0）導入済み。`Assets/Character/AvatarSample_M.vrm`, `AvatarSample_P.vrm` の2体のサンプルアバターが存在し、**`AvatarSample_P` は `Assets/mainscene.unity` 内にPrefabInstanceとして実際に配置されていることを確認済み**（GUID一致で確認）。ただしスクリプト側でVRM10のランタイムAPI（`Vrm10Instance`等）を呼んでいる箇所は見つからず、**エディタ上で静的に配置されたモデルとして使われている**（実行時ロードではない）と考えられる（**推測**）。
- `Assets/_Recovery/0.unity` にも同じVRM GUIDへの参照がある（12章参照）。

## 12. `Assets/_Recovery` フォルダ

`0.unity` という1つのSceneファイルのみが入っている。ファイル名・配置（アンダースコア始まりでソート先頭に来る）から、**Unityのクラッシュ時オートリカバリ機能（Auto Save/Crash Recovery）によって生成された復旧用Sceneである可能性が高い（推測）**。ビルド設定には含まれておらず、他のScene/スクリプトから参照されている形跡もない。内容の詳細（本当に復旧データか、意図的な作業用Sceneか）は未確認。**削除するかどうかはユーザーに確認してから判断すること。中身を精査せず不要と決めつけない。**

## 13. その他、確認された注意点・違和感

1. **ファイル名とクラス名の不一致が多数ある**（Unityでは動作上問題ないが、検索・保守性に影響する）:
   - `Exp.cs` → `class Interactable`
   - `LogoFade.cs` → `class LogoSequence`
   - `LogoBGFade.cs` → `class WhiteBackgroundFade`
   - `camerashaking.cs` → `class HandheldShake`
   - `camerabreathing.cs` → `class CinematicBreathing`
   - スクリプトを探す際はクラス名でのgrepを優先すること。
2. **インタラクトシステムが2系統並存**（`InteractSystem`+`InteractableObject` と `PlayerInteract`+`Interactable`）。役割が重複しているように見えるが、実際にどちらがどのScene/オブジェクトで使われているかは要確認。**どちらかが未使用の古い実装である可能性があるが、断定せず、削除・統合前に必ず参照元（Scene内のコンポーネントアタッチ状況）を確認すること。**
3. **BattleManager.cs の `mainSceneName` デフォルト値の大文字小文字バグ**（3章に既知の問題として記載、今回未修正）。
4. コード中に日本語のTODOコメントが複数存在（例: `BattleManager.cs:235` `// TODO: ここでプレイヤーHPなどを持ち帰る処理`、`BattleManager.cs:230` の敗北時処理は「要相談」との記載）。**これらは仕様未確定の意図的なプレースホルダーであり、放置されているバグではない。** 実装時は既存のTODOコメントの文脈を尊重すること。
5. `Assets/Scenes/SampleScene.unity`（Unity標準テンプレートのデフォルトScene）がビルド設定の先頭に残っている。ゲーム本編で使われている形跡はない（**推測**）。
6. `Assets/InputSystem_Actions.inputactions`（プロジェクトルート直下）と `Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions` の2つのアクションアセットが併存しており、どちらが実際に使われているか要確認。
7. アセットストア系の素材（`Anime Trees`, `SkySeries Freebie`, `Mellow Fox studios`, `Stylize Water Texture`, `Tree9`）には付属のデモScene・サンプルが多数含まれるが、いずれもビルド設定には含まれておらず、ゲーム本編とは無関係と思われる（**推測**）。安易に削除しない（アセット自体はマテリアル/プレハブとしてゲーム内Sceneから参照されている可能性があるため、削除前に参照元を確認すること）。

---

## 今後コードを変更するときの注意事項

1. **事実と推測を区別する**: 本ドキュメントに書かれていない挙動を扱う場合は、必ず実際のファイル（Scene, Prefab, Script, ScriptableObject）を確認してから判断する。本ドキュメントの「推測」ラベルが付いた記述は鵜呑みにせず、実装前に再確認する。
2. **既存機能を変更・削除する前に、関連コードと参照元を確認する**: 特に `Assets/` 直下のスクリプト群（`InteractSystem`/`PlayerInteract`など重複しているように見えるもの）は、どのScene・Prefabで実際にアタッチされているかをGrep/Scene内の確認をしてから判断すること。
3. **Scene・Prefab・ScriptableObjectのUnity参照を壊さない**: `.meta`ファイルのGUIDは絶対に変更・再生成しない。ファイルの移動・リネームは、Unityエディタ外で行うとGUID参照が壊れる可能性が高いため、可能な限りUnityエディタ内操作を前提とした変更提案に留める、またはユーザーに確認する。
4. **大規模な変更を行う前に変更内容を説明する**: 複数ファイルにまたがる変更や、シングルトン・Scene遷移ロジックに関わる変更は、着手前に方針を説明し合意を得る。
5. **不要に見えるスクリプトやアセットを勝手に削除しない**: 特に `Assets/_Recovery/`, 未使用に見えるアセットストア素材、重複して見えるインタラクトシステムなど、判断が難しいものはユーザーに確認する。
6. **既存のゲーム仕様を推測で変更しない**: 例えば「どうぐ」コマンドの未実装、「にげる」の常時成功、敗北時の扱いが未確定である点などは、明示的にそうなっていることがコード上のコメントからも読み取れる。仕様を「直す」のではなく、指示された範囲でのみ実装する。
7. **既存の設計・命名規則を尊重する**: ファイル名とクラス名が一致しない既存の慣習があるが、新規ファイルではファイル名とクラス名は一致させること（既存の不一致を新規ファイルにまで広げない）。日本語コメント・日本語Inspector表記（`[Header("...")]`）の慣習に合わせる。
8. **コード変更後はコンパイルエラーや明らかな参照切れがないかを確認する**: MonoBehaviourのpublicフィールドをリネーム・削除するとInspector上のアサインが外れる（Missing Reference）ため、フィールドの型・シリアライズ名を変更する場合は特に注意する。可能であればUnityエディタでコンパイルが通ることを確認する。
