using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

public class SequencerCommandSetDie : SequencerCommand
{
    private void Awake()
    {
        string dieColor = GetParameter(0);
        int dieValue = GetParameterAsInt(1);

        DiceManager.Instance.SetDieValue(dieColor, dieValue);

        Stop();
    }
}
