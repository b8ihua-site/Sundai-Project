// カメラで撮影されたときに反応するオブジェクトが実装するインターフェース
// 例: 知識の風（KnowledgeWind）が画面中央で撮影されたら具現化する、など
public interface IPhotographable
{
    void OnPhotographed();
}
