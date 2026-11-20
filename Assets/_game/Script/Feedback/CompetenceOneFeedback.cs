using UnityEngine;

public class CompetenceOneFeedback : MonoBehaviour
{
    [SerializeField] CompetenceOnePlayer competenceOne;
    [SerializeField] LightningBurst lightningBurst;

    void OnEnable()
    {
        if (competenceOne == null || lightningBurst == null)
        {
            Debug.LogError("CompetenceOneFeedback: assign Competence One and its lightning group.", this);
            enabled = false;
            return;
        }
        competenceOne.OnTriggered += lightningBurst.Play;
    }

    void OnDisable()
    {
        if (competenceOne != null && lightningBurst != null)
            competenceOne.OnTriggered -= lightningBurst.Play;
    }
}

