using UnityEngine;

public class BuildGrid : MonoBehaviour
{
    [SerializeField] private int rows = 45;
    [SerializeField] private int columns = 45;
    [SerializeField] private float cellSize = 4f;

    public int Rows => rows;
    public int Columns => columns;
    public float CellSize => cellSize;

    public Vector3 GetNearestPointOnGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int z = Mathf.RoundToInt(localPos.z / cellSize);
        return new Vector3(x * cellSize, 0, z * cellSize) + transform.position;
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int z = Mathf.RoundToInt(localPos.z / cellSize);
        return new Vector2Int(x, z);
    }

    public bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < rows && cell.y >= 0 && cell.y < columns;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        for (int i = 0; i <= rows; i++)
        {
            Vector3 start = transform.position + transform.forward * cellSize * i;
            Vector3 end = start + transform.right * cellSize * columns;
            Gizmos.DrawLine(start, end);
        }

        for (int i = 0; i <= columns; i++)
        {
            Vector3 start = transform.position + transform.right * cellSize * i;
            Vector3 end = start + transform.forward * cellSize * rows;
            Gizmos.DrawLine(start, end);
        }
    }
#endif
}