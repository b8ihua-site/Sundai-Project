public static class BattleContext
{
    public static bool HasData = false;
    public static string EnemyName = "なぞの敵";
    public static int EnemyMaxHP = 100;
    public static int EnemyAttack = 20;
    public static string Subject = "市ヶ谷";

    // 知識の風から具現化した敵の色（"Blue"/"Green"/"Red"）。風以外の敵は空文字
    public static string WindColor = "";

    // 撃破時の所持金報酬（この範囲でランダム→敵レベルを掛ける）
    public static int MoneyMin = 10;
    public static int MoneyMax = 20;

    // プレイヤーレベルに応じて敵側が持つレベル（報酬倍率に使う）
    public static int EnemyLevel = 1;
}