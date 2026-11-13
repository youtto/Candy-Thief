using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class V02TitleLogin : MonoBehaviour
{
    public event Action<V02PlayerProfile> LoggedIn;

    [SerializeField] TMP_InputField nicknameField;
    [SerializeField] Button startButton;
    [SerializeField] TextMeshProUGUI status;

    V02LocalProfileService profiles;
    bool busy;
    bool csvBlocked;
    bool wired;

    public bool IsBusy { get { return busy; } }

    public void Bind(V02LocalProfileService profileService)
    {
        profiles = profileService;
        Wire();
        PrefillLastNickname();
    }

    public void SetCsvBlocked(bool blocked, string message)
    {
        csvBlocked = blocked;
        if (blocked && !string.IsNullOrEmpty(message))
            SetStatus(message);
    }

    public void TryLogin()
    {
        if (busy)
            return;

        if (csvBlocked)
        {
            SetStatus("데이터 로딩 실패. CSV를 확인하세요.");
            return;
        }

        string error;
        string nickname;
        if (!V02LocalProfileService.TryValidateNickname(nicknameField != null ? nicknameField.text : "", out nickname, out error))
        {
            SetStatus(error);
            return;
        }

        if (profiles == null)
            Bind(V02LocalProfileService.CreateDefault());

        if (profiles == null)
        {
            SetStatus(V02LocalProfileService.SaveFailedMessage);
            return;
        }

        busy = true;
        SetInteractable(false);

        V02PlayerProfile profile;
        if (!profiles.TryLogin(nickname, out profile, out error))
        {
            SetStatus(error);
            busy = false;
            SetInteractable(true);
            return;
        }

        SetStatus("");
        if (LoggedIn != null)
            LoggedIn(profile);
    }

    void Wire()
    {
        if (wired)
            return;

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(TryLogin);
            startButton.onClick.AddListener(TryLogin);
        }

        if (nicknameField != null)
        {
            nicknameField.onSubmit.RemoveListener(OnSubmit);
            nicknameField.onSubmit.AddListener(OnSubmit);
            nicknameField.characterLimit = V02LocalProfileService.MaxNicknameLength;
            nicknameField.lineType = TMP_InputField.LineType.SingleLine;
        }

        wired = true;
    }

    void PrefillLastNickname()
    {
        if (profiles == null)
            return;

        string lastNickname;
        string error = "";
        if (profiles.TryReadLastNickname(out lastNickname, out error))
        {
            if (!string.IsNullOrEmpty(lastNickname) && nicknameField != null)
                nicknameField.text = lastNickname;
            SetStatus("");
        }
        else if (!string.IsNullOrEmpty(error))
        {
            SetStatus(error);
        }
    }

    void OnSubmit(string _)
    {
        TryLogin();
    }

    void SetInteractable(bool interactable)
    {
        if (startButton != null)
            startButton.interactable = interactable;
        if (nicknameField != null)
            nicknameField.interactable = interactable;
    }

    void SetStatus(string message)
    {
        if (status != null)
            status.text = message ?? "";
    }
}
