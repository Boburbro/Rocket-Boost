using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType { Thrust, Left, Right }
    [SerializeField] ButtonType buttonType;

    Movement playerMovement;

    // Bosilganda rang va o'lcham o'zgarishi uchun (ixtiyoriy)
    Image fill;
    Color normalColor, pressedColor;

    public void SetType(ButtonType type) => buttonType = type;

    public void SetVisual(Image image, Color normal, Color pressed)
    {
        fill = image;
        normalColor = normal;
        pressedColor = pressed;
    }

    void ShowPressed(bool isPressed)
    {
        transform.localScale = Vector3.one * (isPressed ? 0.92f : 1f);
        if (fill != null) fill.color = isPressed ? pressedColor : normalColor;
    }

    void Start()
    {
        playerMovement = FindFirstObjectByType<Movement>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ShowPressed(true);
        if (playerMovement == null) playerMovement = FindFirstObjectByType<Movement>();
        if (playerMovement == null) return;

        if (buttonType == ButtonType.Thrust) playerMovement.SetThrustUI(true);
        else if (buttonType == ButtonType.Left) playerMovement.SetRotateLeftUI(true);
        else if (buttonType == ButtonType.Right) playerMovement.SetRotateRightUI(true);
    }

    // Tugma o'chib/yo'qolib qolsa (sahna qayta yuklanishi) bosilgan holat qolib ketmasin
    void OnDisable()
    {
        Release();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    void Release()
    {
        ShowPressed(false);
        if (playerMovement == null) return;

        if (buttonType == ButtonType.Thrust) playerMovement.SetThrustUI(false);
        else if (buttonType == ButtonType.Left) playerMovement.SetRotateLeftUI(false);
        else if (buttonType == ButtonType.Right) playerMovement.SetRotateRightUI(false);
    }
}
