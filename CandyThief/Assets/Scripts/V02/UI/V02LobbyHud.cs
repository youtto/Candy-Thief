using TMPro;
using UnityEngine;

/// <summary>
/// Binds D-020 lobby HUD numbers. Does not grant gold or candy.
/// </summary>
public sealed class V02LobbyHud : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nicknameText;
    [SerializeField] TextMeshProUGUI energyText;
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI candyText;

    V02LocalProfileService profiles;
    float nextRefreshAt;

    public void Configure(
        TextMeshProUGUI nickname,
        TextMeshProUGUI energy,
        TextMeshProUGUI gold,
        TextMeshProUGUI candy)
    {
        nicknameText = nickname;
        energyText = energy;
        goldText = gold;
        candyText = candy;
    }

    public void Bind(V02LocalProfileService profileService)
    {
        profiles = profileService;
        if (profiles != null && profiles.ApplyEnergyRegen())
        {
            string error;
            profiles.TrySaveCurrent(out error);
        }

        Refresh();
        nextRefreshAt = Time.unscaledTime + 1f;
    }

    public void BindDisplayOnly(V02PlayerProfile profile)
    {
        profiles = null;
        ApplyProfile(profile);
    }

    public void Refresh()
    {
        V02PlayerProfile profile = profiles != null ? profiles.CurrentProfile : null;
        ApplyProfile(profile);
    }

    void ApplyProfile(V02PlayerProfile profile)
    {
        if (nicknameText != null)
            nicknameText.text = profile != null && !string.IsNullOrEmpty(profile.nickname)
                ? profile.nickname
                : "닉네임";
        if (energyText != null)
        {
            int energy = profile != null ? profile.energy : V02LocalProfileService.DefaultEnergy;
            energyText.text = V02LocalProfileService.FormatEnergy(energy, V02LocalProfileService.MaxEnergy);
        }

        if (goldText != null)
            goldText.text = V02LocalProfileService.FormatGold(profile != null ? profile.gold : 0);
        if (candyText != null)
            candyText.text = V02LocalProfileService.FormatCandy(profile != null ? profile.candy : 0);
    }

    void Update()
    {
        if (profiles == null || !isActiveAndEnabled)
            return;
        if (Time.unscaledTime < nextRefreshAt)
            return;

        nextRefreshAt = Time.unscaledTime + 1f;
        if (profiles.ApplyEnergyRegen())
        {
            string error;
            profiles.TrySaveCurrent(out error);
        }

        Refresh();
    }
}
