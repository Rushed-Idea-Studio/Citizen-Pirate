using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;
using TMPro;

public class DiceManager : Singleton<DiceManager>
{
    private class RuntimeStatDie
    {
        public StatDieDefinition Definition { get; private set; }
        public string ID { get; private set; }
        public int Value { get; private set; }
        public string AssignedTarget { get; private set; }
        public bool IsLocked { get; private set; }
        public GameObject UIObject { get; set; }

        public RuntimeStatDie(StatDieDefinition definition, string id)
        {
            this.Definition = definition;
            this.ID = id;
            this.Value = 1;
            this.AssignedTarget = "";
            this.IsLocked = false;
        }

        public void Roll()
        {
            this.Value = UnityEngine.Random.Range(1, 7);
            this.AssignedTarget = "";
        }

        public void SetValue(int newValue)
        {
            this.Value = Mathf.Clamp(newValue, 1, 6);
        }

        // Assigns the die to a specific target option
        public void AssignTo(string target)
        {
            this.AssignedTarget = target;
        }

        public void SetLocked(bool isLocked)
        {
            this.IsLocked = isLocked;
        }

        public void ClearSelection()
        {
            this.AssignedTarget = "";
        }
    }
    
    [Header("Database")]
    [SerializeField] private List<StatDieDefinition> allPossibleStatDice = new List<StatDieDefinition>();

    [Header("Dice UI Setup")]
    [SerializeField] private GameObject statDiePrefab;
    [SerializeField] private Transform uiParent;

    private List<RuntimeStatDie> playerStatDice = new List<RuntimeStatDie>();
    
    // Tracks the current active dialogue options to cycle through
    private List<string> currentActiveOptions = new List<string>();
    private string lastActiveTypesCSV = "";

    protected override void Awake()
    {
        base.Awake();
    }
    
    private void OnEnable()
    {
        Lua.RegisterFunction("IsHighestPower", this, SymbolExtensions.GetMethodInfo(() => LuaIsHighestPower("", "")));
        Lua.RegisterFunction("HasPower", this, SymbolExtensions.GetMethodInfo(() => LuaHasPower("", "", (double)0)));
        Lua.RegisterFunction("GetCurrentPower", this, SymbolExtensions.GetMethodInfo(() => LuaGetCurrentPower("", "")));
        Lua.RegisterFunction("ClearStatDiceSelection", this, SymbolExtensions.GetMethodInfo(() => LuaClearSelection()));
        Lua.RegisterFunction("SetStatDieValue", this, SymbolExtensions.GetMethodInfo(() => LuaSetStatDieValue("", (double)0)));
        Lua.RegisterFunction("AddStatDie", this, SymbolExtensions.GetMethodInfo(() => LuaAddStatDie("", "")));
        Lua.RegisterFunction("AddStatDieAutoID", this, SymbolExtensions.GetMethodInfo(() => LuaAddStatDieAutoID("")));
    }

    private void OnDisable()
    {
        Lua.UnregisterFunction("IsHighestPower");
        Lua.UnregisterFunction("HasPower");
        Lua.UnregisterFunction("GetCurrentPower");
        Lua.UnregisterFunction("ClearStatDiceSelection");
        Lua.UnregisterFunction("SetStatDieValue");
        Lua.UnregisterFunction("AddStatDie");
        Lua.UnregisterFunction("AddStatDieAutoID");
    }

    public void AddStatDie(string statDieType, string statDieID)
    {
        StatDieDefinition def = allPossibleStatDice.Find(die => die.statDieType.ToLowerInvariant() == statDieType.ToLowerInvariant());

        if (def != null)
        {
            RuntimeStatDie newStatDie = new RuntimeStatDie(def, statDieID);
            playerStatDice.Add(newStatDie);
            CreateStatDieUI(newStatDie);
        }
        else
        {
            Debug.LogWarning($"Stat Die with type \"{statDieType}\" not found in the database.");
        }
    }

    public void AddStatDieAutoID(string statDieType)
    {
        int count = 0;

        foreach (RuntimeStatDie die in playerStatDice)
        {
            if (die.Definition.statDieType.ToLowerInvariant() == statDieType.ToLowerInvariant())
            {
                count++;
            }
        }        
        string generatedID = statDieType.ToLowerInvariant() + (count + 1).ToString();

        AddStatDie(statDieType, generatedID);
    }

    public bool IsValidDieType(string statDieType)
    {
        if (string.IsNullOrWhiteSpace(statDieType)) return false;
        return allPossibleStatDice.Exists(die => die.statDieType.ToLowerInvariant() == statDieType.ToLowerInvariant());
    }

    public void RollStatDice()
    {
        foreach (var statDie in playerStatDice)
        {
            statDie.Roll();
        }
        UpdateAllStatDiceVisuals();
    }

    public void SetStatDieValue(string statDieID, int value)
    {
        RuntimeStatDie statDie = playerStatDice.Find(die => die.ID.ToLowerInvariant() == statDieID.ToLowerInvariant());

        if (statDie != null)
        {
            statDie.SetValue(value);
            UpdateSingleStatDieVisual(statDie);
        }
        else
        {
            Debug.LogWarning($"Stat Die with ID \"{statDieID}\" not found among player's dice.");
        }
    }

    private void CreateStatDieUI(RuntimeStatDie statDie)
    {
        if (statDiePrefab != null && uiParent != null)
        {
            GameObject uiObj = Instantiate(statDiePrefab, uiParent);
            statDie.UIObject = uiObj;

            Image img = uiObj.GetComponent<Image>();
            if (img != null) img.color = statDie.Definition.statDieColor;

            Button btn = uiObj.GetComponent<Button>();
            if (btn == null) btn = uiObj.AddComponent<Button>();
            btn.onClick.AddListener(() => OnStatDieClicked(statDie));

            UpdateSingleStatDieVisual(statDie);
        }
        else
        {
            Debug.LogWarning("Stat Die Prefab or UI Parent is not assigned in the DiceManager.");
        }
    }

