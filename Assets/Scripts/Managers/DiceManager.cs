using System;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using TMPro;

public class DiceManager : Singleton<DiceManager>
{
    [Tooltip("The dice values are stored in the Dialogue System variables: Green_die, Red_die, Grey_die")]
    [Serializable]
    private class StatDie
    {        
        [SerializeField] private int value = 1;
        public int Value { get { return value; } }

        public void Roll()
        {
            this.value = UnityEngine.Random.Range(1, 7);
        }

        public void SetValue(int newValue)
        {
            this.value = Mathf.Clamp(newValue, 0, 6);
        }
    }
    
    [Header("Dice Data")]
    [SerializeField] private StatDie greenDie;
    [SerializeField] private StatDie redDie;
    [SerializeField] private StatDie greyDie;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI greenDieText;
    [SerializeField] private TextMeshProUGUI redDieText;
    [SerializeField] private TextMeshProUGUI greyDieText;

    private void Awake()
    {
        base.Awake();

        // Initialize dice if they are not initialized by the Inspector
        if (greenDie == null) greenDie = new StatDie();
        if (redDie == null) redDie = new StatDie();
        if (greyDie == null) greyDie = new StatDie();        
    }

    public void RollDice()
    {
        greenDie.Roll();
        redDie.Roll();
        greyDie.Roll();

        string resultMessage = $"Green Die: {greenDie.Value}, Red Die: {redDie.Value}, Grey Die: {greyDie.Value}";
        Debug.Log($"Dice random roll result: {resultMessage}");

        DialogueLua.SetVariable("Green_die", greenDie.Value);
        DialogueLua.SetVariable("Red_die", redDie.Value);
        DialogueLua.SetVariable("Grey_die", greyDie.Value);

        UpdateDiceVisuals();
    }

    public void SetDieValue(string dieColor, int value)
    {
        string color = string.Empty;

        switch (dieColor?.ToLowerInvariant())
        {
            case "green":
                greenDie.SetValue(value);
                color = "Green";
                break;
            case "red":
                redDie.SetValue(value);
                color = "Red";
                break;
            case "grey":
                greyDie.SetValue(value);
                color = "Grey";
                break;
            default:
                Debug.LogWarning($"Invalid die color: {dieColor}");
                break;
        }

        if (!string.IsNullOrEmpty(color))
        {
            DialogueLua.SetVariable($"{color}_die", value);
            UpdateDiceVisuals();
        }
        else
        {
            Debug.LogWarning($"Failed to set die value for color: {dieColor}");
        }
    }

    /// <summary>
    /// Updates the UI text elements to reflect the current values of the dice.
    /// </summary>
    private void UpdateDiceVisuals()
    {
        if (greenDieText != null) greenDieText.text = greenDie.Value.ToString();
        if (redDieText != null) redDieText.text = redDie.Value.ToString();
        if (greyDieText != null) greyDieText.text = greyDie.Value.ToString();
    }
}