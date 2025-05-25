using UnityEngine;

public static class VoronoiGeneratorCPU
{
    [System.Serializable]
    public struct MaterialParams
    {
        public float voronoiScale;
        public float voronoiJitter;
        public float metricForRamp;
        public float perturbationStrength;
        public float perturbationScale;
        public float planarStrength;
        public Vector4 textureTransform;
        public Vector2 textureWorldSize;
        public Vector2 textureWorldOffset;
    }

    private static float Frac(float x)
    {
        int i = (int)x;
        return x - i;
    }

    private static Vector2 Frac(Vector2 v)
    {
        return new Vector2(Frac(v.x), Frac(v.y));
    }

    private static Vector2 Floor(Vector2 v)
    {
        return new Vector2((int)v.x, (int)v.y);
    }

    private static Vector2 Hash2DTo2D(Vector2 p)
    {
        int ix = (int)Mathf.Round(p.x);
        int iy = (int)Mathf.Round(p.y);
        uint hash = (uint)(ix * 73856093 ^ iy * 19349663);
        float rx = (hash % 10000) / 10000.0f;
        hash = hash * 1664525 + 1013904223;
        float ry = (hash % 10000) / 10000.0f;
        return new Vector2(rx, ry);
    }
    private static Vector2 SimpleValueNoise2D_CPU(Vector2 p)
    {
        Vector2 i = Floor(p);
        Vector2 f = Frac(p);
        Vector2 h00 = Hash2DTo2D(i);
        Vector2 h10 = Hash2DTo2D(i + new Vector2(1, 0));
        Vector2 h01 = Hash2DTo2D(i + new Vector2(0, 1));
        Vector2 h11 = Hash2DTo2D(i + new Vector2(1, 1));
        Vector2 lerpX1 = Vector2.Lerp(h00, h10, f.x);
        Vector2 lerpX2 = Vector2.Lerp(h01, h11, f.x);
        return Vector2.Lerp(lerpX1, lerpX2, f.y) * 0.5f;
    }

    private static Vector4 ComputeVoronoiPixel(int pixelX, int pixelY, int textureSize, MaterialParams matParams)
    {
        // Вычисляем UV с явным ограничением точности
        float uvX = (float)pixelX / textureSize;
        float uvY = (float)pixelY / textureSize;
        uvX = Mathf.Round(uvX * 10000f) / 10000f;
        uvY = Mathf.Round(uvY * 10000f) / 10000f;
        Vector2 uv = new Vector2(uvX, uvY);

        // Применяем преобразования с ограничением точности
        Vector2 coords = new Vector2(
            Mathf.Round((uv.x * matParams.textureWorldSize.x + matParams.textureWorldOffset.x) * 10000f) / 10000f,
            Mathf.Round((uv.y * matParams.textureWorldSize.y + matParams.textureWorldOffset.y) * 10000f) / 10000f
        );
        coords = new Vector2(
            Mathf.Round((coords.x * matParams.textureTransform.x + matParams.textureTransform.z) * 10000f) / 10000f,
            Mathf.Round((coords.y * matParams.textureTransform.y + matParams.textureTransform.w) * 10000f) / 10000f
        );
        coords = new Vector2(
            Mathf.Round(coords.x / Mathf.Max(matParams.planarStrength, 0.001f) * 10000f) / 10000f,
            Mathf.Round(coords.y / Mathf.Max(matParams.planarStrength, 0.001f) * 10000f) / 10000f
        );

        // Применяем шум пертурбации
        Vector2 noiseOffset = SimpleValueNoise2D_CPU(coords * matParams.perturbationScale) * matParams.perturbationStrength;
        coords = new Vector2(
            Mathf.Round((coords.x + noiseOffset.x) * 10000f) / 10000f,
            Mathf.Round((coords.y + noiseOffset.y) * 10000f) / 10000f
        );
        coords = new Vector2(
            Mathf.Round(coords.x * Mathf.Max(matParams.voronoiScale, 0.001f) * 10000f) / 10000f,
            Mathf.Round(coords.y * Mathf.Max(matParams.voronoiScale, 0.001f) * 10000f) / 10000f
        );

        Vector2 n = Floor(coords);
        Vector2 f = Frac(coords);
        float min_dist1 = 8.0f;
        float min_dist2 = 8.0f;
        Vector2 closest_cell_offset = Vector2.zero;

        for (int j = -1; j <= 1; j++)
        {
            for (int i = -1; i <= 1; i++)
            {
                Vector2 g = new Vector2(i, j);
                Vector2 cell_id_temp = n + g;
                Vector2 randomOffsetInCell = Hash2DTo2D(cell_id_temp);
                Vector2 cellPointFeature = g + Vector2.Lerp(new Vector2(0.5f, 0.5f), randomOffsetInCell, Mathf.Clamp01(matParams.voronoiJitter));
                Vector2 r = cellPointFeature - f;

                float dist;
                if (matParams.metricForRamp < 0.5f) dist = Vector2.Dot(r, r);
                else if (matParams.metricForRamp < 1.5f) dist = Vector2.Dot(r, r);
                else dist = Mathf.Abs(r.x) + Mathf.Abs(r.y);

                if (dist < min_dist1)
                {
                    min_dist2 = min_dist1;
                    min_dist1 = dist;
                    closest_cell_offset = g;
                }
                else if (dist < min_dist2) min_dist2 = dist;
            }
        }

        Vector2 cell_id = n + closest_cell_offset;
        if (matParams.metricForRamp < 1.5f)
        {
            min_dist1 = Mathf.Sqrt(Mathf.Max(min_dist1, 0.0f));
            min_dist2 = Mathf.Sqrt(Mathf.Max(min_dist2, 0.0f));
        }

        Vector4 output = new Vector4(Mathf.Clamp01(min_dist1), Mathf.Clamp01(min_dist2), cell_id.x, cell_id.y);
        if (float.IsNaN(output.x) || float.IsNaN(output.y) || float.IsNaN(output.z) || float.IsNaN(output.w))
            output = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);

        return output;
    }

    public static Texture2D GenerateVoronoiTextureOnCPU(MaterialParams matParams, int textureSize, bool makeNoLongerReadable = true)
    {
        Texture2D voronoiTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBAHalf, false);
        voronoiTexture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector4 voronoiData = ComputeVoronoiPixel(x, y, textureSize, matParams);
                pixels[y * textureSize + x] = new Color(voronoiData.x, voronoiData.y, voronoiData.z, voronoiData.w);
            }
        }

        voronoiTexture.SetPixels(pixels);
        voronoiTexture.Apply(false, makeNoLongerReadable); // Освобождаем память на CPU, если указано
        return voronoiTexture;
    }
}