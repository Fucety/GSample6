using UnityEngine;
using System.Collections.Generic;

public class VoronoiComputeManager : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Material[] materials; // Материалы, использующие текстуру
    [SerializeField] private int textureSize = 256; // Размер текстуры (например, 256x256)
    [SerializeField] private Vector2 textureWorldSize = new Vector2(10, 10); // Размер области текстуры в мировых координатах
    [SerializeField] private Vector2 textureWorldOffset = Vector2.zero; // Смещение области текстуры

    private struct MaterialParams
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

    private List<RenderTexture> voronoiTextures = new List<RenderTexture>();
    private ComputeBuffer materialParamsBuffer;
    private int kernelHandle;

    void Start()
    {
        // Проверка поддержки Compute Shader
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("Compute Shaders не поддерживаются на этом устройстве!");
            return;
        }

        if (computeShader == null || materials == null || materials.Length == 0)
        {
            Debug.LogError("Compute Shader или материалы не назначены!");
            return;
        }

        // Настройка Compute Shader
        kernelHandle = computeShader.FindKernel("ComputeVoronoi");
        computeShader.SetInt("_TextureSize", textureSize);

        // Создание текстур и буфера параметров
        SetupTexturesAndBuffer();
    }

    void SetupTexturesAndBuffer()
    {
        // Очистка существующих текстур
        foreach (var tex in voronoiTextures)
        {
            if (tex != null) tex.Release();
        }
        voronoiTextures.Clear();

        // Создание параметров для буфера
        MaterialParams[] paramsArray = new MaterialParams[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            if (mat == null)
            {
                Debug.LogWarning($"Материал на индексе {i} не назначен!");
                continue;
            }

            // Получаем _TextureWorldSize из материала
            Vector4 worldSizeVec = mat.GetVector("_TextureWorldSize");
            Vector2 worldSize = new Vector2(worldSizeVec.x, worldSizeVec.y);

            // Если _TextureWorldSize не задано (нулевое или некорректное), используем значение по умолчанию
            if (worldSize.sqrMagnitude < 0.001f)
            {
                Debug.LogWarning($"_TextureWorldSize для материала {mat.name} не задано или равно нулю. Используется значение по умолчанию: {textureWorldSize}");
                worldSize = textureWorldSize;
                mat.SetVector("_TextureWorldSize", new Vector4(worldSize.x, worldSize.y, 0, 0));
            }

            paramsArray[i] = new MaterialParams
            {
                voronoiScale = mat.GetFloat("_VoronoiScale"),
                voronoiJitter = mat.GetFloat("_VoronoiJitter"),
                metricForRamp = mat.GetFloat("_MetricForRamp"),
                perturbationStrength = mat.GetFloat("_PerturbationStrength"),
                perturbationScale = mat.GetFloat("_PerturbationScale"),
                planarStrength = mat.GetFloat("_PlanarStrength"),
                textureTransform = mat.GetVector("_TextureTransform"),
                textureWorldSize = worldSize,
                textureWorldOffset = mat.GetVector("_TextureWorldOffset")
            };

            // Отладочный вывод параметров
            Debug.Log($"Материал {i} ({mat.name}): Scale={paramsArray[i].voronoiScale}, Jitter={paramsArray[i].voronoiJitter}, Metric={paramsArray[i].metricForRamp}, " +
                      $"PerturbationStrength={paramsArray[i].perturbationStrength}, PerturbationScale={paramsArray[i].perturbationScale}, " +
                      $"PlanarStrength={paramsArray[i].planarStrength}, Transform={paramsArray[i].textureTransform}, " +
                      $"WorldSize={paramsArray[i].textureWorldSize}, WorldOffset={paramsArray[i].textureWorldOffset}");

            // Создание текстуры для каждого материала
            RenderTexture voronoiTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
            voronoiTexture.enableRandomWrite = true;
            voronoiTexture.filterMode = FilterMode.Point; // Точечная фильтрация
            voronoiTexture.Create();
            voronoiTextures.Add(voronoiTexture);

            // Назначение текстуры материалу
            mat.SetTexture("_VoronoiTexture", voronoiTexture);
            computeShader.SetTexture(kernelHandle, "Result", voronoiTexture);
        }

        // Создание и заполнение буфера параметров
        if (materialParamsBuffer != null) materialParamsBuffer.Release();
        materialParamsBuffer = new ComputeBuffer(materials.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(MaterialParams)));
        materialParamsBuffer.SetData(paramsArray);
        computeShader.SetBuffer(kernelHandle, "_MaterialParams", materialParamsBuffer);

        // Начальный запуск
        UpdateVoronoiTextures();
    }

    void Update()
    {
        // Обновление текстур при изменении параметров
        UpdateVoronoiTextures();
    }

    void UpdateVoronoiTextures()
    {
        // Обновление буфера параметров
        MaterialParams[] paramsArray = new MaterialParams[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            if (mat == null) continue;

            // Получаем _TextureWorldSize из материала
            Vector4 worldSizeVec = mat.GetVector("_TextureWorldSize");
            Vector2 worldSize = new Vector2(worldSizeVec.x, worldSizeVec.y);

            // Если _TextureWorldSize не задано, используем значение по умолчанию
            if (worldSize.sqrMagnitude < 0.001f)
            {
                worldSize = textureWorldSize;
                mat.SetVector("_TextureWorldSize", new Vector4(worldSize.x, worldSize.y, 0, 0));
            }

            paramsArray[i] = new MaterialParams
            {
                voronoiScale = mat.GetFloat("_VoronoiScale"),
                voronoiJitter = mat.GetFloat("_VoronoiJitter"),
                metricForRamp = mat.GetFloat("_MetricForRamp"),
                perturbationStrength = mat.GetFloat("_PerturbationStrength"),
                perturbationScale = mat.GetFloat("_PerturbationScale"),
                planarStrength = mat.GetFloat("_PlanarStrength"),
                textureTransform = mat.GetVector("_TextureTransform"),
                textureWorldSize = worldSize,
                textureWorldOffset = mat.GetVector("_TextureWorldOffset")
            };
        }
        materialParamsBuffer.SetData(paramsArray);

        // Диспетчеризация Compute Shader для каждой текстуры
        uint threadX, threadY, threadZ;
        computeShader.GetKernelThreadGroupSizes(kernelHandle, out threadX, out threadY, out threadZ);
        int groupSizeX = Mathf.CeilToInt((float)textureSize / (float)threadX);
        int groupSizeY = Mathf.CeilToInt((float)textureSize / (float)threadY);

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null || voronoiTextures[i] == null) continue;

            computeShader.SetTexture(kernelHandle, "Result", voronoiTextures[i]);
            computeShader.SetInt("_MaterialIndex", i);
            computeShader.Dispatch(kernelHandle, groupSizeX, groupSizeY, 1);
        }
    }

    void OnDestroy()
    {
        // Освобождение ресурсов
        foreach (var tex in voronoiTextures)
        {
            if (tex != null) tex.Release();
        }
        if (materialParamsBuffer != null) materialParamsBuffer.Release();
    }
}