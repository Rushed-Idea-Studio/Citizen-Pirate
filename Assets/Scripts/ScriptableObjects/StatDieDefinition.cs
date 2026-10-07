using UnityEngine;

[CreateAssetMenu(fileName = "NewStatDieDefinition", menuName = "Dice System/Stat Die Definition")]
public class StatDieDefinition : ScriptableObject
{
    [Tooltip("The type of the stat die (e.g., green, red, grey, ...).")]
    public string statDieType;
    public Color statDieColor = Color.white;
}