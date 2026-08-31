/// <summary>
/// State alur battle turn-based, dipakai oleh BattleManager.
/// </summary>
public enum BattleState
{
    Start,
    PlayerTurn,
    PlayerActionResolution,
    EnemyTurn,
    EnemyActionResolution,
    BattleWon,
    BattleLost,
    BattleEscaped
}

public enum PlayerActionType
{
    Attack,
    Skill,
    Item,
    Defend,
    Escape
}

/// <summary>
/// 3 tipe serangan yang bisa dipilih player saat memilih menu Attack.
/// Basic: tanpa cost. Heavy: konsumsi MP. Charged: konsumsi 1 giliran ekstra
/// (charge di giliran ini, meledak otomatis di giliran unit itu berikutnya).
/// </summary>
public enum AttackType
{
    Basic,
    Heavy,
    Charged
}

/// <summary>
/// Sisi/pihak dalam battle -- dipakai sistem turn order random
/// (lihat BattleManager.DecideNextSide).
/// </summary>
public enum BattleSide
{
    Player,
    Enemy
}