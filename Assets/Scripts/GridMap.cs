using UnityEngine;

[RequireComponent(typeof(Grid))]
public class GridMap : MonoBehaviour
{
    #region Private Variables

    [SerializeField]
    private GridLayout m_gridLayout = null;

    #endregion

    #region Public Properties

    public GridLayout GridLayout { get => m_gridLayout; }

    #endregion

    #region Private Methods

    private void Reset()
    {
        m_gridLayout = GetComponent<Grid>();
    }

    #endregion

}
