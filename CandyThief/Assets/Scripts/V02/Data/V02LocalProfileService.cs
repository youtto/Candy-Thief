using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class V02LocalProfileService
{
    public const int MinNicknameLength = 2;
    public const int MaxNicknameLength = 12;
    public const string EmptyNicknameMessage = "닉네임을 입력해 주세요.";
    public const string LengthMessage = "닉네임은 2~12자로 입력해 주세요.";
    public const string CharsetMessage = "한글, 영문, 숫자만 사용할 수 있어요.";
    public const string SaveFailedMessage = "프로필을 저장하지 못했어요. 다시 시도해 주세요.";
    public const string CorruptMessage = "프로필을 불러오지 못했어요. 다시 시도해 주세요.";
    public const int DefaultEnergy = 10;
    public const int MaxEnergy = 20;
    public const int EnergyRegenMinutes = 10;

    readonly string filePath;

    public V02PlayerProfile CurrentProfile { get; private set; }
    public string LastNickname { get; private set; }

    public V02LocalProfileService(string filePath)
    {
        this.filePath = filePath;
        LastNickname = "";
    }

    public static V02LocalProfileService CreateDefault()
    {
        string dir = Path.Combine(Application.persistentDataPath, "CandyThief");
        return new V02LocalProfileService(Path.Combine(dir, "profiles.json"));
    }

    public static bool TryValidateNickname(string raw, out string nickname, out string error)
    {
        nickname = raw == null ? "" : raw.Trim();
        if (nickname.Length == 0)
        {
            error = EmptyNicknameMessage;
            return false;
        }

        if (nickname.Length < MinNicknameLength || nickname.Length > MaxNicknameLength)
        {
            error = LengthMessage;
            return false;
        }

        for (int i = 0; i < nickname.Length; i++)
        {
            if (!IsAllowedNicknameChar(nickname[i]))
            {
                error = CharsetMessage;
                return false;
            }
        }

        error = "";
        return true;
    }

    public static string NicknameKey(string nickname)
    {
        return nickname == null ? "" : nickname.Trim().ToLowerInvariant();
    }

    public bool TryLogin(string rawNickname, out V02PlayerProfile profile, out string error)
    {
        profile = null;
        if (!TryValidateNickname(rawNickname, out string nickname, out error))
            return false;

        V02ProfileStoreData store;
        if (!TryReadStore(out store, out error))
            return false;

        V02PlayerProfile existing = FindByNickname(store, nickname);
        string now = NowIso();
        if (existing != null)
        {
            existing.lastLoginAt = now;
            profile = existing;
        }
        else
        {
            profile = new V02PlayerProfile
            {
                profileId = Guid.NewGuid().ToString("N"),
                nickname = nickname,
                createdAt = now,
                lastLoginAt = now,
                energy = DefaultEnergy,
                gold = 0,
                candy = 0,
                energyUpdatedAt = now
            };
            store.profiles = Append(store.profiles, profile);
        }

        EnsureEconomyDefaults(profile, now);
        ApplyEnergyRegen(profile, DateTime.UtcNow);

        store.lastNickname = profile.nickname;
        store.currentProfileId = profile.profileId;
        if (!TryWriteStore(store, out error))
        {
            profile = null;
            return false;
        }

        CurrentProfile = profile;
        LastNickname = profile.nickname;
        return true;
    }

    public bool TryReadLastNickname(out string nickname, out string error)
    {
        nickname = "";
        V02ProfileStoreData store;
        if (!TryReadStore(out store, out error))
            return false;

        LastNickname = store.lastNickname ?? "";
        nickname = LastNickname;
        error = "";
        return true;
    }

    public bool ApplyEnergyRegen()
    {
        if (CurrentProfile == null)
            return false;
        return ApplyEnergyRegen(CurrentProfile, DateTime.UtcNow);
    }

    public bool TryPeek(string nickname, out V02PlayerProfile profile, out string error)
    {
        profile = null;
        V02ProfileStoreData store;
        if (!TryReadStore(out store, out error))
            return false;

        V02PlayerProfile existing = FindByNickname(store, nickname);
        if (existing == null)
        {
            error = "";
            return false;
        }

        profile = CloneProfile(existing);
        ApplyEnergyRegen(profile, DateTime.UtcNow);
        error = "";
        return true;
    }

    static V02PlayerProfile CloneProfile(V02PlayerProfile source)
    {
        var copy = new V02PlayerProfile();
        CopyProfileFields(source, copy);
        return copy;
    }

    public bool TrySaveCurrent(out string error)
    {
        error = "";
        if (CurrentProfile == null)
        {
            error = SaveFailedMessage;
            return false;
        }

        V02ProfileStoreData store;
        if (!TryReadStore(out store, out error))
            return false;

        V02PlayerProfile existing = FindByNickname(store, CurrentProfile.nickname);
        if (existing == null)
            store.profiles = Append(store.profiles, CurrentProfile);
        else
            CopyProfileFields(CurrentProfile, existing);

        store.lastNickname = CurrentProfile.nickname;
        store.currentProfileId = CurrentProfile.profileId;
        if (!TryWriteStore(store, out error))
            return false;

        if (existing != null)
            CurrentProfile = existing;
        return true;
    }

    public static string FormatGold(int value)
    {
        return FormatCount(value, 9999);
    }

    public static string FormatCandy(int value)
    {
        return FormatCount(value, 999);
    }

    public static string FormatEnergy(int current, int max)
    {
        if (current < 0)
            current = 0;
        if (max < 1)
            max = MaxEnergy;
        return current.ToString(CultureInfo.InvariantCulture)
            + "/"
            + max.ToString(CultureInfo.InvariantCulture);
    }

    static void EnsureEconomyDefaults(V02PlayerProfile profile, string nowIso)
    {
        if (profile == null)
            return;

        if (string.IsNullOrEmpty(profile.energyUpdatedAt))
        {
            if (profile.energy <= 0)
                profile.energy = DefaultEnergy;
            profile.energyUpdatedAt = nowIso;
        }

        if (profile.energy > MaxEnergy)
            profile.energy = MaxEnergy;
        if (profile.gold < 0)
            profile.gold = 0;
        if (profile.candy < 0)
            profile.candy = 0;
    }

    static bool ApplyEnergyRegen(V02PlayerProfile profile, DateTime utcNow)
    {
        if (profile == null)
            return false;

        DateTime now = utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime();
        string nowIso = now.ToString("o");
        EnsureEconomyDefaults(profile, nowIso);

        DateTime last;
        if (!DateTime.TryParse(profile.energyUpdatedAt, null, DateTimeStyles.RoundtripKind, out last))
            last = now;
        if (last.Kind == DateTimeKind.Unspecified)
            last = DateTime.SpecifyKind(last, DateTimeKind.Utc);
        last = last.ToUniversalTime();

        if (profile.energy >= MaxEnergy)
        {
            if (profile.energy == MaxEnergy)
                return false;

            profile.energy = MaxEnergy;
            profile.energyUpdatedAt = nowIso;
            return true;
        }

        double minutes = (now - last).TotalMinutes;
        if (minutes < EnergyRegenMinutes)
            return false;

        int ticks = (int)(minutes / EnergyRegenMinutes);
        if (ticks <= 0)
            return false;

        int room = MaxEnergy - profile.energy;
        int gained = ticks < room ? ticks : room;
        if (gained <= 0)
            return false;

        profile.energy += gained;
        if (profile.energy >= MaxEnergy)
        {
            profile.energy = MaxEnergy;
            profile.energyUpdatedAt = nowIso;
        }
        else
        {
            profile.energyUpdatedAt = last.AddMinutes(gained * EnergyRegenMinutes).ToString("o");
        }

        return true;
    }

    static string FormatCount(long value, int fullUntil)
    {
        if (value < 0)
            value = 0;
        if (value <= fullUntil)
            return value.ToString(CultureInfo.InvariantCulture);

        string[] suffixes = { "", "K", "M", "B" };
        int unit = 0;
        double scaled = value;
        while (scaled >= 1000d && unit < 3)
        {
            scaled /= 1000d;
            unit++;
        }

        double floored = Math.Floor(scaled * 10d) / 10d;
        if (floored >= 1000d && unit < 3)
        {
            floored /= 1000d;
            unit++;
            floored = Math.Floor(floored * 10d) / 10d;
        }

        if (Math.Abs(floored - Math.Floor(floored)) < 0.05d)
            return ((long)Math.Floor(floored)).ToString(CultureInfo.InvariantCulture) + suffixes[unit];

        return floored.ToString("0.0", CultureInfo.InvariantCulture) + suffixes[unit];
    }

    static void CopyProfileFields(V02PlayerProfile source, V02PlayerProfile dest)
    {
        dest.profileId = source.profileId;
        dest.nickname = source.nickname;
        dest.createdAt = source.createdAt;
        dest.lastLoginAt = source.lastLoginAt;
        dest.energy = source.energy;
        dest.gold = source.gold;
        dest.candy = source.candy;
        dest.energyUpdatedAt = source.energyUpdatedAt;
        dest.shopList = source.shopList != null ? source.shopList.Clone() : new V02ShopListState();
        dest.activeRunId = source.activeRunId ?? "";
        dest.settledRunId = source.settledRunId ?? "";
    }

    bool TryReadStore(out V02ProfileStoreData store, out string error)
    {
        store = new V02ProfileStoreData();
        error = "";
        if (!File.Exists(filePath))
            return true;

        string json;
        try
        {
            json = File.ReadAllText(filePath, Encoding.UTF8);
        }
        catch (Exception exception)
        {
            Debug.LogError("[V02] 프로필 파일을 읽지 못했습니다: " + exception.Message);
            error = CorruptMessage;
            return false;
        }

        if (string.IsNullOrWhiteSpace(json))
            return true;

        if (!LooksLikeStore(json))
        {
            Debug.LogError("[V02] 프로필 JSON이 손상되었습니다: " + filePath);
            error = CorruptMessage;
            store = null;
            return false;
        }

        try
        {
            store = JsonUtility.FromJson<V02ProfileStoreData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError("[V02] 프로필 JSON이 손상되었습니다: " + exception.Message);
            error = CorruptMessage;
            store = null;
            return false;
        }

        if (store == null)
        {
            error = CorruptMessage;
            return false;
        }

        if (!string.IsNullOrEmpty(store.schema) && store.schema != V02ProfileStoreData.SchemaId)
        {
            Debug.LogError("[V02] 프로필 schema가 지원되지 않습니다: " + store.schema);
            error = CorruptMessage;
            store = null;
            return false;
        }

        if (store.profiles == null)
            store.profiles = Array.Empty<V02PlayerProfile>();
        if (store.lastNickname == null)
            store.lastNickname = "";
        return true;
    }

    bool TryWriteStore(V02ProfileStoreData store, out string error)
    {
        error = "";
        string json = JsonUtility.ToJson(store, true);
        string directory = Path.GetDirectoryName(filePath);
        string tempPath = filePath + ".tmp";
        try
        {
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(tempPath, json, new UTF8Encoding(false));
            File.Copy(tempPath, filePath, true);
            File.Delete(tempPath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("[V02] 프로필 저장 실패: " + exception.Message);
            error = SaveFailedMessage;
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
            }

            return false;
        }
    }

    static V02PlayerProfile FindByNickname(V02ProfileStoreData store, string nickname)
    {
        string key = NicknameKey(nickname);
        if (store.profiles == null)
            return null;

        for (int i = 0; i < store.profiles.Length; i++)
        {
            V02PlayerProfile row = store.profiles[i];
            if (row != null && NicknameKey(row.nickname) == key)
                return row;
        }

        return null;
    }

    static V02PlayerProfile[] Append(V02PlayerProfile[] source, V02PlayerProfile added)
    {
        int length = source == null ? 0 : source.Length;
        var next = new V02PlayerProfile[length + 1];
        if (length > 0)
            Array.Copy(source, next, length);
        next[length] = added;
        return next;
    }

    static bool IsAllowedNicknameChar(char value)
    {
        if (value >= '0' && value <= '9')
            return true;
        if (value >= 'A' && value <= 'Z')
            return true;
        if (value >= 'a' && value <= 'z')
            return true;
        if (value >= '\uAC00' && value <= '\uD7A3')
            return true;
        if (value >= '\u1100' && value <= '\u11FF')
            return true;
        if (value >= '\u3131' && value <= '\u318E')
            return true;
        return false;
    }

    static string NowIso()
    {
        return DateTime.UtcNow.ToString("o");
    }

    static bool LooksLikeStore(string json)
    {
        string trimmed = json.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '{' || trimmed[trimmed.Length - 1] != '}')
            return false;

        return trimmed.IndexOf("\"profiles\"", StringComparison.Ordinal) >= 0
            || trimmed.IndexOf("\"lastNickname\"", StringComparison.Ordinal) >= 0
            || trimmed.IndexOf("\"schema\"", StringComparison.Ordinal) >= 0;
    }
}
