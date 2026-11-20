using UnityEngine;

public class Web : MonoBehaviour
{
    public void Init(Vector3 normal, Vector3 position)
    {
        transform.rotation = Quaternion.FromToRotation(Vector3.forward, normal);
        transform.position = position + normal * 0.01f;
    }
}
