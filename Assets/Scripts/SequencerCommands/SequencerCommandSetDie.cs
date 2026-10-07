using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

public class SequencerCommandSetDie : SequencerCommand
{
    [Tooltip("Sets the value of a stat die by its ID.")]
    private void Awake()
    {
        string statDieID = GetParameter(0);
        int dieValue = GetParameterAsInt(1);

        DiceManager.Instance.SetStatDieValue(statDieID, dieValue);
        DiceManager.Instance.UpdateAllStatDiceVisuals();

        Stop();
    }
}
