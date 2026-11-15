using UnityEditor;
using UnityEngine;

public static class V02AllSelfTests
{
    [MenuItem("Candy Thief/V02/Run All Self-Tests")]
    public static void RunAll()
    {
        V02RunResultPopupBuilder.BakeIfMissing();
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
        if (ActionHandSelfTest.RunAll(database))
            passed++;
        groups++;
        if (ResultFeedbackSelfTest.RunAll(database))
            passed++;
        groups++;
        if (HudPresentationSelfTest.RunAll(database))
            passed++;
        groups++;
        if (ShopListDrawerSelfTest.RunAll(database))
            passed++;
        groups++;
        if (ShopRumorSelfTest.RunAll(database))
            passed++;
        groups++;
        if (ShopListManagerSelfTest.RunAll(database))
            passed++;
        V02OperationPrepPrefabBuilder.EnsureCurrent();
        groups++;
        if (V02OperationPrepSelfTest.RunAll(database))
            passed++;
        groups++;
        if (V02AbandonSelfTest.RunAll(database))
            passed++;
        groups++;
        if (EscapePrepSelfTest.RunAll(database))
            passed++;
        groups++;
        if (RunOutcomeSelfTest.RunAll(database))
            passed++;
        groups++;
        if (ActionSelectTimerSelfTest.RunAll(database))
            passed++;
        groups++;
        if (TimeoutAutoSelectSelfTest.RunAll(database))
            passed++;
        groups++;
        if (ChoiceInfoChromeSelfTest.RunAll(database))
            passed++;

        if (passed == groups)
            Debug.Log("[V02] 전체 자가 검사 통과: 그룹 " + passed + "/" + groups + " (ResultFeedback 18 + ActionHand 14 + HudPresentation 11 + ShopListDrawer 12 + ShopRumor 17 + ShopListManager 14 + OperationPrep 10 + Abandon 7 + EscapePrep 8 + RunOutcome 8 + ActionSelectTimer 8 + TimeoutAutoSelect 14 + ChoiceInfoChrome 11 포함)");
        else
            Debug.LogError("[V02] 전체 자가 검사 실패: 그룹 " + passed + "/" + groups);
    }
}
