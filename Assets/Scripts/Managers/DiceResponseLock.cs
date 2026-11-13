using System;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class DiceResponseLock : MonoBehaviour
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

            string powerDefinition = Field.LookupValue(response.destinationEntry.fields, "Description");

            if (string.IsNullOrWhiteSpace(powerDefinition)) continue;

            if (TryParsePowerDefinition(powerDefinition, out string targetType, out string activeTypesCSV))
            {
                response.enabled = DiceManager.Instance.IsHighestPowerForResponse(targetType, activeTypesCSV);
            }
            else
            {
                Debug.LogWarning(
                    $"Response '{response.destinationEntry.MenuText}' has no valid dice power definition. " +
                    "Set Description to: power=<type>;active=<type1,type2,...>");
            }
        }
    }

    private bool TryParsePowerDefinition(string powerDefinition, out string targetType, out string activeTypesCSV)
    {
        targetType = string.Empty;
        activeTypesCSV = string.Empty;

        if (string.IsNullOrWhiteSpace(powerDefinition)) return false;

        string[] fields = powerDefinition.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string field in fields)
        {
            string[] keyValue = field.Split(new[] { '=' }, 2);

            if (keyValue.Length != 2) continue;

            string key = keyValue[0].Trim().ToLowerInvariant();
            string value = keyValue[1].Trim();

            if (key == "power")
            {
                targetType = value;
            }
            else if (key == "active")
            {
                activeTypesCSV = value;
            }
        }
        return !string.IsNullOrEmpty(targetType) && !string.IsNullOrEmpty(activeTypesCSV);
    }
}
