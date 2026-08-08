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
