using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Holds expression name to sprite mappings for one character.
/// </summary>
[System.Serializable]
public class ExpressionData
{
    public string expressionName;
    public Sprite sprite;
}

/// <summary>
/// ScriptableObject containing all portrait expressions for a character.
/// Create via Assets -> Create -> Dialogue -> Character Portrait
/// </summary>
[CreateAssetMenu(fileName = "CharacterPortrait", menuName = "Dialogue/Character Portrait")]
public class CharacterPortrait : ScriptableObject
{
    [Header("Character Info")]
    public string characterName;
    
    [Header("Expressions")]
    [Tooltip("Map expression names to sprite images")]
    public List<ExpressionData> expressions = new List<ExpressionData>();
    
    /// <summary>
    /// Get sprite for a specific expression. Returns default expression if not found, or null if no default.
    /// </summary>
    public Sprite GetExpression(string expressionName)
    {
        // If no expression specified, try to use default
        if (string.IsNullOrEmpty(expressionName))
        {
            return GetDefaultExpression();
        }
            
        // Look for exact match
        foreach (ExpressionData expr in expressions)
        {
            if (expr.expressionName == expressionName)
            {
                return expr.sprite;
            }
        }
        
        // Not found - try to find "default" expression as fallback
        Debug.LogWarning($"[CharacterPortrait] Expression '{expressionName}' not found for character '{characterName}', trying default");
        
        foreach (ExpressionData expr in expressions)
        {
            if (expr.expressionName == "default")
            {
                return expr.sprite;
            }
        }
        
        // No default found either - return null to hide portrait
        Debug.LogWarning($"[CharacterPortrait] No default expression found for character '{characterName}'");
        return null;
    }
    
    /// <summary>
    /// Get the first expression sprite (useful for default/fallback).
    /// </summary>
    public Sprite GetDefaultExpression()
    {
        if (expressions.Count > 0 && expressions[0].sprite != null)
        {
            return expressions[0].sprite;
        }
        return null;
    }
}
