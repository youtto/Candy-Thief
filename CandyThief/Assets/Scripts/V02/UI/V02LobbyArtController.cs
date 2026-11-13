using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds lobby Background / Thief images. Does not overwrite RectTransform layout.
/// Pivot on Thief should stay at the feet so idle breath/sway keeps soles planted.
/// </summary>
[DefaultExecutionOrder(-20)]
public sealed class V02LobbyArtController : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] Image thiefImage;
    [SerializeField] RectTransform thiefRect;
    [SerializeField] Sprite lobbyBackground;
    [SerializeField] Sprite thiefIdle;

    Vector2 restAnchoredPosition;

    public Image PlayerImage { get { return thiefImage; } }
    public RectTransform PlayerRect { get { return thiefRect; } }
    public Vector2 RestAnchoredPosition { get { return restAnchoredPosition; } }

    void Awake()
    {
        Bind();
    }

    void Start()
    {
        Bind();
        ApplyMissingSprites();
        CacheRestPosition();
    }

    void Bind()
    {
        Transform lobby = FindLobby();
        if (lobby == null)
            return;

        if (background == null)
        {
            Transform bg = lobby.Find("Background");
            if (bg != null)
                background = bg.GetComponent<Image>();
            if (background == null)
                background = lobby.GetComponent<Image>();
        }

        if (thiefRect == null || thiefImage == null)
        {
            Transform thief = lobby.Find("Thief");
            if (thief == null)
                thief = lobby.Find("Player");
            if (thief != null)
            {
                if (thiefRect == null)
                    thiefRect = thief as RectTransform;
                if (thiefImage == null)
                    thiefImage = thief.GetComponent<Image>();
            }
        }

        if (thiefRect != null)
        {
            var bob = thiefRect.GetComponent<V02IdleBob>();
            if (bob != null)
                bob.enabled = false;
        }
    }

    void ApplyMissingSprites()
    {
        if (background != null && background.sprite == null && lobbyBackground != null)
        {
            background.sprite = lobbyBackground;
            background.color = Color.white;
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.raycastTarget = false;
        }

        if (thiefImage != null && thiefImage.sprite == null && thiefIdle != null)
        {
            thiefImage.sprite = thiefIdle;
            thiefImage.color = Color.white;
            thiefImage.type = Image.Type.Simple;
            thiefImage.preserveAspect = true;
            thiefImage.raycastTarget = false;
        }
    }

    void CacheRestPosition()
    {
        if (thiefRect != null)
            restAnchoredPosition = thiefRect.anchoredPosition;
    }

    Transform FindLobby()
    {
        Transform lobby = transform.Find("TitleCanvas/SafeArea/Lobby");
        if (lobby != null)
            return lobby;

        lobby = transform.Find("Canvas/SafeArea/Lobby");
        if (lobby != null)
            return lobby;

        var titleCanvas = GameObject.Find("TitleCanvas");
        if (titleCanvas != null)
            return titleCanvas.transform.Find("SafeArea/Lobby");

        return null;
    }
}
