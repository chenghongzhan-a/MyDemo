using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 过场动画
/// </summary>
[RequireComponent(typeof(RawImage))]
public class NuclearFissionAnimation : MonoBehaviour
{
    [Header("内部分辨率 (越低 = 越像素)")]
    public int internalWidth = 240;
    public int internalHeight = 135;

    [Header("画面上维持的原子数量")]
    public int targetAtoms = 10;

    [Header("动画速度倍率 (1=默认, 2=两倍速, 0.5=半速)")]
    [Range(0.1f, 5f)]
    public float animationSpeed = 1f;

    // 纹理 & 像素缓冲
    private Texture2D _tex;
    private Color32[] _px;
    private RawImage _img;
    private int W, H;

    // 固定 60fps 逻辑帧（帧率波动时动画速度不变）
    private float _timer;
    private const float TICK = 1f / 60f;

    // ===== 颜色 =====
    static readonly Color32 C_BG         = new Color32(0x0a, 0x0a, 0x0a, 0xff);
    static readonly Color32 C_ATOM_GLOW  = new Color32(0x0d, 0x3a, 0x0d, 0xff);
    static readonly Color32 C_ATOM       = new Color32(0x2e, 0xcc, 0x40, 0xff);
    static readonly Color32 C_ATOM_CORE  = new Color32(0x7e, 0xff, 0x7e, 0xff);
    static readonly Color32 C_NEUTRON    = new Color32(0xff, 0xd7, 0x00, 0xff);
    static readonly Color32 C_NEUTRON_M  = new Color32(0xbf, 0xa0, 0x00, 0xff);
    static readonly Color32 C_NEUTRON_T  = new Color32(0x8a, 0x7a, 0x00, 0xff);
    static readonly Color32 C_FLASH      = new Color32(0xff, 0xff, 0xff, 0xff);
    static readonly Color32 C_ENERGY1    = new Color32(0xff, 0x85, 0x1b, 0xff);
    static readonly Color32 C_ENERGY2    = new Color32(0xff, 0x41, 0x36, 0xff);
    static readonly Color32 C_RING       = new Color32(0xff, 0xe0, 0x4a, 0xff);
    static readonly Color32 C_SPAWN_RING  = new Color32(0x2e, 0xcc, 0x40, 0xff);

    // ===== 数据结构 =====
    class Atom     { public float x, y, r = 4; public bool alive = true; public float wobble, spawn; }
    class Neutron  { public float x, y, vx, vy; public bool dead; }
    class Particle { public float x, y, vx, vy, life, maxLife; public Color32 color; }
    class FlashFx  { public float x, y, life, max; }
    class RingFx   { public float x, y, r, max, life; public Color32 color; }

    readonly List<Atom>     _atoms     = new List<Atom>();
    readonly List<Neutron>  _neutrons  = new List<Neutron>();
    readonly List<Particle> _particles = new List<Particle>();
    readonly List<FlashFx>  _flashes   = new List<FlashFx>();
    readonly List<RingFx>   _rings     = new List<RingFx>();
    float _shake, _spawnTimer;

    // ====================================================================
    //  Unity 生命周期
    // ====================================================================

    void Start()
    {
        _img = GetComponent<RawImage>();
        W = internalWidth;
        H = internalHeight;
        targetAtoms = Mathf.Clamp(targetAtoms, 1, 50);

        _tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,   // ← 像素感的关键：最近邻采样
            wrapMode   = TextureWrapMode.Clamp
        };
        _px = new Color32[W * H];
        _img.texture = _tex;

