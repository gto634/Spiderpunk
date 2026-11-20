using UnityEngine;

public class RotateBonePlayer : MonoBehaviour
{
    [SerializeField] ShoulderCameraSystem shoulderCameraSystem;
    [SerializeField] LateRotateSystem lateRotateSystem;
    [SerializeField] CompetenceOnePlayer competenceOnePlayer;

    void OnEnable()
    {
        if (shoulderCameraSystem == null || lateRotateSystem == null)
        {
            Debug.LogError("RotateBonePlayer: assign the shoulder camera and late rotation systems.", this);
            enabled = false;
            return;
        }

        shoulderCameraSystem.OnShoulderRotation += ApplyShoulderRotation;
    }

    void OnDisable()
    {
        if (shoulderCameraSystem != null)
            shoulderCameraSystem.OnShoulderRotation -= ApplyShoulderRotation;
        if (lateRotateSystem != null)
            lateRotateSystem.SetWorldRotationOffset(Quaternion.identity);
    }

    void ApplyShoulderRotation(Quaternion rotation)
    {
        bool playingCompetenceOne = competenceOnePlayer != null && competenceOnePlayer.IsPlayingAnimation;
        lateRotateSystem.SetWorldRotationOffset(playingCompetenceOne ? Quaternion.identity : rotation);
    }
}