    private void OnStatDieClicked(RuntimeStatDie statDie)
    {
        if (statDie.IsLocked) return;

        List<string> validTargets = new List<string>();
        string dieType = statDie.Definition.statDieType.ToLowerInvariant();

        // Filter available options based on rules
        foreach (string opt in currentActiveOptions)
        {
            if (opt == "grey" && dieType != "grey") 
            {
                continue;
            }
            validTargets.Add(opt);
        }

        if (validTargets.Count > 0)
        {
            int currentIndex = validTargets.IndexOf(statDie.AssignedTarget);
            int nextIndex = currentIndex + 1;

            if (nextIndex >= validTargets.Count)
            {
                statDie.AssignTo("");
            }
            else
            {
                statDie.AssignTo(validTargets[nextIndex]);
            }
        }

        UpdateSingleStatDieVisual(statDie);

        if (DialogueManager.isConversationActive)
        {
            DialogueManager.UpdateResponses();
        }
    }

    public void UpdateAllStatDiceVisuals()
    {
        foreach (RuntimeStatDie statDie in playerStatDice)
        {
            UpdateSingleStatDieVisual(statDie);
        }
    }
    
    private void UpdateSingleStatDieVisual(RuntimeStatDie statDie)
    {
        if (statDie.UIObject != null)
        {
            TextMeshProUGUI text = statDie.UIObject.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                if (string.IsNullOrEmpty(statDie.AssignedTarget))
                {
                    text.text = statDie.Value.ToString();
                }
                else
                {
                    text.text = $"{statDie.Value}\n<size=40%>{statDie.AssignedTarget.ToUpper()}</size>";
                }
            }

            Vector3 localPos = statDie.UIObject.transform.localPosition;
            localPos.y = string.IsNullOrEmpty(statDie.AssignedTarget) ? 0f : 20f;
            statDie.UIObject.transform.localPosition = localPos;

            Button button = statDie.UIObject.GetComponent<Button>();
            if (button != null) button.interactable = !statDie.IsLocked;
        }
    }
    
    // Updates the current state based on dialogue CSV and automatically locks base stats
    private void UpdateEncounterState(string csv)
    {
        if (lastActiveTypesCSV == csv.ToLowerInvariant())
        {
            return;
        }

        lastActiveTypesCSV = csv.ToLowerInvariant();
        currentActiveOptions = GetActiveTypes(lastActiveTypesCSV);

        foreach (RuntimeStatDie die in playerStatDice)
        {
            string type = die.Definition.statDieType.ToLowerInvariant();
            
            if (type != "grey" && currentActiveOptions.Contains(type))
            {
                die.SetLocked(true);
                die.AssignTo(type);
            }
            else
            {
                die.SetLocked(false);
                if (!string.IsNullOrEmpty(die.AssignedTarget) && !currentActiveOptions.Contains(die.AssignedTarget))
                {
                    die.AssignTo("");
                }
            }
        }
        UpdateAllStatDiceVisuals();
    }

    private bool IsHighestPower(string targetType, string activeTypesCSV)
    {
        UpdateEncounterState(activeTypesCSV);

        int currentPower = CalculateCurrentPower(targetType, activeTypesCSV);
        foreach (string type in currentActiveOptions)
        {
            if (type != targetType.ToLowerInvariant())
            {
                int otherPower = CalculateCurrentPower(type, activeTypesCSV);
                if (otherPower > currentPower)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public bool IsHighestPowerForResponse(string targetType, string activeTypesCSV)
    {
        return IsHighestPower(targetType, activeTypesCSV);
    }

    public int CalculateCurrentPower(string targetType, string activeTypesCSV)
    {
        UpdateEncounterState(activeTypesCSV);

        int totalPower = 0;
        string targetLower = targetType.ToLowerInvariant();

        foreach (RuntimeStatDie die in playerStatDice)
        {
            if (die.AssignedTarget == targetLower)
            {
                totalPower += die.Value;
            }
        }

        return totalPower;
    }

    private List<string> GetActiveTypes(string activeTypesCSV)
    {
        string[] activeTypes = activeTypesCSV.ToLowerInvariant().Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        List<string> activeList = new List<string>();

        foreach (string type in activeTypes)
        {
            string trimmedType = type.Trim();
            if (!activeList.Contains(trimmedType))
            {
                activeList.Add(trimmedType);
            }
        }

        return activeList;
    }

    private bool LuaIsHighestPower(string targetType, string activeTypesCSV)
    {
        return IsHighestPower(targetType, activeTypesCSV);
    }
    
    private bool LuaHasPower(string targetType, string activeTypesCSV, double requiredValue)
    {
        return CalculateCurrentPower(targetType, activeTypesCSV) >= (int)requiredValue;
    }

    private double LuaGetCurrentPower(string targetType, string activeTypesCSV)
    {
        return (double)CalculateCurrentPower(targetType, activeTypesCSV);
    }

    private void LuaClearSelection()
    {
        lastActiveTypesCSV = "";
        
        foreach (RuntimeStatDie statDie in playerStatDice)
        {
            statDie.ClearSelection();
        }
        UpdateAllStatDiceVisuals();
    }

    private void LuaSetStatDieValue(string statDieType, double newValue)
    {
        SetStatDieValue(statDieType, (int)newValue);
    }

    private void LuaAddStatDie(string statDieType, string statDieID)
    {
        AddStatDie(statDieType, statDieID);
    }

    private void LuaAddStatDieAutoID(string statDieType)
    {
        AddStatDieAutoID(statDieType);
    }
}