        for (int i = 0; i < targetAtoms; i++) SpawnAtom(true);
        _neutrons.Add(new Neutron { x = -4, y = H * 0.5f, vx = 1.6f, vy = 0 });
    }

    void Update()
    {
        float scaledTick = TICK / Mathf.Max(animationSpeed, 0.01f);
        _timer += Time.deltaTime;
        int safety = 0;  // 防止极高速度下死循环
        while (_timer >= scaledTick && safety < 8) { Tick(); _timer -= scaledTick; safety++; }
        if (_timer > scaledTick) _timer = 0;  // 积压过多则丢弃
        Render();
    }

    void OnDestroy()
    {
        if (_tex != null) Destroy(_tex);
    }

    // ====================================================================
    //  像素绘制
    // ====================================================================

    /// <summary>写单个像素（自动翻转 Y 轴：Unity 原点在左下）</summary>
    void Pix(int x, int y, Color32 c)
    {
        // (uint) 技巧：负数变超大正数，一步搞定越界检查
        if ((uint)x >= W || (uint)y >= H) return;
        _px[(H - 1 - y) * W + x] = c;
    }

    void DrawCircle(float cx, float cy, float radius, Color32 c)
    {
        int r = Mathf.CeilToInt(radius);
        if (r <= 0) { Pix(Mathf.FloorToInt(cx), Mathf.FloorToInt(cy), c); return; }
        int ix = Mathf.FloorToInt(cx);
        int iy = Mathf.FloorToInt(cy);
        int r2 = Mathf.Max(1, Mathf.FloorToInt(radius * radius));
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
                if (x * x + y * y <= r2)
                    Pix(ix + x, iy + y, c);
    }

    void DrawRing(float cx, float cy, float radius, Color32 c)
    {
        int ix = Mathf.FloorToInt(cx);
        int iy = Mathf.FloorToInt(cy);
        int r = Mathf.Max(1, Mathf.FloorToInt(radius));
        for (float a = 0; a < 6.2832f; a += 0.08f)
            Pix(ix + Mathf.RoundToInt(Mathf.Cos(a) * r),
                iy + Mathf.RoundToInt(Mathf.Sin(a) * r), c);
    }

    // ====================================================================
    //  生成 & 裂变
    // ====================================================================

    void SpawnAtom(bool instant)
    {
        float x = 0, y = 0;
        for (int i = 0; i < 8; i++)   // 尝试 8 次找不重叠的位置
        {
            x = 25 + Random.value * (W - 50);
            y = 15 + Random.value * (H - 30);
            bool ok = true;
            for (int j = 0; j < _atoms.Count; j++)
            {
                float dx = _atoms[j].x - x, dy = _atoms[j].y - y;
                if (dx * dx + dy * dy < 400f) { ok = false; break; }
            }
            if (ok) break;
        }
        _atoms.Add(new Atom
        {
            x = x, y = y,
            spawn = instant ? 1f : 0f,
            wobble = Random.value * 6.2832f
        });
        _rings.Add(new RingFx { x = x, y = y, r = 1, max = 7, life = 10, color = C_SPAWN_RING });
    }

    void SplitAtom(Atom atom)
    {
        atom.alive = false;
        _flashes.Add(new FlashFx { x = atom.x, y = atom.y, life = 6, max = 6 });
        _rings.Add(new RingFx { x = atom.x, y = atom.y, r = 2, max = 14, life = 12, color = C_RING });
        _shake = Mathf.Min(_shake + 2.5f, 5f);

        // 释放 2~3 个中子
        int count = 2 + Mathf.FloorToInt(Random.value * 2f);
        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * 6.2832f + Random.value * 0.6f;
            float speed = 1.4f + Random.value * 0.5f;
            _neutrons.Add(new Neutron
            {
                x = atom.x, y = atom.y,
                vx = Mathf.Cos(angle) * speed,
                vy = Mathf.Sin(angle) * speed
            });
        }

        // 能量粒子
        for (int i = 0; i < 12; i++)
        {
            float angle = Random.value * 6.2832f;
            float speed = 0.4f + Random.value * 2.0f;
            _particles.Add(new Particle
            {
                x = atom.x, y = atom.y,
                vx = Mathf.Cos(angle) * speed,
                vy = Mathf.Sin(angle) * speed,
                life = 16 + Random.value * 16,
                maxLife = 32,
                color = Random.value < 0.5f ? C_ENERGY1 : C_ENERGY2
            });
        }
    }

    // ====================================================================
    //  逻辑帧（固定 60fps）
    // ====================================================================

    void Tick()
    {
        // --- 中子运动 + 碰撞 ---
        for (int i = 0; i < _neutrons.Count; i++)
        {
            var n = _neutrons[i];
            n.x += n.vx; n.y += n.vy;
            for (int j = 0; j < _atoms.Count; j++)
            {
                var a = _atoms[j];
                if (!a.alive) continue;
                float dx = n.x - a.x, dy = n.y - a.y;
                float rr = a.r + 1.5f;
                if (dx * dx + dy * dy < rr * rr)
                {
                    SplitAtom(a);
                    n.dead = true;
                    break;
                }
            }
            if (n.x < -8 || n.x > W + 8 || n.y < -8 || n.y > H + 8) n.dead = true;
        }
        _neutrons.RemoveAll(n => n.dead);

        // --- 清理已裂变原子 ---
        _atoms.RemoveAll(a => !a.alive);

        // --- 持续补充新原子 ---
        _spawnTimer--;
        if (_atoms.Count < targetAtoms && _spawnTimer <= 0)
        {
            SpawnAtom(false);
            _spawnTimer = 12 + Random.value * 18f;
        }

        // --- 中子耗尽 → 补射 ---
        if (_neutrons.Count == 0 && _atoms.Count > 0)
        {
            var target = _atoms[Random.Range(0, _atoms.Count)];
            bool left = Random.value < 0.5f;
            _neutrons.Add(new Neutron
            {
                x = left ? -4 : W + 4,
                y = target.y + (Random.value - 0.5f) * 6f,
                vx = left ? 1.6f : -1.6f,
                vy = (Random.value - 0.5f) * 0.4f
            });
        }

        // --- 粒子 ---
        for (int i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            p.x += p.vx; p.y += p.vy;
            p.vx *= 0.95f; p.vy *= 0.95f;
            p.life--;
        }
        _particles.RemoveAll(p => p.life <= 0);

        // --- 闪光 ---
        for (int i = 0; i < _flashes.Count; i++) _flashes[i].life--;
        _flashes.RemoveAll(f => f.life <= 0);

        // --- 冲击波 ---
        for (int i = 0; i < _rings.Count; i++) { _rings[i].r += 1f; _rings[i].life--; }
        _rings.RemoveAll(r => r.life <= 0);

        // --- 震动衰减 ---
        _shake *= 0.82f;

        // --- 原子动画：抖动 + 生长 ---
        for (int i = 0; i < _atoms.Count; i++)
        {
            _atoms[i].wobble += 0.08f;
            if (_atoms[i].spawn < 1f)
                _atoms[i].spawn = Mathf.Min(1f, _atoms[i].spawn + 0.08f);
        }
    }

    // ====================================================================
    //  渲染
    // ====================================================================

    void Render()
    {
        // 清屏
        var bg = C_BG;
        for (int i = 0; i < _px.Length; i++) _px[i] = bg;

        int sx = Mathf.RoundToInt((Random.value - 0.5f) * _shake);
        int sy = Mathf.RoundToInt((Random.value - 0.5f) * _shake);

        // 原子（发光层 + 主体 + 核心，带生长动画）
        for (int i = 0; i < _atoms.Count; i++)
        {
            var a = _atoms[i];
            float s = a.spawn;
            float wob = Mathf.Sin(a.wobble) * 0.3f;
            float r = a.r * s;
            if (r < 1f) { DrawCircle(a.x + sx, a.y + sy, 1, C_ATOM_CORE); continue; }
            DrawCircle(a.x + sx, a.y + sy, r + 2 + wob, C_ATOM_GLOW);
            DrawCircle(a.x + sx, a.y + sy, r + wob,     C_ATOM);
            if (r > 2f) DrawCircle(a.x + sx, a.y + sy, r - 2 + wob, C_ATOM_CORE);
        }

        // 冲击波环
        for (int i = 0; i < _rings.Count; i++)
        {
            var ring = _rings[i];
            if (ring.r < ring.max)
                DrawRing(ring.x + sx, ring.y + sy, Mathf.Floor(ring.r), ring.color);
        }

        // 裂变闪光
        for (int i = 0; i < _flashes.Count; i++)
        {
            var f = _flashes[i];
            float t = f.life / f.max;
            DrawCircle(f.x + sx, f.y + sy, Mathf.Ceil(5f * t + 1f), C_FLASH);
        }

        // 能量粒子
        for (int i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            if (p.life > 3f)
                Pix(Mathf.FloorToInt(p.x + sx), Mathf.FloorToInt(p.y + sy), p.color);
        }

        // 中子（拖尾 → 本体）
        for (int i = 0; i < _neutrons.Count; i++)
        {
            var n = _neutrons[i];
            Pix(Mathf.FloorToInt(n.x - n.vx * 2 + sx), Mathf.FloorToInt(n.y - n.vy * 2 + sy), C_NEUTRON_T);
            Pix(Mathf.FloorToInt(n.x - n.vx     + sx), Mathf.FloorToInt(n.y - n.vy     + sy), C_NEUTRON_M);
            Pix(Mathf.FloorToInt(n.x           + sx), Mathf.FloorToInt(n.y           + sy), C_NEUTRON);
        }

        // 上传 GPU
        _tex.SetPixels32(_px);
        _tex.Apply(false);
    }
}
