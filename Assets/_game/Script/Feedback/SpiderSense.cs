using UnityEngine;

public class SpiderSense : MonoBehaviour
{
    [SerializeField] private SpiderSenseOrientation _sso;
    [SerializeField] private ShakeSystem _shakeSystem;
    [SerializeField, Min(0f)] private float _cooldown = 2f;

    private double _nextPlayTime;

    public void Play()
    {
        double now = Time.timeAsDouble;
        if (!isActiveAndEnabled || _sso == null || !_sso.isActiveAndEnabled || now < _nextPlayTime)
            return;

        _nextPlayTime = now + _cooldown;
        _sso.Stop();
        _sso.Play();
        if (_shakeSystem != null)
            _shakeSystem.Play();
    }

    private void OnDisable()
    {
        if (_sso != null)
            _sso.Stop();
    }
}
