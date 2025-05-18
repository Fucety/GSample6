using UnityEngine;
using System.Collections.Generic;

public class VoronoiGen : MonoBehaviour
{
    [Header("Settings for CPU Voronoi Generation")]
    [SerializeField] private Material[] materials;
    [SerializeField] private int textureSize = 256;
    [SerializeField] private Vector2 defaultTextureWorldSize = new Vector2(10, 10);
    [SerializeField] private Vector2 defaultTextureWorldOffset = Vector2.zero;
    [SerializeField] private bool makeNonReadableAfterApply = true; // Новая опция для экономии памяти

    private List<Texture2D> cpuGeneratedTextures = new List<Texture2D>();
    private Dictionary<int, VoronoiGeneratorCPU.MaterialParams> lastMaterialParams = new Dictionary<int, VoronoiGeneratorCPU.MaterialParams>();

    void Start()
    {
        if (materials == null || materials.Length == 0)
        {
            Debug.LogError("VoronoiGen: Materials array is not assigned or empty!");
            return;
        }
        SetupAndGenerateTextures_CPU();
    }

    void Update()
    {
        bool parametersChanged = CheckIfMaterialParametersChanged();
        if (parametersChanged)
        {
            Debug.Log("VoronoiGen: Material parameters changed, regenerating textures.");
            SetupAndGenerateTextures_CPU();
        }
    }

    void SetupAndGenerateTextures_CPU()
    {
        ClearGeneratedTextures();

        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null)
            {
                Debug.LogWarning($"VoronoiGen: Material at index {i} is not assigned. Skipping.");
                cpuGeneratedTextures.Add(null);
                continue;
            }

            VoronoiGeneratorCPU.MaterialParams cpuParams = GetMaterialParamsForGeneration(mat);
            Texture2D generatedTexture = VoronoiGeneratorCPU.GenerateVoronoiTextureOnCPU(cpuParams, textureSize, makeNonReadableAfterApply);
            cpuGeneratedTextures.Add(generatedTexture);
            mat.SetTexture("_VoronoiTexture", generatedTexture);
            lastMaterialParams[i] = cpuParams; // Сохраняем параметры для отслеживания изменений
        }
    }

    private VoronoiGeneratorCPU.MaterialParams GetMaterialParamsForGeneration(Material mat)
    {
        Vector4 worldSizeVec = mat.HasProperty("_TextureWorldSize") ? mat.GetVector("_TextureWorldSize") : new Vector4(defaultTextureWorldSize.x, defaultTextureWorldSize.y, 0, 0);
        Vector2 actualTextureWorldSize = new Vector2(worldSizeVec.x, worldSizeVec.y);
        if (actualTextureWorldSize.sqrMagnitude < 0.001f)
        {
            actualTextureWorldSize = defaultTextureWorldSize;
            if (mat.HasProperty("_TextureWorldSize")) mat.SetVector("_TextureWorldSize", new Vector4(actualTextureWorldSize.x, actualTextureWorldSize.y, 0, 0));
        }

        Vector2 actualTextureWorldOffset = mat.HasProperty("_TextureWorldOffset") ? mat.GetVector("_TextureWorldOffset") : defaultTextureWorldOffset;

        return new VoronoiGeneratorCPU.MaterialParams
        {
            voronoiScale = mat.GetFloat("_VoronoiScale"),
            voronoiJitter = mat.GetFloat("_VoronoiJitter"),
            metricForRamp = mat.GetFloat("_MetricForRamp"),
            perturbationStrength = mat.GetFloat("_PerturbationStrength"),
            perturbationScale = mat.GetFloat("_PerturbationScale"),
            planarStrength = 1.0f, // Нейтральное значение для генерации
            textureTransform = new Vector4(1f, 1f, 0f, 0f), // Нейтральное значение для генерации
            textureWorldSize = actualTextureWorldSize,
            textureWorldOffset = actualTextureWorldOffset
        };
    }

    private bool CheckIfMaterialParametersChanged()
    {
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null) continue;

            VoronoiGeneratorCPU.MaterialParams currentParams = GetMaterialParamsForGeneration(mat);

            if (lastMaterialParams.TryGetValue(i, out VoronoiGeneratorCPU.MaterialParams previousParams))
            {
                if (!AreParamsEqual(currentParams, previousParams))
                    return true;
            }
            else
            {
                return true; // Первый вызов после Start, нужно обновить
            }
        }
        return false;
    }

    private bool AreParamsEqual(VoronoiGeneratorCPU.MaterialParams a, VoronoiGeneratorCPU.MaterialParams b)
    {
        return a.voronoiScale == b.voronoiScale &&
               a.voronoiJitter == b.voronoiJitter &&
               a.metricForRamp == b.metricForRamp &&
               a.perturbationStrength == b.perturbationStrength &&
               a.perturbationScale == b.perturbationScale &&
               a.planarStrength == b.planarStrength &&
               a.textureTransform == b.textureTransform &&
               a.textureWorldSize == b.textureWorldSize &&
               a.textureWorldOffset == b.textureWorldOffset;
    }

    private void ClearGeneratedTextures()
    {
        foreach (var tex in cpuGeneratedTextures)
        {
            if (tex != null)
            {
                if (Application.isPlaying) Destroy(tex);
                else DestroyImmediate(tex);
            }
        }
        cpuGeneratedTextures.Clear();
    }

    public void RegenerateAllTextures()
    {
        Debug.Log("VoronoiGen: Force regenerating all CPU Voronoi textures.");
        SetupAndGenerateTextures_CPU();
    }

    void OnDestroy()
    {
        ClearGeneratedTextures();
    }
}