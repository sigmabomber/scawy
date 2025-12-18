using UnityEngine;

public class PatrolPointManager : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    [SerializeField] private bool showConnections = true;
    [SerializeField] private Color pathColor = Color.cyan;

    public Transform[] GetPatrolPoints()
    {
        return points;
    }

    void OnDrawGizmos()
    {
        if (points == null || points.Length < 2 || !showConnections) return;

        Gizmos.color = pathColor;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;

            // Draw sphere at point
            Gizmos.DrawWireSphere(points[i].position, 0.3f);

            // Draw line to next point
            int nextIndex = (i + 1) % points.Length;
            if (points[nextIndex] != null)
            {
                Gizmos.DrawLine(points[i].position, points[nextIndex].position);

                // Draw direction arrow
                Vector3 direction = (points[nextIndex].position - points[i].position).normalized;
                Vector3 midPoint = (points[i].position + points[nextIndex].position) / 2f;
                DrawArrow(midPoint, direction, 0.5f);
            }
        }
    }

    void DrawArrow(Vector3 position, Vector3 direction, float size)
    {
        Vector3 right = Quaternion.Euler(0, 30, 0) * direction * size;
        Vector3 left = Quaternion.Euler(0, -30, 0) * direction * size;

        Gizmos.DrawLine(position, position - right);
        Gizmos.DrawLine(position, position - left);
    }
}