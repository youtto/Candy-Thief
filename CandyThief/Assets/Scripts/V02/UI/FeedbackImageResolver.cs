using System;
using System.Collections.Generic;
using UnityEngine;

// 결과 분기별 전용 이미지. 키가 비어 있거나 파일을 읽지 못하면 호출부가 기존 임시 프레임을 쓴다.
public static class FeedbackImageResolver
{
    public const string LogPrefix = "[FeedbackImage] ";

    public static string ImageKey(ResultFeedbackRow row, bool success)
    {
        if (row == null)
            return string.Empty;
        string key = success ? row.SuccessImageKey : row.FailImageKey;
        return string.IsNullOrEmpty(key) ? string.Empty : key.Trim();
    }

    public static bool TryResolve(CsvDatabase database, ResultFeedbackRow row, bool success, out Sprite sprite)
    {
        return TryResolve(row, success, key => LoadRegistered(database, key), out sprite);
    }

    public static bool TryResolve(ResultFeedbackRow row, bool success, Func<string, Sprite> load, out Sprite sprite)
    {
        sprite = null;
        string key = ImageKey(row, success);
        if (key.Length == 0)
            return false;

        Sprite loaded = load != null ? load(key) : null;
        if (loaded == null)
        {
            Debug.LogWarning(LogPrefix + "전용 이미지를 찾지 못해 임시 피드백을 사용합니다. key=" + key);
            return false;
        }

        sprite = loaded;
        return true;
    }

    static Sprite LoadRegistered(CsvDatabase database, string imageKey)
    {
        if (database == null || string.IsNullOrEmpty(imageKey))
            return null;

        AssetRow asset;
        if (!database.TryGetAssetByImageKey(imageKey, out asset) || asset == null)
            return null;
        return V02FeedbackSprites.Load(asset.FilePath);
    }
}

public static class V02FeedbackSprites
{
    public static Sprite Load(string filePath)
    {
        string key = V02ShopSprites.ResourcesKey(filePath);
        if (!string.IsNullOrEmpty(key))
        {
            Sprite sprite = Resources.Load<Sprite>(key);
            if (sprite != null)
                return sprite;
        }

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(filePath))
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
#endif
        return null;
    }
}

public sealed class FeedbackImageAuditReport
{
    public int SlotCount;
    public int EmptyKeyCount;
    public int DuplicateKeyCount;
    public int UnknownKeyCount;
    public int MissingFileCount;
    public int LoadedCount;
    public bool Complete;
}

// 개발 중 누락은 경고와 임시 폴백이다. Complete는 승인된 142슬롯이 모두 로드될 때만 참이다.
public static class FeedbackImageAudit
{
    public const int ApprovedSlotCount = 142;

    public static FeedbackImageAuditReport Evaluate(CsvDatabase database)
    {
        var report = new FeedbackImageAuditReport();
        if (database == null || database.EnabledResultFeedback == null)
            return report;

        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        IReadOnlyList<ResultFeedbackRow> rows = database.EnabledResultFeedback;
        for (int i = 0; i < rows.Count; i++)
        {
            CountSlot(database, rows[i], true, seen, report);
            CountSlot(database, rows[i], false, seen, report);
        }

        report.Complete = report.SlotCount == ApprovedSlotCount
            && report.EmptyKeyCount == 0
            && report.DuplicateKeyCount == 0
            && report.UnknownKeyCount == 0
            && report.MissingFileCount == 0
            && report.LoadedCount == ApprovedSlotCount;
        return report;
    }

    static void CountSlot(
        CsvDatabase database,
        ResultFeedbackRow row,
        bool success,
        Dictionary<string, string> seen,
        FeedbackImageAuditReport report)
    {
        report.SlotCount++;
        string key = FeedbackImageResolver.ImageKey(row, success);
        if (key.Length == 0)
        {
            report.EmptyKeyCount++;
            return;
        }

        string owner = row.FeedbackID + (success ? ":success" : ":fail");
        if (seen.ContainsKey(key))
            report.DuplicateKeyCount++;
        else
            seen.Add(key, owner);

        AssetRow asset;
        if (database == null || !database.TryGetAssetByImageKey(key, out asset) || asset == null)
        {
            report.UnknownKeyCount++;
            return;
        }

        if (V02FeedbackSprites.Load(asset.FilePath) == null)
            report.MissingFileCount++;
        else
            report.LoadedCount++;
    }
}
