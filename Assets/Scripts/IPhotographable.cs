// カメラで撮影されたときに反応するオブジェクトが実装するインターフェース
// 例: 知識の風（KnowledgeWind）が画面中央で撮影されたら「見つけた！」演出→たたかう/みのがす選択、など
public interface IPhotographable
{
    // 発見メッセージ等に使う表示名（例:「数学の風」）
    string DisplayName { get; }

    // 構え中、画面中央に捉えられているかどうかの通知（名前表示などに使う）
    void SetAimHighlight(bool highlighted);

    // 撮影で見つかった瞬間に呼ばれる（発見演出のトリガー。ここでは戦闘には入らない）
    void OnPhotographed();

    // 「たたかう」が選ばれた
    void Capture();

    // 「みのがす」が選ばれた
    void Release();
}
