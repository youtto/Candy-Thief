using System.Collections.Generic;
using UnityEngine;

// 카드에 넘기는 표시용 값. 프로필이나 가게 테이블에는 저장하지 않는다.
public sealed class V02ShopCardData
{
    public string ShopId = string.Empty;
    public string ShopName = string.Empty;
    public string Distance = string.Empty;
    public string Scale = string.Empty;
    public string Security = string.Empty;
    public string Goods = string.Empty;
    public int EnergyCost;
    public string Rumor0 = string.Empty;
    public string Rumor1 = string.Empty;
    public Sprite Background;

    // 위험도 규칙이 없으면 false. 규칙이 생기면 이 값만 채우면 칸이 켜진다.
    public bool HasRisk;
    public int RiskSteps;

    public static V02ShopCardData FromShop(ShopRow shop, IReadOnlyList<string> rumorLines, Sprite background)
    {
        var data = new V02ShopCardData();
        if (shop == null)
            return data;

        data.ShopId = shop.ShopID ?? string.Empty;
        data.ShopName = shop.ShopName ?? string.Empty;
        data.Distance = V02OperationPrepCard.DistanceLabel(shop.DistanceGrade);
        data.Scale = V02OperationPrepCard.ScaleLabel(shop.ScaleGrade);
        data.Security = V02OperationPrepCard.SecurityLabel(shop.SecurityGrade);
        data.Goods = V02OperationPrepCard.GoodsLabel(shop.GoodsGrade);
        data.EnergyCost = shop.EnergyCost;
        data.Background = background;
        V02OperationPrepCard.SplitRumors(rumorLines, out data.Rumor0, out data.Rumor1);
        return data;
    }
}
