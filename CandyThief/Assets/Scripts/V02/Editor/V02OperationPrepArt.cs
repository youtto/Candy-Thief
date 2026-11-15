using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// 작전 준비 화면용 단순 도형 리소스. 둥근 사각형(9-slice)과 소프트 그림자는 코드로 만든 흰색 도형이다.
// 완성 시안을 자르지 않고, 그림자·광택을 색으로 합성하지 않는다. 색과 그림자 강도는 프리팹의 Image 가 정한다.
public static class V02OperationPrepArt
{
    public const string Folder = "Assets/Art/V02/UI/Generated";
    public const string RoundLargePath = Folder + "/CT_UI_Shape_RoundLarge.png";
    public const string RoundSmallPath = Folder + "/CT_UI_Shape_RoundSmall.png";
    public const string SoftShadowPath = Folder + "/CT_UI_Shape_SoftShadow.png";
    public const string OutlineMaterialPath = Folder + "/CT_UI_Text_Outline.mat";

    public const float LargeRadius = 40f;
    public const float SmallRadius = 16f;
    public const float ShadowBorder = 80f;
    public const float ShadowInset = 32f;
    public const float ShadowRadius = 36f;

    static readonly Color OutlineColor = new Color(0.16f, 0.08f, 0.045f, 1f);

    public static void EnsureAssets()
    {
        EnsureFolder();
        EnsureShape(RoundLargePath, 96, LargeRadius, 0f, 0f, LargeRadius);
        EnsureShape(RoundSmallPath, 40, SmallRadius, 0f, 0f, SmallRadius);
        EnsureShape(SoftShadowPath, 160, ShadowRadius, ShadowInset, 16f, ShadowBorder);
        EnsureOutlineMaterial();
        AssetDatabase.SaveAssets();
    }

    public static Sprite RoundLarge()
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(RoundLargePath);
    }

    public static Sprite RoundSmall()
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(RoundSmallPath);
    }

    public static Sprite SoftShadow()
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SoftShadowPath);
    }

    public static Material OutlineMaterial()
    {
        return AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/V02/UI/Generated"))
            AssetDatabase.CreateFolder("Assets/Art/V02/UI", "Generated");
    }

    // inset 0 이면 텍스처 가장자리까지 채운 둥근 사각형. soft > 0 이면 inset 만큼 안쪽 도형의 가장자리를 흐린다.
    static void EnsureShape(string assetPath, int size, float radius, float inset, float soft, float border)
    {
        if (AssetDatabase.LoadAssetAtPath<Sprite>(assetPath) == null)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            const int samples = 4;
            float half = size * 0.5f;
            float core = half - inset - radius;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    for (int sy = 0; sy < samples; sy++)
                    {
                        for (int sx = 0; sx < samples; sx++)
                        {
                            float px = x + (sx + 0.5f) / samples - half;
                            float py = y + (sy + 0.5f) / samples - half;
                            float dx = Mathf.Max(Mathf.Abs(px) - core, 0f);
                            float dy = Mathf.Max(Mathf.Abs(py) - core, 0f);
                            float distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                            if (soft <= 0f)
                                sum += Mathf.Clamp01(0.5f - distance);
                            else
                                sum += 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((distance + soft) / (soft * 2f)));
                        }
                    }

                    byte alpha = (byte)Mathf.RoundToInt(255f * sum / (samples * samples));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            File.WriteAllBytes(full, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer == null)
            return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = new Vector4(border, border, border, border);
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    // 한글 TMP 폰트 머티리얼을 복제해 어두운 갈색 외곽선만 켠다. 글자색은 TMP 의 vertex color 가 정한다.
    static void EnsureOutlineMaterial()
    {
        TMP_FontAsset font = V02Ui.Font();
        if (font == null || font.material == null)
        {
            Debug.LogError("[V02] 한글 TMP 폰트가 없어 외곽선 머티리얼을 만들지 못했습니다.");
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        if (material == null)
        {
            material = new Material(font.material);
            material.name = "CT_UI_Text_Outline";
            AssetDatabase.CreateAsset(material, OutlineMaterialPath);
        }

        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        material.SetColor(ShaderUtilities.ID_OutlineColor, OutlineColor);
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.3f);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.5f);
        material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.02f);
        EditorUtility.SetDirty(material);
    }
}
