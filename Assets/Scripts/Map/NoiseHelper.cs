using UnityEngine;

/// <summary>
/// 柏林噪声工具类
/// </summary>
public static class NoiseHelper
{
    /// <summary>
    /// 要改变地形细节的话 就要改变这个
    /// </summary>
    /// <param name="x">世界X坐标（已乘以 noiseScale 后的值）</param>
    /// <param name="y">世界Y坐标（已乘以 noiseScale 后的值）</param>
    /// <param name="octaves">叠加层数 层数越大细节越丰富</param>
    /// <param name="persistence">振幅衰减率 值越大 下一层的影响就越大</param>
    /// <param name="lacunarity">频率倍增率 值越大 下一层形成的波频率就越大</param>
    /// <returns>归一化后的噪声值（0~1）</returns>
    public static float FBM(float x, float y, int octaves, float persistence, float lacunarity)
    {
        float value = 0f;
        //当前层的振幅
        float amplitude = 1f;
        //当前层的频率
        float frequency = 1f;
        //总振幅（用于归一化） 
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            //累加当前层的噪声贡献
            value += amplitude * Mathf.PerlinNoise(x * frequency, y * frequency);
            maxValue += amplitude;

            //下一层：振幅衰减，频率增加
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        //归一化到 0~1 范围
        return value / maxValue;
    }

    /// <summary>
    /// 计算单层柏林噪声
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="offset">随机偏移量，确保不同噪声层不相关</param>
    /// <returns>噪声值（0~1）</returns>
    public static float Perlin(float x, float y, float offset)
    {
        return Mathf.PerlinNoise(x + offset, y + offset);
    }

    /// <summary>
    /// 基于种子和坐标生成确定的偏移量
    /// </summary>
    /// <param name="seed">世界种子</param>
    /// <param name="chunkX">区块 X 坐标</param>
    /// <param name="chunkY">区块 Y 坐标</param>
    /// <returns>确定的偏移量（0 ~ 10000）</returns>
    public static float GetDeterministicOffset(int seed, int chunkX, int chunkY)
    {
        unchecked
        {
            // 使用经典哈希混合：乘一个质数再加下一个值
            int hash = seed;
            hash = hash * 397 + chunkX;
            hash = hash * 397 + chunkY;
            // 再从哈希值派生一个 0~10000 的浮点数
            // 这里用取模 + 除法，确保跨平台一致性
            uint uhash = (uint)hash;
            return (float)(uhash % 10000u) + (float)((uhash >> 16) & 0xFFFF) / 65536f;
        }
    }

    /// <summary>
    /// 基于种子和坐标生成第二层噪声的独立偏移量
    /// （确保高度噪声和湿度噪声使用不同的偏移，否则会产生相关性）
    /// </summary>
    public static float GetDeterministicOffset2(int seed, int chunkX, int chunkY)
    {
        unchecked
        {
            //用不同的初始值
            int hash = seed ^ 0x7F3A2B1C;
            //用不同的乘数
            hash = hash * 733 + chunkX;
            hash = hash * 733 + chunkY;
            uint uhash = (uint)hash;
            return (float)(uhash % 10000u) + (float)((uhash >> 16) & 0xFFFF) / 65536f;
        }
    }

    /// <summary>
    /// 基于坐标生成对象放置用的噪声值
    /// 使用较高的频率，让装饰物的分布看起来随机
    /// </summary>
    public static float GetObjectNoise(int worldX, int worldY, float scale, float offset)
    {
        return Mathf.PerlinNoise(
            worldX * scale + offset,
            worldY * scale + offset
        );
    }

    /// <summary>
    /// 将噪声值映射到 [0, 1] 范围并做平滑处理
    /// 使用平滑步进函数（smoothstep）消除硬边界
    /// </summary>
    public static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t); // 标准smoothstep公式
    }
}
