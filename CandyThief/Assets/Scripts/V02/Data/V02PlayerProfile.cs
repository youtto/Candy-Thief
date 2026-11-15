using System;

// D-026 가게 목록 저장 필드. 스키마 ID는 그대로(candythief.v02.profiles.v1) 두고 프로필에 필드만 추가한다.
[Serializable]
public sealed class V02ShopRumorPair
{
    public string shopId = "";
    public string rumorId = "";
}

[Serializable]
public sealed class V02ShopListState
{
    public string listId = "";
    public string[] shopIds = Array.Empty<string>();
    public V02ShopRumorPair[] rumors = Array.Empty<V02ShopRumorPair>();
    public int freeRerollUsed;
    public int totalRerollUsed;
    // 직전 목록(교체된 목록)의 가게 ID. 기록용이며 다음 추첨 제외는 현재 목록을 기준으로 한다.
    public string[] previousShopIds = Array.Empty<string>();

    public V02ShopListState Clone()
    {
        var copy = new V02ShopListState
        {
            listId = listId ?? "",
            shopIds = shopIds == null ? Array.Empty<string>() : (string[])shopIds.Clone(),
            freeRerollUsed = freeRerollUsed,
            totalRerollUsed = totalRerollUsed,
            previousShopIds = previousShopIds == null ? Array.Empty<string>() : (string[])previousShopIds.Clone()
        };
        int count = rumors == null ? 0 : rumors.Length;
        copy.rumors = new V02ShopRumorPair[count];
        for (int i = 0; i < count; i++)
        {
            V02ShopRumorPair pair = rumors[i];
            copy.rumors[i] = new V02ShopRumorPair
            {
                shopId = pair == null ? "" : pair.shopId,
                rumorId = pair == null ? "" : pair.rumorId
            };
        }

        return copy;
    }
}

[Serializable]
public sealed class V02PlayerProfile
{
    public string profileId;
    public string nickname;
    public string createdAt;
    public string lastLoginAt;
    public int energy = V02LocalProfileService.DefaultEnergy;
    public int gold;
    public int candy;
    public string energyUpdatedAt;

    // OP-DEV-05 (D-026). 필드가 없는 기존 저장본은 빈 목록/빈 ID로 읽힌다.
    public V02ShopListState shopList = new V02ShopListState();
    public string activeRunId = "";
    public string settledRunId = "";
}

[Serializable]
public sealed class V02ProfileStoreData
{
    public const string SchemaId = "candythief.v02.profiles.v1";

    public string schema = SchemaId;
    public string lastNickname = "";
    public string currentProfileId = "";
    public V02PlayerProfile[] profiles = Array.Empty<V02PlayerProfile>();
}
