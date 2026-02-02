Shader "UI/GlowingRibbonFill"
{
    Properties
    {
        [Header(Progress)]
        _Progress("Progress 0-1", Range(0,1)) = 1.0

        [Header(Colors)]
        _MainColor("Main Color (Gold)", Color) = (1,0.85,0.4,1)
        _SecondaryColor("Secondary Color (White)", Color) = (1,0.98,0.9,1)
        _BGColor("Background Color", Color) = (0,0,0,0)

        [Header(Strands)]
        _StrandCount("Strand Count", Range(1,6)) = 4
        _StrandWidth("Strand Width", Range(0.001,0.08)) = 0.012
        _StrandGlow("Strand Glow Radius", Range(0.01,0.15)) = 0.06
        _StrandSpread("Strand Spread", Range(0.02,0.4)) = 0.15

        [Header(Wave Animation)]
        _WaveAmp("Wave Amplitude", Range(0,0.25)) = 0.08
        _WaveFreq("Wave Frequency", Range(1,15)) = 5
        _WaveSpeed("Wave Speed", Range(0,3)) = 1.2

        [Header(Glow)]
        _GlowIntensity("Glow Intensity", Range(1,8)) = 3.5
        _CenterGlow("Center Glow", Range(0,2)) = 0.8

        [Header(Sparkles)]
        _SparkleDensity("Sparkle Density", Range(0,100)) = 40
        _SparkleIntensity("Sparkle Intensity", Range(0,1)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Progress;
                float4 _MainColor;
                float4 _SecondaryColor;
                float4 _BGColor;
                float _StrandCount;
                float _StrandWidth;
                float _StrandGlow;
                float _StrandSpread;
                float _WaveAmp;
                float _WaveFreq;
                float _WaveSpeed;
                float _GlowIntensity;
                float _CenterGlow;
                float _SparkleDensity;
                float _SparkleIntensity;
            CBUFFER_END

            // 高质量哈希
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // 平滑噪声
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash(i), hash(i + float2(1,0)), u.x),
                    lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), u.x),
                    u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float t = _Time.y * _WaveSpeed;

                // 进度裁剪：超出 _Progress 的部分完全透明
                if (uv.x > _Progress)
                    return float4(0, 0, 0, 0);

                // 开头柔和淡入（前3%）
                float startFade = smoothstep(0.0, 0.03, uv.x);
                // 末端柔和淡出（_Progress 前5%区域渐隐）
                float endFade = smoothstep(_Progress, _Progress - 0.05, uv.x);
                float edgeMask = startFade * endFade;

                // 中心 y = 0.5
                float cy = uv.y - 0.5;

                // 累积所有光丝
                float totalStrand = 0;
                float totalGlow = 0;
                int count = (int)_StrandCount;

                for (int i = 0; i < count; i++)
                {
                    // 不再固定偏移，让每条丝都从中心出发，靠波浪交织
                    // 每条丝有大幅度、不同频率和相位的波浪，形成交织效果
                    float basePhase = i * 3.14159 / count; // 均匀分布初始相位
                    
                    // 主波浪：大振幅，让丝线能够跨越中心交叉
                    float wave1 = sin(uv.x * _WaveFreq * 6.28 + basePhase + t * 1.2) * _WaveAmp;
                    // 次波浪：不同频率，增加复杂度
                    float wave2 = sin(uv.x * _WaveFreq * 3.14 * 1.7 + basePhase * 2.0 - t * 0.8) * _WaveAmp * 0.5;
                    // 第三波浪：更高频的细节
                    float wave3 = sin(uv.x * _WaveFreq * 9.42 + basePhase * 0.5 + t * 2.0) * _WaveAmp * 0.25;
                    
                    float wave = wave1 + wave2 + wave3;
                    
                    // 加噪声让线条更自然流动
                    wave += 0.02 * (noise(float2(uv.x * 12.0 + i * 5.0, t * 0.8 + i)) - 0.5);
                    
                    // 当前丝与像素的距离（从中心出发）
                    float dy = cy - wave;
                    float dist = abs(dy);
                    
                    // 核心细线（高斯衰减）
                    float coreStrand = exp(-dist * dist / (2.0 * _StrandWidth * _StrandWidth));
                    
                    // 发光晕（更宽的高斯）
                    float glowStrand = exp(-dist * dist / (2.0 * _StrandGlow * _StrandGlow));
                    
                    totalStrand += coreStrand;
                    totalGlow += glowStrand;
                }

                // 中心整体发光
                float centerGlow = exp(-cy * cy / 0.02) * _CenterGlow;

                // 闪烁粒子
                float2 sparkleUV = uv * _SparkleDensity;
                float sparkleRand = hash(floor(sparkleUV) + floor(t * 8.0));
                float sparkle = step(0.97, sparkleRand) * _SparkleIntensity;
                // 粒子随时间闪烁
                sparkle *= 0.5 + 0.5 * sin(t * 20.0 + sparkleRand * 100.0);
                sparkle *= edgeMask;

                // 颜色渐变：左侧金色，右侧偏白
                float colorMix = uv.x;
                float3 col = lerp(_MainColor.rgb, _SecondaryColor.rgb, colorMix * 0.7);

                // 最终合成
                float strandIntensity = saturate(totalStrand * 1.5);
                float glowIntensity = saturate(totalGlow * 0.4 + centerGlow) * _GlowIntensity;
                
                float3 rgb = col * (strandIntensity + glowIntensity * 0.5) + sparkle * float3(1,1,0.9);
                float alpha = saturate(strandIntensity * 0.9 + glowIntensity * 0.25 + sparkle);
                
                // 应用边缘遮罩
                alpha *= edgeMask;
                rgb *= edgeMask;

                // HDR 亮度提升（配合 Bloom）
                rgb *= 1.0 + glowIntensity * 0.5;

                return float4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
