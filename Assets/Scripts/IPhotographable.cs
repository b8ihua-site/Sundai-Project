// カメラで撮影されたときに反応するオブジェクトが実装するインターフェース
// 例: 知識の風（KnowledgeWind）が画面中央で撮影されたら具現化する、など
public interface IPhotographable
{
    void OnPhotographed();

    // 構え中、画面中央に捉えられているかどうかの通知（名前表示などに使う）
    void SetAimHighlight(bool highlighted);
}
