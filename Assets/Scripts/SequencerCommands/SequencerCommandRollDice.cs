using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

public class SequencerCommandRollDice : SequencerCommand
{
    private void Awake()
    {
        DiceManager.Instance.RollDice();

        Stop();
    }
}
