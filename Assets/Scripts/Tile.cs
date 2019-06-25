using UnityEngine;

[System.Serializable]
public class Tile
{
    public GameObject gameObject;
    public Matrix4x4 transform;

    public Tile(GameObject obj, Matrix4x4 tr)
    {
        gameObject = obj;
        transform = tr;
    }
}
