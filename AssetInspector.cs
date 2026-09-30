using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
public class AssetInspector : EditorWindow
{
    [MenuItem("Window/Lucky/Asset Inspector")]
    public static void ShowWindow()
    {
        GetWindow<AssetInspector>("Asset Inspector");
    }

    [MenuItem("Assets/Lucky/Inspect Cards and Decks")]
    public static void InspectAssetsContext()
    {
        ShowWindow();
    }

    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("Lucky Card & Deck Inspector", EditorStyles.boldLabel);
        GUILayout.Space(10);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Load and display all cards
        GUILayout.Label("=== CARDS ===", EditorStyles.boldLabel);
        string[] cardGUIDs = AssetDatabase.FindAssets("t:Card", new[] { "Assets/CardsData" });
        
        foreach (string guid in cardGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Card card = AssetDatabase.LoadAssetAtPath<Card>(path);
            
            if (card != null)
            {
                EditorGUILayout.BeginVertical("box");
                
                GUILayout.Label($"Card: {card.cardName}", EditorStyles.boldLabel);
                GUILayout.Label($"Type: {card.cardType}");
                GUILayout.Label($"Cost: {card.cost}");
                
                // Show type-specific properties
                if (card.IsUnitCard)
                {
                    GUILayout.Label($"Base Attack: {card.baseAttack}");
                    GUILayout.Label($"Base HP: {card.baseHP}");
                    GUILayout.Label($"Base Armor: {card.baseArmor}");
                    
                    if (card.cardType == CardType.SiegeEngine)
                    {
                        GUILayout.Label($"Base Attack Speed: {card.baseAttackSpeed}");
                    }
                    else
                    {
                        GUILayout.Label($"Base Count: {card.baseCount}");
                    }
                }
                else if (card.IsEffectCard)
                {
                    GUILayout.Label($"Effect: {card.effect}");
                }
                
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }
        
        GUILayout.Space(20);
        
        // Load and display all decks
        GUILayout.Label("=== DECKS ===", EditorStyles.boldLabel);
        string[] deckGUIDs = AssetDatabase.FindAssets("t:Deck", new[] { "Assets/Decks" });
        
        foreach (string guid in deckGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Deck deck = AssetDatabase.LoadAssetAtPath<Deck>(path);
            
            if (deck != null)
            {
                EditorGUILayout.BeginVertical("box");
                
                GUILayout.Label($"Deck: {deck.DeckName}", EditorStyles.boldLabel);
                GUILayout.Label($"ID: {deck.DeckID}");
                
                var stats = deck.GetDeckStats();
                GUILayout.Label($"Total Cards: {stats.totalCards}");
                GUILayout.Label($"Unique Cards: {stats.uniqueCards}");
                
                bool isValid = deck.ValidateDeck();
                GUILayout.Label($"Is Valid: {(isValid ? "✓" : "✗")}");
                
                if (!isValid && deck.ValidationErrors.Count > 0)
                {
                    GUILayout.Label("Validation Errors:", EditorStyles.boldLabel);
                    foreach (string error in deck.ValidationErrors)
                    {
                        GUILayout.Label($"  • {error}", EditorStyles.miniLabel);
                    }
                }
                
                GUILayout.Space(5);
                GUILayout.Label("Card Breakdown:", EditorStyles.boldLabel);
                
                foreach (var cardEntry in deck.GetCardCounts())
                {
                    if (cardEntry.Value > 0)
                    {
                        GUILayout.Label($"  {cardEntry.Key.cardName}: {cardEntry.Value}x");
                    }
                }
                
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Refresh"))
        {
            AssetDatabase.Refresh();
            Repaint();
        }
    }
}
#endif
