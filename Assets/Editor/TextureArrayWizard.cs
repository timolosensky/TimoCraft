using UnityEngine;
using UnityEditor;

public class TextureArrayWizard : ScriptableWizard
{
    [MenuItem("VoxelTools/Create Texture Array")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<TextureArrayWizard>("Create Texture Array", "Create");
    }

    public Texture2D[] textures;

    void OnWizardCreate()
    {
        if (textures.Length == 0) return;

        Texture2D t = textures[0];
        Texture2DArray textureArray = new Texture2DArray(t.width, t.height, textures.Length, t.format, false);
        
        textureArray.filterMode = FilterMode.Point; // Pixel-Look
        textureArray.wrapMode = TextureWrapMode.Repeat; // Wichtig für Tiling

        for (int i = 0; i < textures.Length; i++)
        {
            Graphics.CopyTexture(textures[i], 0, 0, textureArray, i, 0);
        }

        string path = "Assets/Resources/TerrainTextureArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Texture Array gespeichert unter: " + path);
    }
}