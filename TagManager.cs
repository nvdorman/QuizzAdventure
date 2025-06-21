#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class TagManager : MonoBehaviour
{
    [ContextMenu("Create Missing Tags")]
    void CreateMissingTags()
    {
        string[] requiredTags = { "Lava", "Spike", "Poison", "Trap" };
        
        foreach (string tag in requiredTags)
        {
            if (!TagExists(tag))
            {
                AddTag(tag);
                Debug.Log("✅ Tag '" + tag + "' berhasil ditambahkan!");
            }
            else
            {
                Debug.Log("ℹ️ Tag '" + tag + "' sudah ada.");
            }
        }
        
        Debug.Log("🎉 Semua tags sudah siap!");
    }
    
    bool TagExists(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tag)) return true;
        }
        return false;
    }
    
    void AddTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
        newTagProp.stringValue = tag;
        
        tagManager.ApplyModifiedProperties();
    }
}
#endif