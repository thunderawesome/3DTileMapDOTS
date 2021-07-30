using UnityEngine;

public class Bending : MonoBehaviour
{
    public Material grassMaterial;

    private void Update()
    {
        grassMaterial.SetVector("_Position", transform.position);
    }
}