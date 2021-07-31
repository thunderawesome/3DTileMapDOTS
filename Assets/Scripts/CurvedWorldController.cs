using UnityEngine;

public class CurvedWorldController : MonoBehaviour
{
    [SerializeField] private Material[] curvedSurfaceMats;
    void Start()
    {
        for (int i = 0; i < curvedSurfaceMats.Length; i++)
        {
            curvedSurfaceMats[i].SetFloat("_BendFallOff", 300f);
            curvedSurfaceMats[i].SetFloat("_BendFallOffStr", 2.25f);
        }
       
    }

    void Update()
    {
        for (int i = 0; i < curvedSurfaceMats.Length; i++)
        {
            curvedSurfaceMats[i].SetVector("_BendOrigin", transform.position);
        }
        //curvedSurfaceMat.SetVector("_BendAmount", new Vector3(0, -0.01f, (Mathf.Sin(Time.time) * 0.03f)));
    }
}