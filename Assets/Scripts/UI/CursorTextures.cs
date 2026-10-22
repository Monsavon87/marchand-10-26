using System;
using UnityEngine;

/// <summary>
/// Generates small mouse cursors in code, so no external asset is needed:
/// a "grabbing hand" (view drag) and two curved arrows (view rotation).
/// The caller owns the returned textures and must destroy them.
/// </summary>
public static class CursorTextures
{
    private const int Size = 32;
    private const float Center = 16f;
    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly Color32 Outline = new Color32(40, 40, 40, 255);

    /// <summary>Creates a 32x32 closed hand. A good hotspot is its center (16, 16).</summary>
    public static Texture2D CreateHand()
    {
        return Build("HandCursor", IsHand, new Color32(255, 224, 189, 255));
    }

    /// <summary>Creates a 32x32 rotation icon (two curved arrows). A good hotspot is its center (16, 16).</summary>
    public static Texture2D CreateRotation()
    {
        return Build("RotateCursor", IsRotationIcon, new Color32(255, 255, 255, 255));
    }

    // Fills the shape, then adds a dark outline so the cursor stays readable on any terrain.
    private static Texture2D Build(string name, Func<float, float, bool> isInside, Color32 fill)
    {
        bool[] mask = new bool[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                mask[y * Size + x] = isInside(x + 0.5f, y + 0.5f);
            }
        }

        Color32[] pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int index = y * Size + x;
                if (mask[index])
                {
                    pixels[index] = fill;
                }
                else
                {
                    pixels[index] = HasMaskNeighbour(mask, x, y) ? Outline : Clear;
                }
            }
        }

        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // Closed hand: a palm, four knuckles on top, a thumb on the left and a wrist at the bottom.
    private static bool IsHand(float x, float y)
    {
        bool palm = x >= 9f && x <= 23f && y >= 6f && y <= 17f;
        bool wrist = x >= 12f && x <= 20f && y >= 2f && y < 6f;
        bool knuckles = InCircle(x, y, 11f, 18f, 3.2f) || InCircle(x, y, 15f, 19.5f, 3.2f)
                        || InCircle(x, y, 19f, 19.5f, 3.2f) || InCircle(x, y, 22.5f, 18f, 3.0f);
        bool thumb = InCircle(x, y, 8f, 12f, 3.2f) || (x >= 7f && x <= 10f && y >= 10f && y <= 15f);
        return palm || wrist || knuckles || thumb;
    }

    // Two opposite arcs of a ring, each ending with a clockwise arrow head.
    private static bool IsRotationIcon(float x, float y)
    {
        const float radius = 9.75f;
        const float halfWidth = 1.9f;

        float dx = x - Center;
        float dy = y - Center;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        if (angle < 0f)
        {
            angle += 360f;
        }

        bool onRing = Mathf.Abs(distance - radius) <= halfWidth;
        bool inArc = (angle >= 35f && angle <= 145f) || (angle >= 215f && angle <= 325f);
        return (onRing && inArc) || InArrowHead(x, y, 35f, radius) || InArrowHead(x, y, 215f, radius);
    }

    // Triangle at the clockwise end of an arc: base across the ring, apex pointing along the tangent.
    private static bool InArrowHead(float x, float y, float angleDegrees, float radius)
    {
        const float length = 5.5f;
        const float halfBase = 4.5f;

        float theta = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(theta);
        float sin = Mathf.Sin(theta);
        float qx = x - (Center + radius * cos);
        float qy = y - (Center + radius * sin);

        // Clockwise tangent is (sin, -cos); the normal of the ring is (cos, sin).
        float along = qx * sin - qy * cos;
        float across = qx * cos + qy * sin;
        return along >= 0f && along <= length && Mathf.Abs(across) <= halfBase * (1f - along / length);
    }

    private static bool InCircle(float x, float y, float cx, float cy, float radius)
    {
        float dx = x - cx;
        float dy = y - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool HasMaskNeighbour(bool[] mask, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < Size && ny >= 0 && ny < Size && mask[ny * Size + nx])
                {
                    return true;
                }
            }
        }

        return false;
    }
}
