using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class VFXVanishing : MonoBehaviour
{

    [SerializeField] DecalProjector dp;
    [SerializeField] float delay;
    [SerializeField] float fadeDuration;


    private void Awake()
    {
        
    }


    void Start()
    {
        Despawn();
        
    }

    void Update()
    {
        
    }

    async void Despawn()
    {
        await Awaitable.WaitForSecondsAsync(delay);

        float start = 1f;
        float end = 0f;
        float startTime = Time.time;

        while (Time.time < startTime + fadeDuration)
        {
            float value = Mathf.Lerp(start, end, (Time.time - startTime) / fadeDuration);
            if (dp)
                dp.fadeFactor = value;

            await Awaitable.NextFrameAsync();
        }

        for (int i = 0; i > gameObject.transform.childCount; i++)
        {
            Destroy(gameObject.transform.GetChild(i).gameObject);
        }

        Destroy(gameObject);
    }
}
