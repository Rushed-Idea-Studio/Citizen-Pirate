using System;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class DiceResponseHandler : MonoBehaviour
{
    private bool isSubscribed;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void Subscribe()
    {
        if (isSubscribed) return;
        if (DialogueManager.instance == null) return;

        DialogueManager.instance.conversationResponseMenuPrepared += UpdateResponseLocks;
        isSubscribed = true;
    }

    private void OnDisable()
    {
        if (isSubscribed && DialogueManager.instance != null)
        {
            DialogueManager.instance.conversationResponseMenuPrepared -= UpdateResponseLocks;
            isSubscribed = false;
        }
    }

    private void UpdateResponseLocks(Response[] responses)
    {
        if (DiceManager.Instance == null || responses == null) return;

        foreach (Response response in responses)
        {
            if (response == null || response.destinationEntry == null) continue;

            // Reads the "DiceTarget" and "DiceActive" fields from the response's destination entry
            string targetType = Field.LookupValue(response.destinationEntry.fields, "DiceTarget");
            string activeTypesCSV = Field.LookupValue(response.destinationEntry.fields, "DiceActive");

            bool hasError = false;

            if (string.IsNullOrWhiteSpace(targetType) || string.IsNullOrWhiteSpace(activeTypesCSV)) 
                continue;

            if (!DiceManager.Instance.IsValidDieType(targetType))
            {
                Debug.LogError($"[Dice System] Invalid DiceTarget type '{targetType}' in response '{response.formattedText.text}' in node {response.destinationEntry.MenuText}. The target type must match one of the defined stat die types in the DiceManager.");
                hasError = true;
            }

            string[] activeTypes = activeTypesCSV.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string activeType in activeTypes)
            {
                if (!DiceManager.Instance.IsValidDieType(activeType.Trim()))
                {
                    Debug.LogError($"[Dice System] Invalid DiceActive type '{activeType}' in response '{response.formattedText.text}' in node {response.destinationEntry.MenuText}. The active types must match one of the defined stat die types in the DiceManager.");
                    hasError = true;
                }
            }

            if (hasError) continue;

            // 1. Lock
            response.enabled = DiceManager.Instance.IsHighestPowerForResponse(targetType, activeTypesCSV);

            // 2. Visual
            int currentPower = DiceManager.Instance.CalculateCurrentPower(targetType, activeTypesCSV);
            response.formattedText.text = $"{response.formattedText.text} [{currentPower}]";
        }
    }
}