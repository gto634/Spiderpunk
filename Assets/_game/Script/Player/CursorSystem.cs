using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorSystem : MonoBehaviour
{
    [SerializeField] BrainPlayer brainPlayer;
    [SerializeField] Texture2D cursorTexture;
    [SerializeField, Min(1f)] float sizeInPixels = 32f;
    [SerializeField, OnValueChanged(nameof(ApplyOffset))]
    [Tooltip("Offset from screen center in pixels. Positive X moves right and positive Y moves up.")]
    Vector2 offsetInPixels;

    Canvas cursorCanvas;
    RectTransform reticleTransform;
    CursorLockMode previousLockState;
    bool previousVisibility;

    void Awake()
    {
        var overlay = new GameObject("Aim Cursor Canvas", typeof(Canvas));
        SceneManager.MoveGameObjectToScene(overlay, gameObject.scene);
        cursorCanvas = overlay.GetComponent<Canvas>();
        cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cursorCanvas.enabled = false;

        var reticle = new GameObject("Aim Cursor", typeof(RectTransform));
        var image = reticle.AddComponent<UnityEngine.UI.RawImage>();
        reticleTransform = image.rectTransform;
        reticleTransform.SetParent(overlay.transform, false);
        reticleTransform.anchorMin = reticleTransform.anchorMax = new Vector2(0.5f, 0.5f);
        reticleTransform.sizeDelta = Vector2.one * sizeInPixels;
        ApplyOffset();
        image.texture = cursorTexture;
        image.raycastTarget = false;
    }

    void OnEnable()
    {
        previousLockState = Cursor.lockState;
        previousVisibility = Cursor.visible;
        LockCursor();
        brainPlayer.OnAimingChanged += ShowReticle;
        ShowReticle(brainPlayer.IsAiming);
    }

    void ShowReticle(bool aiming) => cursorCanvas.enabled = aiming;

    void ApplyOffset()
    {
        if (reticleTransform != null)
            reticleTransform.anchoredPosition = offsetInPixels;
    }

    void OnApplicationFocus(bool focused)
    {
        if (focused && isActiveAndEnabled)
            LockCursor();
    }

    static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        if (brainPlayer != null)
            brainPlayer.OnAimingChanged -= ShowReticle;
        if (cursorCanvas != null)
            cursorCanvas.enabled = false;
        Cursor.lockState = previousLockState;
        Cursor.visible = previousVisibility;
    }

    void OnDestroy()
    {
        if (cursorCanvas != null)
            Destroy(cursorCanvas.gameObject);
    }
}
