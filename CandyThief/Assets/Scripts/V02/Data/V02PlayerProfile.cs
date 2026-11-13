using System;

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
