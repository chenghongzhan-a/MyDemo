using UnityEngine;

/// <summary>
/// 区块配置（ScriptableObject）
/// 在 Unity Inspector 中调整这些参数来控制地图生成的各项行为。
/// 右键 → Create → 2D → Chunk Config 即可创建配置文件。
/// </summary>
[CreateAssetMenu(fileName = "ChunkConfig", menuName = "2D/Chunk Config")]
public class ChunkConfig : ScriptableObject
{
    // ============================================
    // 一、区块尺寸与加载范围
    // ============================================
    [Header("=== 区块尺寸 ===")]
    [Tooltip("每个区块的边长（瓦片数）。")]
    public int chunkSize = 16;

    [Header("=== 加载范围 ===")]
    [Tooltip("玩家周围加载几圈区块。")]
    public int loadRadius = 3;

    [Tooltip("超出几圈后卸载区块")]
    public int unloadRadius = 4;

    [Header("=== 性能优化 ===")]
    [Tooltip("每隔多少帧检查一次玩家是否移动到新区块。")]
    public int updateInterval = 10;

    [Tooltip("每帧最多生成几个新区块")]
    public int maxChunksPerFrame = 2;

    [Header("=== 瓦片网格 ===")]
    [Tooltip("每个瓦片在世界空间中的大小（单位）。\n这必须与 Sprite 的 PixelsPerUnit 匹配！\n公式：cellSize = 精灵像素 / PixelsPerUnit\n\n常见配置：\n  16px精灵 + PPU=16  → cellSize=1\n  32px精灵 + PPU=32  → cellSize=1\n  16px精灵 + PPU=100 → cellSize=0.16")]
    public float cellSize = 1f;

    // ============================================
    // 二、柏林噪声参数
    // ============================================
    [Header("=== 噪声参数（分形布朗运动 FBM）===")]
    [Tooltip("噪声缩放系数。值越小，地形特征越大、越平缓；值越大，地形越破碎。")]
    public float noiseScale = 0.05f;

    [Tooltip("噪声叠加层数（octaves）。层数越多地形细节越丰富，但计算量也越大。推荐 3~6。")]
    [Range(1, 8)]
    public int octaves = 4;

    [Tooltip("持续度（persistence）。每层振幅的衰减系数，通常 0.3~0.7。值越大，细节越突出。")]
    [Range(0.1f, 1f)]
    public float persistence = 0.5f;

    [Tooltip("间隙度（lacunarity）。每层频率的倍增系数，通常 1.5~3.0。值越大，细节越密集。")]
    [Range(0.1f, 5f)]
    public float lacunarity = 2.0f;

    // ============================================
    // 三、地形阈值（归一化后的值 0~1）
    // ============================================
    [Header("=== 地形阈值 ===")]
    [Tooltip("深海阈值：噪声值 < 此值为深海。")]
    [Range(0f, 1f)]
    public float deepSeaThreshold = 0.15f;

    [Tooltip("浅海阈值：噪声值介于 deepSea ~ shallowSea 之间为浅海。")]
    [Range(0f, 1f)]
    public float shallowSeaThreshold = 0.30f;

    [Tooltip("沙滩阈值：噪声值介于 shallowSea ~ sand 之间为沙滩。")]
    [Range(0f, 1f)]
    public float sandThreshold = 0.45f;

    [Tooltip("草地阈值：噪声值介于 sand ~ grass 之间为草地。高于此值为森林/山脉。")]
    [Range(0f, 1f)]
    public float grassThreshold = 0.70f;

    //湿度噪声
    [Header("=== 湿度噪声（独立于高度的第二层噪声）===")]
    [Tooltip("湿度噪声的缩放系数。")]
    public float moistureScale = 0.03f;

    [Tooltip("湿度高于此值的草地变为沼泽。")]
    [Range(0f, 1f)]
    public float swampMoistureThreshold = 0.7f;

    [Tooltip("湿度高于此值的区域可能出现池塘。")]
    [Range(0f, 1f)]
    public float pondMoistureThreshold = 0.85f;

    //温度噪声
    [Header("=== 湿度噪声（独立于高度的第二层噪声）===")]
    [Tooltip("温度噪声的缩放系数。")]
    public float temperatureScale = 0.03f;

    [Tooltip("温度低于此值的草地变为雪地。")]
    [Range(0f, 1f)]
    public float temperatureThreshold = 0.7f;

    // ============================================
    // 五、对象生成（树木、石头等装饰物）
    // ============================================
    [Header("=== 对象生成 ===")]
    [Tooltip("是否在区块中随机放置装饰物。")]
    public bool spawnObjects = true;

    [Tooltip("每个区块最多放置多少个装饰物。")]
    public int maxObjectsPerChunk = 20;

    [Tooltip("对象生成的随机种子偏移。")]
    public float objectNoiseScale = 0.5f;

    // ============================================
    // 六、调试
    // ============================================
    [Header("=== 调试 ===")]
    [Tooltip("是否在场景中绘制区块边界线（绿色线框）。")]
    public bool showDebugBounds = false;

    [Tooltip("是否在控制台输出详细日志。")]
    public bool verboseLogging = false;
}
