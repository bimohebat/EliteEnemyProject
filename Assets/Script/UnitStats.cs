using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitStats", menuName = "Battle/UnitStats")]
public class UnitStats : ScriptableObject
{
    [Header("Identity")]
    public string unitName = "Unit";
    public Sprite portrait;

    [Header("Core Stats")]
    public int maxHP = 100;
    public int maxMP = 20;
    public int attack = 10;
    public int defense = 5;
    public int speed = 10;     // urutan giliran (turn order)
    public int agility = 10;   // memengaruhi peluang escape & dodge
    public int luck = 5;       // memengaruhi peluang critical hit

    [Header("Battle Config")]
    public bool canBeEscapedFrom = true; // false untuk boss battle
}
