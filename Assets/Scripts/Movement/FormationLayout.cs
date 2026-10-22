using UnityEngine;

/// <summary>
/// Computes the positions of a simple formation: a compact grid centered on a point,
/// facing a direction, front row first.
/// </summary>
public static class FormationLayout
{
    /// <summary>
    /// Fills result[0..count-1] with the formation slots. The result array must hold at least 'count' items.
    /// </summary>
    public static void Fill(Vector3 center, Vector3 forward, int count, float spacing, Vector3[] result)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);

        int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt(count / (float)columns);
        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            // An incomplete last row is centered.
            int inRow = row == rows - 1 ? count - row * columns : columns;
            float x = (column - (inRow - 1) * 0.5f) * spacing;
            float z = ((rows - 1) * 0.5f - row) * spacing;
            result[i] = center + right * x + forward * z;
        }
    }
}
