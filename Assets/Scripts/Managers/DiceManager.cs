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
        public bool IsSelected { get; private set; }
        public GameObject UIObject { get; set; }

        public RuntimeStatDie(StatDieDefinition definition, string id)
        {
            this.Definition = definition;
            this.ID = id;
            this.Value = 1;
            this.IsSelected = false;
        }

        public void Roll()
        {
            this.Value = UnityEngine.Random.Range(1, 7);
            this.IsSelected = false;
        }

        public void SetValue(int newValue)
        {
            this.Value = Mathf.Clamp(newValue, 1, 6);
        }

        public void ToggleSelection()
        {
            this.IsSelected = !this.IsSelected;
        }

        public void ClearSelection()
        {
            this.IsSelected = false;
        }
    }
    
    [Header("Database ")]
    [SerializeField] private List<StatDieDefinition> allPossibleStatDice = new List<StatDieDefinition>();

    [Header("Dice UI Setup")]
    [SerializeField] private GameObject statDiePrefab;
    [SerializeField] private Transform uiParent;

    private List<RuntimeStatDie> playerStatDice = new List<RuntimeStatDie>();

    private void Awake()
    {
        base.Awake();
    }
    
    private void OnEnable()
    {
        Lua.RegisterFunction("HasPower", this, SymbolExtensions.GetMethodInfo(() => LuaHasPower("", "", (double)0)));
        Lua.RegisterFunction("GetCurrentPower", this, SymbolExtensions.GetMethodInfo(() => LuaGetCurrentPower("", "")));
        Lua.RegisterFunction("ClearStatDiceSelection", this, SymbolExtensions.GetMethodInfo(() => LuaClearSelection()));
        Lua.RegisterFunction("SetStatDieValue", this, SymbolExtensions.GetMethodInfo(() => LuaSetStatDieValue("", (double)0)));
        Lua.RegisterFunction("AddStatDie", this, SymbolExtensions.GetMethodInfo(() => LuaAddStatDie("", "")));
        Lua.RegisterFunction("AddStatDieAutoID", this, SymbolExtensions.GetMethodInfo(() => LuaAddStatDieAutoID("")));
    }

    private void OnDisable()
    {
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
            Debug.LogWarning($"Stat Die with type '{statDieType}' not found in the database.");
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

    /// <summary>
    /// Rolls all the player's stat dice and updates the Dialogue System variables and UI text elements accordingly.
    /// </summary>
    public void RollStatDice()
    {
        foreach (var statDie in playerStatDice)
        {
            statDie.Roll();
        }
        UpdateAllStatDiceVisuals();
    }

    /// <summary>
    /// Sets the value of a specific stat die based on its ID and updates the variable and UI element accordingly.
    /// </summary>
    /// <param name="statDieID"></param>
    /// <param name="value"></param>
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
            Debug.LogWarning($"Stat Die with type '{statDieID}' not found among player's dice.");
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
        statDie.ToggleSelection();
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
            if (text != null) text.text = statDie.Value.ToString();

            Vector3 localPos = statDie.UIObject.transform.localPosition;
            localPos.y = statDie.IsSelected ? 20f : 0f;
            statDie.UIObject.transform.localPosition = localPos;
        }
    }
    
    private int GetStatSumByType(string type)
    {
        int sum = 0;

        foreach (RuntimeStatDie statDie in playerStatDice)
        {
            if (statDie.Definition.statDieType.ToLowerInvariant() == type.ToLowerInvariant())
            {
                sum += statDie.Value;
            }
        }
        return sum;
    }

    /// <summary>
    /// Calculates the total power for a given target type based on the player's selected stat dice and the active types provided in a CSV string.
    /// The calculation includes the sum of all stat dice of the target type and adds the values of any selected stat dice that are either grey or not part of the active types and not of the target type.
    /// </summary>
    /// <param name="targetType"></param>
    /// <param name="activeTypesCSV"></param>
    /// <returns></returns>
    private int CalculateCurrentPower(string targetType, string activeTypesCSV)
    {
        string[] separator = new string[] { "," };
        string[] activeTypes = activeTypesCSV.ToLowerInvariant().Split(separator, StringSplitOptions.RemoveEmptyEntries);

        List<string> activeList = new List<string>();
        
        foreach (string t in activeTypes)
        {
            activeList.Add(t.Trim());
        }

        int totalPower = 0;
        string targetLower = targetType.ToLowerInvariant();

        totalPower += GetStatSumByType(targetLower);

        foreach (RuntimeStatDie statDie in playerStatDice)
        {
            if (statDie.IsSelected)
            {
                string type = statDie.Definition.statDieType.ToLowerInvariant();
                
                bool isGrey = (type == "grey");
                bool isUnusedWildcard = (!activeList.Contains(type) && !isGrey && type != targetLower);

                if (isGrey || isUnusedWildcard)
                {
                    totalPower += statDie.Value;
                }
            }
        }

        return totalPower;
    }
    
    /// <summary>
    /// Checks if the player has enough power for a given target type based on the active types and the required value.
    /// Returns true if the calculated current power is greater than or equal to the required value; otherwise, returns false.
    /// This method is registered as a Lua function for use in dialogue scripts.
    /// </summary>
    /// <param name="targetType"></param>
    /// <param name="activeTypesCSV"></param>
    /// <param name="requiredValue"></param>
    /// <returns></returns>
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