using UnityEngine;

public sealed class EnableBackground : MonoBehaviour
{
    [SerializeField] GameObject background;

    public void Toggle()
    {
        if (background != null)
            background.SetActive(!background.activeSelf);
    }
}
