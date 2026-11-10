using UnityEditor;
using UnityEngine;

public static class V02AllSelfTests
{
    [MenuItem("Candy Thief/V02/Run All Self-Tests")]
    public static void RunAll()
    {
        CsvDatabase database = CsvLoadRunner.LoadAndReport();
        if (database == null || database.HasErrors)
        {
            Debug.LogError("[V02] 전체 자가 검사 중단: CSV 로딩 실패");
            return;
        }

        int groups = 0;
        int passed = 0;
        groups++;
        if (CsvLoadRunner.RunValidationSelfTest())
            passed++;
        groups++;
        if (RunStateSelfTest.RunAll(database))
            passed++;
        groups++;
        if (ActionResolverSelfTest.RunAll(database))
            passed++;
        groups++;
        if (EventResolverSelfTest.RunAll(database))
            passed++;
        groups++;
        if (PresentedChoiceSelfTest.RunAll(database))
            passed++;
        groups++;
        if (ResultFeedbackSelfTest.RunAll(database))
            passed++;
        groups++;
        if (HudPresentationSelfTest.RunAll(database))
            passed++;

        if (passed == groups)
            Debug.Log("[V02] 전체 자가 검사 통과: 그룹 " + passed + "/" + groups + " (ResultFeedback 14 + HudPresentation 6 포함)");
        else
            Debug.LogError("[V02] 전체 자가 검사 실패: 그룹 " + passed + "/" + groups);
    }
}
