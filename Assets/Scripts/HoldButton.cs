using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType { Thrust, Left, Right }
    [SerializeField] ButtonType buttonType;

    Movement playerMovement;

    public void SetType(ButtonType type) => buttonType = type;

    void Start()
    {
        playerMovement = FindFirstObjectByType<Movement>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
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
        if (playerMovement == null) return;

        if (buttonType == ButtonType.Thrust) playerMovement.SetThrustUI(false);
        else if (buttonType == ButtonType.Left) playerMovement.SetRotateLeftUI(false);
        else if (buttonType == ButtonType.Right) playerMovement.SetRotateRightUI(false);
    }
}
