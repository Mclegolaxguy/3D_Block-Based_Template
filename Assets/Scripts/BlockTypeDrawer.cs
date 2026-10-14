#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(WorldManager.BlockType))]
public class BlockTypeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty blockNameProp = property.FindPropertyRelative("blockName");
        SerializedProperty isTransparentProp = property.FindPropertyRelative("isTransparent");
        SerializedProperty blockMaterialProp = property.FindPropertyRelative("blockMaterial");
        SerializedProperty seperateFacesProp = property.FindPropertyRelative("seperateFaces");
        SerializedProperty atlasSizeProp = property.FindPropertyRelative("textureAtlasSizeInBlocks");

        SerializedProperty singleTextureProp = property.FindPropertyRelative("singleTexture");
        SerializedProperty topTextureProp = property.FindPropertyRelative("topTexture");
        SerializedProperty bottomTextureProp = property.FindPropertyRelative("bottomTexture");
        SerializedProperty frontTextureProp = property.FindPropertyRelative("frontTexture");
        SerializedProperty backTextureProp = property.FindPropertyRelative("backTexture");
        SerializedProperty leftTextureProp = property.FindPropertyRelative("leftTexture");
        SerializedProperty rightTextureProp = property.FindPropertyRelative("rightTexture");

        float spacing = EditorGUIUtility.standardVerticalSpacing;
        Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
        rect.y += EditorGUIUtility.singleLineHeight + spacing;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            DrawProperty(ref rect, blockNameProp, spacing);
            DrawProperty(ref rect, isTransparentProp, spacing);
            DrawProperty(ref rect, blockMaterialProp, spacing);
            DrawProperty(ref rect, seperateFacesProp, spacing);
            DrawProperty(ref rect, atlasSizeProp, spacing);

            bool separate = seperateFacesProp.boolValue;

            if (separate)
            {
                DrawProperty(ref rect, topTextureProp, spacing);
                DrawProperty(ref rect, bottomTextureProp, spacing);
                DrawProperty(ref rect, frontTextureProp, spacing);
                DrawProperty(ref rect, backTextureProp, spacing);
                DrawProperty(ref rect, leftTextureProp, spacing);
                DrawProperty(ref rect, rightTextureProp, spacing);
            }
            else
            {
                DrawProperty(ref rect, singleTextureProp, spacing);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawProperty(ref Rect rect, SerializedProperty prop, float spacing)
    {
        if (prop == null) return;
        float height = EditorGUI.GetPropertyHeight(prop, true);
        rect.height = height;
        EditorGUI.PropertyField(rect, prop, true);
        rect.y += height + spacing;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        if (!property.isExpanded)
            return lineHeight;

        SerializedProperty seperateFacesProp = property.FindPropertyRelative("seperateFaces");
        bool separate = seperateFacesProp != null && seperateFacesProp.boolValue;

        float totalHeight = lineHeight + spacing;
        totalHeight += GetPropHeight(property.FindPropertyRelative("blockName"), spacing);
        totalHeight += GetPropHeight(property.FindPropertyRelative("isTransparent"), spacing);
        totalHeight += GetPropHeight(property.FindPropertyRelative("blockMaterial"), spacing);
        totalHeight += GetPropHeight(property.FindPropertyRelative("seperateFaces"), spacing);
        totalHeight += GetPropHeight(property.FindPropertyRelative("textureAtlasSizeInBlocks"), spacing);

        if (separate)
        {
            totalHeight += GetPropHeight(property.FindPropertyRelative("topTexture"), spacing);
            totalHeight += GetPropHeight(property.FindPropertyRelative("bottomTexture"), spacing);
            totalHeight += GetPropHeight(property.FindPropertyRelative("frontTexture"), spacing);
            totalHeight += GetPropHeight(property.FindPropertyRelative("backTexture"), spacing);
            totalHeight += GetPropHeight(property.FindPropertyRelative("leftTexture"), spacing);
            totalHeight += GetPropHeight(property.FindPropertyRelative("rightTexture"), spacing);
        }
        else
        {
            totalHeight += GetPropHeight(property.FindPropertyRelative("singleTexture"), spacing);
        }

        return totalHeight;
    }

    private float GetPropHeight(SerializedProperty prop, float spacing)
    {
        if (prop == null) return 0;
        return EditorGUI.GetPropertyHeight(prop, true) + spacing;
    }
}
#endif