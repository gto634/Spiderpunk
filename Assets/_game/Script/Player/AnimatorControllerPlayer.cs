using System;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class AnimatorControllerPlayer : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] MovementPlayer movementPlayer;
    [SerializeField] NormalAttackPlayer normalAttackPlayer;
    [SerializeField] CompetenceOnePlayer competenceOnePlayer;
    [SerializeField] LightningBurst competenceOneLightning;
    [SerializeField] AttackCeilingWeb attackCeilingWeb;
    [SerializeField] AttackGroundImpact attackGroundImpact;
    [SerializeField] BrainPlayer brainPlayer;
    [SerializeField] Transform rootMotionTarget;
    [SerializeField] Transform animatedRoot;
    [SerializeField, Min(0f)] float attackBlendDuration = 0.12f;
    [SerializeField, Min(0f)] float locomotionBlendDuration = 0.15f;
    [SerializeField, Min(0f)] float aimBlendDuration = 0.1f;

    const int BaseLayer = 0;
    const int AttackCount = 3;
    const string AimLayerName = "Right Arm";
    const string CompetenceOneStateName = "Competence1";

    static readonly int IsMoving = Animator.StringToHash("isMoving");
    static readonly int IsRunning = Animator.StringToHash("isRunning");
    static readonly int DashPlaybackSpeed = Animator.StringToHash("dashPlaybackSpeed");

    int[] attackStates;
    int currentAttackState;
    int competenceOneState;
    int idleState;
    int walkingState;
    int runningState;
    int dashState;
    float dashClipLength;
    bool isPlayingDash;
    int aimPoseState;
    int shootState;
    int currentAimState;
    int aimLayer = -1;
    Rigidbody characterBody;
    Vector3 pendingRootMotion;
    Vector3 restingRootPosition;

    void Reset()
    {
        animator = GetComponent<Animator>();
        movementPlayer = transform.root.GetComponentInChildren<MovementPlayer>();
        normalAttackPlayer = transform.root.GetComponentInChildren<NormalAttackPlayer>();
        competenceOnePlayer = transform.root.GetComponentInChildren<CompetenceOnePlayer>();
        competenceOneLightning = transform.root.GetComponentInChildren<LightningBurst>();
        brainPlayer = transform.root.GetComponentInChildren<BrainPlayer>();
        rootMotionTarget = transform.root;
    }

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (movementPlayer == null)
            movementPlayer = transform.root.GetComponentInChildren<MovementPlayer>();
        if (normalAttackPlayer == null)
            normalAttackPlayer = transform.root.GetComponentInChildren<NormalAttackPlayer>();
        if (competenceOnePlayer == null)
            competenceOnePlayer = transform.root.GetComponentInChildren<CompetenceOnePlayer>();
        if (competenceOneLightning == null)
            competenceOneLightning = transform.root.GetComponentInChildren<LightningBurst>();
        if (brainPlayer == null)
            brainPlayer = transform.root.GetComponentInChildren<BrainPlayer>();
        if (rootMotionTarget == null)
            rootMotionTarget = transform.root;

        characterBody = rootMotionTarget.GetComponent<Rigidbody>();
        if (animatedRoot != null)
            restingRootPosition = animatedRoot.localPosition;
        if (animator == null || movementPlayer == null)
        {
            Debug.LogError("AnimatorControllerPlayer: assign Animator and Movement Player in the Inspector.", this);
            enabled = false;
            return;
        }

        if (!HasParameter(IsMoving, AnimatorControllerParameterType.Bool) ||
            !HasParameter(IsRunning, AnimatorControllerParameterType.Bool))
        {
            Debug.LogError("AnimatorControllerPlayer: the assigned Animator Controller must define isMoving and isRunning as Bool parameters.", animator);
            enabled = false;
            return;
        }

        string layer = animator.GetLayerName(BaseLayer) + ".";
        idleState = Animator.StringToHash(layer + "Idle");
        walkingState = Animator.StringToHash(layer + "Walking");
        runningState = Animator.StringToHash(layer + "Running");

        if (normalAttackPlayer != null)
            BindAttackStates();
        if (competenceOnePlayer != null)
        {
            int state = Animator.StringToHash(layer + CompetenceOneStateName);
            if (animator.HasState(BaseLayer, state))
                competenceOneState = state;
            else
                Debug.LogError($"AnimatorControllerPlayer: missing {CompetenceOneStateName} state.", animator);
        }
        if (brainPlayer != null)
        {
            BindAimStates();
            int state = Animator.StringToHash(layer + "Dash");
            if (animator.HasState(BaseLayer, state) && HasParameter(DashPlaybackSpeed, AnimatorControllerParameterType.Float))
            {
                AnimationClip clip = Array.Find(animator.runtimeAnimatorController.animationClips, candidate => candidate.name == "Dash");
                if (clip != null && clip.length > 0f)
                {
                    dashState = state;
                    dashClipLength = clip.length;
                }
            }
        }
    }

    bool HasParameter(int hash, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.nameHash == hash && parameter.type == type)
                return true;

        return false;
    }

    void BindAttackStates()
    {
        string layer = animator.GetLayerName(BaseLayer) + ".";
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        var states = new int[AttackCount];
        for (int attack = 0; attack < AttackCount; attack++)
        {
            string name = $"Attack{attack + 1}";
            int hash = Animator.StringToHash(layer + name);
            AnimationClip clip = Array.Find(clips, candidate => candidate.name == name);
            if (clip == null || clip.length <= 0f || !animator.HasState(BaseLayer, hash))
            {
                Debug.LogError($"AnimatorControllerPlayer: missing attack state or clip {name}.", animator);
                return;
            }

            states[attack] = hash;
        }

        attackStates = states;
    }

    void OnEnable()
    {
        if (attackStates != null)
        {
            normalAttackPlayer.OnAttackChanged += HandleAttackChanged;
            SynchronizeAttack();
        }

        if (competenceOneState != 0)
            competenceOnePlayer.OnTriggered += HandleCompetenceOne;

        if (aimPoseState != 0)
        {
            brainPlayer.OnShoot += HandleShoot;
            SynchronizeAim();
        }
    }

    void OnDisable()
    {
        if (normalAttackPlayer != null)
            normalAttackPlayer.OnAttackChanged -= HandleAttackChanged;
        if (competenceOnePlayer != null)
            competenceOnePlayer.OnTriggered -= HandleCompetenceOne;
        if (brainPlayer != null)
            brainPlayer.OnShoot -= HandleShoot;

        pendingRootMotion = Vector3.zero;
        currentAimState = 0;
        if (animator != null && aimLayer >= 0)
            animator.SetLayerWeight(aimLayer, 0f);
        if (isPlayingDash)
        {
            isPlayingDash = false;
            CrossFadeToLocomotion();
        }
        ReturnToLocomotion();
    }

    void Update()
    {
        animator.SetBool(IsMoving, movementPlayer.IsMoving);
        animator.SetBool(IsRunning, movementPlayer.IsRunning);

        if (SynchronizeDash())
            return;

        if (IsPlayingCompetenceOne)
            SynchronizeCompetenceOne();
        else if (attackStates != null)
            SynchronizeAttack();
        if (aimPoseState != 0)
            SynchronizeAim();
    }

    bool SynchronizeDash()
    {
        bool dashing = dashState != 0 && brainPlayer.IsDashing;
        if (dashing == isPlayingDash)
            return dashing;

        isPlayingDash = dashing;
        if (dashing)
        {
            ClearAttackState();
            pendingRootMotion = Vector3.zero;
            currentAimState = 0;
            if (aimLayer >= 0)
                animator.SetLayerWeight(aimLayer, 0f);
            animator.SetFloat(DashPlaybackSpeed, dashClipLength / brainPlayer.DashDuration);
            animator.CrossFadeInFixedTime(dashState, locomotionBlendDuration, BaseLayer, 0f);
        }
        else
            CrossFadeToLocomotion();

        return dashing;
    }

    void HandleAttackChanged(int index)
    {
        if (IsPlayingCompetenceOne || index >= attackStates.Length || !animator.isActiveAndEnabled)
            return;

        PlayAttackState(attackStates[index]);
    }

    bool IsPlayingCompetenceOne => competenceOneState != 0 && currentAttackState == competenceOneState;

    void HandleCompetenceOne()
    {
        if (!animator.isActiveAndEnabled)
            return;

        competenceOnePlayer.IsPlayingAnimation = true;
        PlayAttackState(competenceOneState);
    }

    void SynchronizeCompetenceOne()
    {
        if (!competenceOnePlayer.isActiveAndEnabled)
        {
            ReturnToLocomotion();
            return;
        }

        if (HasFinishedAttackAnimation() && (competenceOneLightning == null || !competenceOneLightning.IsPlaying))
            ReturnToLocomotion();
    }

    void PlayAttackState(int state)
    {
        if (attackCeilingWeb != null)
            attackCeilingWeb.Release();
        currentAttackState = state;
        currentAimState = 0;
        if (aimLayer >= 0)
            animator.SetLayerWeight(aimLayer, 0f);
        animator.CrossFadeInFixedTime(currentAttackState, attackBlendDuration, BaseLayer, 0f);
    }

    public void HitMarker(AnimationEvent marker)
    {
        if (attackStates == null || !marker.isFiredByAnimator || marker.animatorStateInfo.fullPathHash != currentAttackState)
            return;

        if (currentAttackState == attackStates[1] && attackCeilingWeb != null)
        {
            if (marker.stringParameter == "ReleaseWeb")
                attackCeilingWeb.Release();
            else if (string.IsNullOrEmpty(marker.stringParameter))
                attackCeilingWeb.QueueAttachment();
        }
        else if (currentAttackState == attackStates[2] && attackGroundImpact != null)
            attackGroundImpact.QueueImpact();
    }

    void SynchronizeAttack()
    {
        if (!normalAttackPlayer.IsAttacking)
        {
            if (currentAttackState != 0 && HasFinishedAttackAnimation())
                ReturnToLocomotion();
            return;
        }

        int index = normalAttackPlayer.ActiveAttackIndex;
        if (index < attackStates.Length && currentAttackState != attackStates[index])
            HandleAttackChanged(index);
    }

    bool HasFinishedAttackAnimation()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(BaseLayer);
        return state.fullPathHash == currentAttackState && state.normalizedTime >= 1f;
    }

    void OnAnimatorMove()
    {
        if (!isActiveAndEnabled || IsPlayingCompetenceOne || isPlayingDash)
            return;

        if (currentAttackState == 0)
        {
            int state = animator.GetCurrentAnimatorStateInfo(BaseLayer).fullPathHash;
            if (attackStates == null || Array.IndexOf(attackStates, state) < 0)
                return;
        }

        Vector3 offset = animator.deltaPosition;
        if (characterBody != null)
            pendingRootMotion += offset;
        else
            rootMotionTarget.position += offset;
    }

    void LateUpdate()
    {
        if (animatedRoot == null)
            return;

        Vector3 position = animatedRoot.localPosition;
        position.x = restingRootPosition.x;
        position.z = restingRootPosition.z;
        animatedRoot.localPosition = position;
    }

    void FixedUpdate()
    {
        if (characterBody == null || pendingRootMotion == Vector3.zero)
            return;

        characterBody.MovePosition(characterBody.position + pendingRootMotion);
        pendingRootMotion = Vector3.zero;
    }

    void ReturnToLocomotion()
    {
        if (currentAttackState == 0)
            return;
        ClearAttackState();
        CrossFadeToLocomotion();
    }

    void ClearAttackState()
    {
        if (currentAttackState == 0)
            return;
        if (attackCeilingWeb != null)
            attackCeilingWeb.Release();

        if (IsPlayingCompetenceOne && competenceOnePlayer != null)
            competenceOnePlayer.IsPlayingAnimation = false;
        currentAttackState = 0;
    }

    void CrossFadeToLocomotion()
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return;

        int state = idleState;
        if (movementPlayer.IsMoving)
            state = movementPlayer.IsRunning ? runningState : walkingState;

        animator.CrossFadeInFixedTime(state, locomotionBlendDuration, BaseLayer, 0f);
    }

    void BindAimStates()
    {
        aimLayer = animator.GetLayerIndex(AimLayerName);
        if (aimLayer < 0)
        {
            Debug.LogError($"AnimatorControllerPlayer: missing masked {AimLayerName} layer.", animator);
            return;
        }

        string layer = AimLayerName + ".";
        int pose = Animator.StringToHash(layer + "Aim");
        int shot = Animator.StringToHash(layer + "Shoot");
        if (!animator.HasState(aimLayer, pose) || !animator.HasState(aimLayer, shot))
        {
            Debug.LogError("AnimatorControllerPlayer: the Animator Controller must define Aim and Shoot states.", animator);
            return;
        }

        aimPoseState = pose;
        shootState = shot;
    }

    void SynchronizeAim()
    {
        if (currentAttackState != 0 || (normalAttackPlayer != null && normalAttackPlayer.IsAttacking))
        {
            currentAimState = 0;
            animator.SetLayerWeight(aimLayer, 0f);
            return;
        }

        float targetWeight = brainPlayer.IsAiming ? 1f : 0f;
        float weight = aimBlendDuration > 0f
            ? Mathf.MoveTowards(animator.GetLayerWeight(aimLayer), targetWeight, Time.deltaTime / aimBlendDuration)
            : targetWeight;
        animator.SetLayerWeight(aimLayer, weight);

        if (!brainPlayer.IsAiming)
        {
            currentAimState = 0;
            return;
        }

        if (currentAimState == 0)
        {
            PlayAimPose();
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(aimLayer);
        if (currentAimState == shootState && !animator.IsInTransition(aimLayer) &&
            state.fullPathHash == shootState && state.normalizedTime >= 1f)
            PlayAimPose();
    }

    void PlayAimPose()
    {
        currentAimState = aimPoseState;
        animator.CrossFadeInFixedTime(aimPoseState, aimBlendDuration, aimLayer, 0f);
    }

    void HandleShoot()
    {
        if (!animator.isActiveAndEnabled || !brainPlayer.IsAiming || currentAttackState != 0 ||
            (normalAttackPlayer != null && normalAttackPlayer.IsAttacking))
            return;

        currentAimState = shootState;
        animator.CrossFadeInFixedTime(shootState, aimBlendDuration, aimLayer, 0f);
    }
}
