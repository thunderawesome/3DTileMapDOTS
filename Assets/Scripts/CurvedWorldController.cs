using UnityEngine;

public class CurvedWorldController : MonoBehaviour
{
    [SerializeField]
    private Vector3 _bendAmount = Vector3.zero;
    [SerializeField]
    private float _bendFallOff = 10f;
    [SerializeField]
    private float _bendFallOffStr = 2.25f;

    [SerializeField] private Material[] curvedSurfaceMats;
    void Start()
    {
        for (int i = 0; i < curvedSurfaceMats.Length; i++)
        {
            curvedSurfaceMats[i].SetFloat("_BendFallOff", 10f);
            curvedSurfaceMats[i].SetFloat("_BendFallOffStr", 2.25f);
        }
       
    }

    void Update()
    {
        for (int i = 0; i < curvedSurfaceMats.Length; i++)
        {
            curvedSurfaceMats[i].SetVector("_BendOrigin", transform.position);
            curvedSurfaceMats[i].SetVector("_BendAmount", _bendAmount);
            curvedSurfaceMats[i].SetFloat("_BendFallOff", _bendFallOff);
            curvedSurfaceMats[i].SetFloat("_BendFallOffStr", _bendFallOffStr);
        }
        //curvedSurfaceMat.SetVector("_BendAmount", new Vector3(0, -0.01f, (Mathf.Sin(Time.time) * 0.03f)));
    }

    private void OnDisable()
    {
        for (int i = 0; i < curvedSurfaceMats.Length; i++)
        {
            curvedSurfaceMats[i].SetVector("_BendOrigin", Vector3.zero);
            curvedSurfaceMats[i].SetFloat("_BendFallOff", _bendFallOff);
            curvedSurfaceMats[i].SetFloat("_BendFallOffStr", _bendFallOffStr);
        }
    }
}