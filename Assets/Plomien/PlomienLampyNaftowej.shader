Shader "WlasneEfekty/PlomienLampyNaftowej"
{
    Properties
    {
        [HDR] _KolorRdzenia ("Kolor rdzenia", Color) = (1.8, 1.4, 0.7, 1)
        [HDR] _KolorZewnetrzny ("Kolor zewnetrzny", Color) = (1.8, 0.45, 0.08, 1)

        _MnoznikJasnosci ("Mnoznik jasnosci", Range(0, 10)) = 1.5

        _WysokoscPlomienia ("Wysokosc plomienia", Range(0.6, 1.2)) = 1
        _SzerokoscPodstawy ("Szerokosc podstawy", Range(0.05, 0.4)) = 0.22
        _MiekkoscKrawedzi ("Miekkosc krawedzi", Range(0.01, 0.2)) = 0.07

        _SilaFalowania ("Sila falowania", Range(0, 0.2)) = 0.045
        _PredkoscFalowania ("Predkosc falowania", Range(0, 10)) = 3.5

        _SilaMigotania ("Sila migotania", Range(0, 1)) = 0.2
        _PredkoscMigotania ("Predkosc migotania", Range(0, 10)) = 4.0

        _PrzesuniecieFazy ("Przesuniecie fazy", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "PlomienLampyNaftowej"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM

            #pragma vertex Wierzcholki
            #pragma fragment Fragmenty

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)

                float4 _KolorRdzenia;
                float4 _KolorZewnetrzny;

                float _MnoznikJasnosci;

                float _WysokoscPlomienia;
                float _SzerokoscPodstawy;
                float _MiekkoscKrawedzi;

                float _SilaFalowania;
                float _PredkoscFalowania;

                float _SilaMigotania;
                float _PredkoscMigotania;

                float _PrzesuniecieFazy;

            CBUFFER_END

            struct DaneWierzcholka
            {
                float4 pozycjaOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct DaneFragmentu
            {
                float4 pozycjaHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            DaneFragmentu Wierzcholki(DaneWierzcholka dane)
            {
                DaneFragmentu wynik;

                wynik.pozycjaHCS =
                    TransformObjectToHClip(dane.pozycjaOS.xyz);

                wynik.uv = dane.uv;

                return wynik;
            }

            half4 Fragmenty(DaneFragmentu dane) : SV_Target
            {
                float2 uv = dane.uv;

                float czas = _Time.y + _PrzesuniecieFazy;

                // Normalizacja wysokosci plomienia.
                float y = saturate(uv.y / max(0.0001, _WysokoscPlomienia));

                // Falowanie plomienia na boki.
                float przesuniecieX =
                    sin(y * 12.0 + czas * _PredkoscFalowania) * _SilaFalowania;

                przesuniecieX +=
                    sin(y * 25.0 - czas * _PredkoscFalowania * 1.7) * (_SilaFalowania * 0.5);

                float x = (uv.x - 0.5) + przesuniecieX;

                // Zwê¿anie ku gorze.
                float szerokosc =
                    lerp(_SzerokoscPodstawy, 0.02, pow(y, 0.8));

                float maskaSzerokosci =
                    1.0 - smoothstep(
                        szerokosc,
                        szerokosc + _MiekkoscKrawedzi,
                        abs(x)
                    );

                // Delikatne pojawienie przy knotku i zanik przy gorze.
                float maskaDolna = smoothstep(0.0, 0.08, uv.y);
                float maskaGorna = 1.0 - smoothstep(0.82, 1.0, y);

                float maskaPlomienia =
                    saturate(maskaSzerokosci * maskaDolna * maskaGorna);

                // Migotanie jasnosci.
                float puls =
                    1.0
                    + sin(czas * _PredkoscMigotania) * (_SilaMigotania * 0.5)
                    + sin(czas * (_PredkoscMigotania * 1.87) + 3.1) * (_SilaMigotania * 0.25);

                maskaPlomienia *= puls;

                // Rdzen plomienia.
                float szerokoscRdzenia = szerokosc * 0.45;

                float maskaRdzenia =
                    1.0 - smoothstep(
                        szerokoscRdzenia,
                        szerokoscRdzenia + (_MiekkoscKrawedzi * 0.5),
                        abs(x)
                    );

                maskaRdzenia *=
                    smoothstep(0.0, 0.1, uv.y) *
                    (1.0 - smoothstep(0.55, 0.95, y));

                maskaRdzenia = saturate(maskaRdzenia);

                float3 kolor =
                    lerp(_KolorZewnetrzny.rgb, _KolorRdzenia.rgb, maskaRdzenia);

                kolor *= _MnoznikJasnosci;

                float alfa =
                    saturate(maskaPlomienia * (0.9 + maskaRdzenia * 0.25));

                return half4(kolor, alfa);
            }

            ENDHLSL
        }
    }

    FallBack Off
}