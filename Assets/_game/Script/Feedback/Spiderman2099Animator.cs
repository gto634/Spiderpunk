using UnityEngine;

[DefaultExecutionOrder(-10)]
[DisallowMultipleComponent]
public sealed class Spiderman2099Animator : MonoBehaviour
{
    static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    static readonly int HangingIdle = Animator.StringToHash("Base Layer.Hanging Idle");
    static readonly int GettingHit = Animator.StringToHash("Base Layer.GettingHit");
    static readonly int Dying = Animator.StringToHash("Base Layer.Dying");
    static readonly int Hanging = Animator.StringToHash("Hanging");

    [SerializeField] Animator animator;
    [SerializeField] LifeSystem lifeSystem;
    [SerializeField] ParticleSystem spiderSense;
    [SerializeField] bool hanging;

    Transform head;
    Transform headTip;

    void Awake()
    {
        foreach (Transform bone in animator.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name is "head.x" or "Head")
            {
                head = bone;
                headTip = bone.Find("head.x_end") ?? bone.Find("HeadTop_End");
                break;
            }
        }

        var particles = spiderSense.main;
        particles.simulationSpace = ParticleSystemSimulationSpace.Local;
        if (hanging)
            spiderSense.gameObject.SetActive(false);
        UpdateSpiderSensePosition();
    }

    void Start()
    {
        if (hanging)
        {
            animator.SetBool(Hanging, true);
            animator.Play(HangingIdle, 0, 0f);
        }
    }

    void OnEnable()
    {
        if (hanging || lifeSystem == null)
            return;
        lifeSystem.OnDamage += PlayHit;
        lifeSystem.OnDeath += PlayDeath;
        lifeSystem.OnHeal += ResumeAfterRevive;
    }

    void OnDisable()
    {
        if (hanging || lifeSystem == null)
            return;
        lifeSystem.OnDamage -= PlayHit;
        lifeSystem.OnDeath -= PlayDeath;
        lifeSystem.OnHeal -= ResumeAfterRevive;
    }

    void LateUpdate() => UpdateSpiderSensePosition();

    void UpdateSpiderSensePosition()
    {
        if (head == null || !spiderSense.gameObject.activeSelf)
            return;

        spiderSense.transform.position = headTip != null
            ? Vector3.Lerp(head.position, headTip.position, 0.75f)
            : head.position;
    }

    void PlayHit(int previousHealth, int remainingHealth)
    {
        if (remainingHealth > 0 && remainingHealth < previousHealth)
            animator.CrossFadeInFixedTime(GettingHit, 0.07f, 0, 0f);
    }

    void PlayDeath()
    {
        spiderSense.gameObject.SetActive(false);
        animator.CrossFadeInFixedTime(Dying, 0.12f, 0, 0f);
    }

    void ResumeAfterRevive(int previousHealth, int health)
    {
        if (previousHealth == 0 && health > 0)
        {
            spiderSense.gameObject.SetActive(true);
            animator.Play(animator.GetBool(Hanging) ? HangingIdle : Idle, 0, 0f);
        }
    }
}
