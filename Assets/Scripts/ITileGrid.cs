using UnityEngine;

public interface ITileGrid
{   
    /// <summary>
    /// The origin of the Tilemap in cell position.
    /// </summary>
    Vector3Int origin { get; }

    /// <summary>
    /// The size of the Tilemap in cells.
    /// </summary>
    Vector3Int size { get; }
  
    /// <summary>
    /// Returns the boundaries of the Tilemap in local space size.
    /// </summary>
    Bounds localBounds { get; }

    /// <summary>
    /// Returns the boundaries of the Tilemap in cell size.
    /// </summary>
    BoundsInt cellBounds { get; }
   
    /// <summary>
    /// Gets the GameObject used in a Tile given the XYZ coordinates of a cell in the Tilemap.
    /// </summary>
    /// <param name="position">Position of the Tile on the Tilemap.</param>
    /// <returns>GameObject at the XY coordinate.</returns>
    GameObject GetGameObject(Vector3Int position);
   
    /// <summary>
    /// Gets the Tile of type T at the given XYZ coordinates of a cell in the Tilemap.
    /// </summary>
    /// <param name="position">Position of the Tile on the Tilemap.</param>
    /// <returns>placed at the cell.</returns>
    Tile GetTile(Vector3Int position);
    T GetTile<T>(Vector3Int position) where T : Tile;
 
    /// <summary>
    /// Gets the transform matrix of a Tile given the XYZ coordinates of a cell in the Tilemap.
    /// </summary>
    /// <param name="position">Position of the Tile on the Tilemap.</param>
    /// <returns>The transform matrix.</returns>
    Matrix4x4 GetTransformMatrix(Vector3Int position);
 
    /// <summary>
    /// Refreshes a Tile at the given XYZ coordinates of a cell in the :Tilemap.
    /// </summary>
    /// <param name="position">Position of the Tile on the Tilemap.</param>
    void RefreshTile(Vector3Int position);
}