
Shader "WlasneEfekty/KurzPowietrzny"
{
    Properties
    {
        // Podstawowy kolor drobinek kurzu.
        _KolorKurzu
            ("Kolor kurzu", Color) = (1, 0.82, 0.55, 1)

        // Maksymalna widocznosc czasteczek.
        _Przezroczystosc
            ("Przezroczystosc", Range(0, 1)) = 0.45

        // Miekkosc krawedzi kazdej czasteczki.
        _MiekkoscKrawedzi
            ("Miekkosc krawedzi", Range(0.05, 0.5)) = 0.3

        // Parametry subtelnego migotania.
        _SilaMigotania
            ("Sila migotania", Range(0, 1)) = 0.15

        _PredkoscMigotania
            ("Predkosc migotania", Range(0, 10)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "RenderowanieKurzu"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            // Standardowe mieszanie przezroczystosci.
            Blend SrcAlpha OneMinusSrcAlpha

            // Kurz nie zapisuje glebi.
            ZWrite Off

            // Czasteczki pozostaja zasloniete
            // przez sciany i inne obiekty.
            ZTest LEqual

            Cull Off

            HLSLPROGRAM

            #pragma vertex Wierzcholki
            #pragma fragment Fragmenty

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)

                float4 _KolorKurzu;
                float _Przezroczystosc;
                float _MiekkoscKrawedzi;
                float _SilaMigotania;
                float _PredkoscMigotania;

            CBUFFER_END

            // Dane otrzymywane z systemu czasteczek.
            struct DaneWierzcholka
            {
                float4 pozycjaOS : POSITION;
                float2 wspolrzedneUV : TEXCOORD0;
                float4 kolorCzasteczki : COLOR;
            };

            // Dane przekazywane do fragmentow.
            struct DaneFragmentu
            {
                float4 pozycjaHCS : SV_POSITION;
                float2 wspolrzedneUV : TEXCOORD0;
                float4 kolorCzasteczki : COLOR;
            };

            // =====================================
            // SHADER WIERZCHOLKOW
            // =====================================

            DaneFragmentu Wierzcholki(
                DaneWierzcholka dane
            )
            {
                DaneFragmentu wynik;

                wynik.pozycjaHCS =
                    TransformObjectToHClip(
                        dane.pozycjaOS.xyz
                    );

                wynik.wspolrzedneUV =
                    dane.wspolrzedneUV;

                // Kolor pochodzi z Particle System.
                wynik.kolorCzasteczki =
                    dane.kolorCzasteczki;

                return wynik;
            }

            // =====================================
            // SHADER FRAGMENTOW
            // =====================================

            half4 Fragmenty(
                DaneFragmentu dane
            ) : SV_Target
            {
                // 1. Odleglosc od srodka czasteczki.
                float2 pozycja =
                    dane.wspolrzedneUV - 0.5;

                float odleglosc =
                    length(pozycja);

                // 2. Tworzenie miekkiej, okraglej maski.
                // Srodek jest widoczny, krawedzie zanikaja.
                float maska = 1.0 - smoothstep(
                    0.5 - _MiekkoscKrawedzi,
                    0.5,
                    odleglosc
                );

                // 3. Subtelne proceduralne migotanie.
                float migotanie = sin(
                    _Time.y * _PredkoscMigotania
                    + dane.wspolrzedneUV.x * 8.0
                    + dane.wspolrzedneUV.y * 5.0
                );

                migotanie =
                    1.0 + migotanie * _SilaMigotania;

                // 4. Kolor kurzu.
                float3 kolor =
                    _KolorKurzu.rgb
                    * dane.kolorCzasteczki.rgb
                    * migotanie;

                // 5. Przezroczystosc.
                // Uwzgledniamy rowniez zanikanie
                // z systemu czasteczek.
                float alfa =
                    maska
                    * _Przezroczystosc
                    * _KolorKurzu.a
                    * dane.kolorCzasteczki.a;

                alfa = saturate(alfa);

                return half4(kolor, alfa);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
