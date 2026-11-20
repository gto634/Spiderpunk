using UnityEngine;
using UnityEngine.UIElements;

public class Vanishing : MonoBehaviour
{
    [SerializeField] Renderer _renderer;
    [SerializeField] string _param = "_Threshold";

    private void Start()
    {
        Despawn();
    }
    async void Despawn()
    {
        float start = 0.5f;
        float end = 1f;
        float startTime = Time.time;
        float duration = 3f;

        _renderer.material.SetFloat(_param, start);

        while (Time.time < startTime + duration)
        {
            float value = Mathf.Lerp(start, end, (Time.time - startTime) / duration);
            _renderer.material.SetFloat(_param, value);

            await Awaitable.NextFrameAsync();
        }

        _renderer.material.SetFloat(_param, end);

        RecursiveDestroy(gameObject);
        
    }

    void RecursiveDestroy(GameObject go)
    {
        if(go.transform.parent != null)
        {
            RecursiveDestroy(go.transform.parent.gameObject);
        }

        Destroy(go);
    }

}


