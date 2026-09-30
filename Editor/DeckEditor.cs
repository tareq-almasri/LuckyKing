using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Deck))]
public class DeckEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        Deck deck = (Deck)target;
        
        GUILayout.Space(10);
        GUILayout.Label("Auto Fill Options", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Auto Fill Deck", GUILayout.Height(30)))
        {
            deck.AutoFillDeck();
        }
        
        if (GUILayout.Button("Smart Fill Deck", GUILayout.Height(30)))
        {
            deck.SmartFillDeck();
        }
        
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button("Clear Deck", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Deck", 
                "Are you sure you want to remove all cards from this deck?", 
                "Yes", "No"))
            {
                deck.ClearDeck();
            }
        }
        
        GUILayout.Space(10);
        
        // Show deck stats
        if (deck.Cards.Count > 0)
        {
            GUILayout.Label("Deck Statistics", EditorStyles.boldLabel);
            var stats = deck.GetDeckStats();
            
            EditorGUILayout.LabelField("Total Cards:", stats.totalCards.ToString());
            EditorGUILayout.LabelField("Unique Cards:", stats.uniqueCards.ToString());
            EditorGUILayout.LabelField("Infantry:", stats.infantryCount.ToString());
            EditorGUILayout.LabelField("Cavalry:", stats.cavalryCount.ToString());
            EditorGUILayout.LabelField("Siege Engines:", stats.siegeEngineCount.ToString());
            EditorGUILayout.LabelField("Buffs:", stats.buffCount.ToString());
            EditorGUILayout.LabelField("Debuffs:", stats.debuffCount.ToString());
            EditorGUILayout.LabelField("Logistics:", stats.logisticsCount.ToString());
            EditorGUILayout.LabelField("Average Cost:", stats.averageCost.ToString("F1"));
        }
    }
}