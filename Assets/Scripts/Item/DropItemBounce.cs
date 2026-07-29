using System.Collections;
using UnityEngine;

/// <summary>
/// 掉落物生成后的弹跳动画，挂在掉落物prefab上即可
/// </summary>
public class DropItemBounce : MonoBehaviour
{
    [Header("弹跳参数")]
    public float duration = 0.4f;
    public float distance = 1f;
    public float peakHeight = 0.5f;

    private IEnumerator Bounce()
    {
        // 随机决定弹跳方向：50%概率向左（-1），50%概率向右（1）
        float dir = Random.value > 0.5f ? 1f : -1f;

        // 随机水平距离：在基础距离的 50%~150% 之间波动，增加变化性
        float randomDist = Random.Range(distance * 0.5f, distance * 1.5f);

        // 记录起点位置
        Vector3 startPos = transform.position;

        // 计算终点位置：沿X轴方向移动 randomDist 距离
        Vector3 endPos = startPos + new Vector3(dir * randomDist, 0, 0);

        // 计时器，从0开始累加到 duration
        float timer = 0f;

        // 逐帧更新位置，直到计时结束
        while (timer < duration)
        {
            // t 从 0→1 线性变化，代表动画的进度（0=开始，1=结束）
            float t = timer / duration;

            // 水平方向：从起点到终点做线性插值（匀速移动）
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            // 垂直方向：使用抛物线公式 y = 4h * t * (1-t)
            // 当 t=0 时 y=0（起点），t=0.5 时 y=peakHeight（最高点），t=1 时 y=0（终点）
            // 乘以 4 是为了让峰值正好等于 peakHeight
            pos.y += peakHeight * 4f * t * (1f - t);

            // 应用计算好的位置
            transform.position = pos;

            // 累加帧时间，继续下一帧
            timer += Time.deltaTime;
            // 等待一帧，让动画逐帧播放（yield return null 表示等待当前帧结束）
            yield return null;
        }

        // 确保物体精确落在终点（防止浮点数误差导致位置偏移）
        transform.position = endPos;
        //this.gameObject.GetComponent<ItemBase>().RegisterToWorld();
    }

    public void UseBounce()
    {
        StartCoroutine(Bounce());
    }
}
