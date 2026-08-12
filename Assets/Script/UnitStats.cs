using UnityEngine;

/// <summary>
/// Menyimpan data dasar (base stats) sebuah unit — baik player maupun enemy.
/// Dibuat sebagai ScriptableObject supaya bisa di-tweak lewat Inspector
/// tanpa perlu mengubah kode, dan bisa direuse antar battle.
/// </summary>
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

   [Header("Enemy Config (hanya relevan jika unit ini musuh)")]
public EnemyType enemyType = EnemyType.Common; // menentukan escape chance: Common/Elite bisa, Boss tidak bisa

}

   //public EnemyType enemyType = EnemyType.Common; // menentukan escape chance: Common/Elite bisa, Boss tidak bis