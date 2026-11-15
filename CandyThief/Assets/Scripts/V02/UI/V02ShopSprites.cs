using System;
using UnityEngine;

// 가게 배경은 에디터와 빌드가 같은 Resources.Load 를 쓴다.
// AssetTable.FilePath 문구는 바꾸지 않는다. 정식 파일은 Assets/Resources/V02/Shops/ 에 같은 파일명으로 둔다.
public static class V02ShopSprites
{
    public const string ShopFolder = "V02/Shops";

    public static Sprite Load(string filePath)
    {
        string key = ResourcesKey(filePath);
        if (string.IsNullOrEmpty(key))
            return null;
        return Resources.Load<Sprite>(key);
    }

    // ShopTable.BackgroundImageKey → AssetTable.FilePath → Resources. 에디터와 빌드가 같다.
    public static Sprite LoadByImageKey(CsvDatabase database, string imageKey)
    {
        if (database == null || string.IsNullOrEmpty(imageKey))
            return null;

        AssetRow asset;
        if (!database.TryGetAssetByImageKey(imageKey, out asset))
            return null;
        return Load(asset.FilePath);
    }

    public static string ResourcesKey(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;

        string path = filePath.Replace('\\', '/').Trim();
        int slash = path.LastIndexOf('/');
        int dot = path.LastIndexOf('.');
        if (dot > slash)
            path = path.Substring(0, dot);

        const string marker = "Resources/";
        int at = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (at >= 0)
            return path.Substring(at + marker.Length);

        if (path.IndexOf("/Shops/", StringComparison.OrdinalIgnoreCase) < 0)
            return string.Empty;

        int nameAt = path.LastIndexOf('/');
        string name = nameAt >= 0 ? path.Substring(nameAt + 1) : path;
        if (name.Length == 0)
            return string.Empty;
        return ShopFolder + "/" + name;
    }
}
