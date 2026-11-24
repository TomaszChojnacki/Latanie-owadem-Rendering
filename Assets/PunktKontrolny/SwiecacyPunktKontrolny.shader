
Shader "WlasneEfekty/SwiecacyPunktKontrolny"
{
    Properties
    {
        // Kolor i sila swiecenia punktu kontrolnego.
        [HDR] _KolorSwiecenia
            ("Kolor swiecenia", Color) = (1, 0.5, 0.05, 1)

        _Intensywnosc
            ("Intensywnosc swiecenia", Range(0, 15)) = 4

        // Parametry efektu Fresnela.
        _SilaFresnela
            ("Sila Fresnela", Range(0, 5)) = 2

        _PotegaFresnela
            ("Potega Fresnela", Range(0.5, 10)) = 3

        // Parametry animowanych pasm energii.
        _PredkoscFali
            ("Predkosc fali", Range(0, 10)) = 2

        _CzestotliwoscFali
            ("Czestotliwosc fali", Range(0, 30)) = 10

        // Parametry zmieniane przez skrypt C#.
        [HideInInspector] _Pulsowanie
            ("Pulsowanie", Range(0, 1)) = 1

        [HideInInspector] _Widocznosc
            ("Widocznosc", Range(0, 1)) = 1
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
            Name "SwieceniePunktuKontrolnego"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            // Wlaczenie przezroczystosci.
            Blend SrcAlpha OneMinusSrcAlpha

            // Przezroczysty obiekt nie zapisuje glebi.
            ZWrite Off
            ZTest LEqual

            Cull Back

            HLSLPROGRAM

            #pragma vertex Wierzcholki
            #pragma fragment Fragmenty

            // Biblioteka funkcji Unity URP.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Parametry materialu.
            CBUFFER_START(UnityPerMaterial)

                float4 _KolorSwiecenia;

                float _Intensywnosc;

                float _SilaFresnela;
                float _PotegaFresnela;

                float _PredkoscFali;
                float _CzestotliwoscFali;

                float _Pulsowanie;
                float _Widocznosc;

            CBUFFER_END

            // Dane wejsciowe wierzcholkow modelu.
            struct DaneWierzcholka
            {
                float4 pozycjaOS : POSITION;
                float3 normalnaOS : NORMAL;
            };

            // Dane przekazywane do shadera fragmentow.
            struct DaneFragmentu
            {
                float4 pozycjaHCS : SV_POSITION;

                float3 pozycjaWS : TEXCOORD0;
                float3 normalnaWS : TEXCOORD1;
                float3 pozycjaOS : TEXCOORD2;
            };

            // Shader wierzcholkow.
            DaneFragmentu Wierzcholki(
                DaneWierzcholka dane
            )
            {
                DaneFragmentu wynik;

                // Przeksztalcenie pozycji do przestrzeni ekranu.
                wynik.pozycjaHCS =
                    TransformObjectToHClip(
                        dane.pozycjaOS.xyz
                    );

                // Pozycja w przestrzeni swiata.
                wynik.pozycjaWS =
                    TransformObjectToWorld(
                        dane.pozycjaOS.xyz
                    );

                // Normalna powierzchni w przestrzeni swiata.
                wynik.normalnaWS =
                    TransformObjectToWorldNormal(
                        dane.normalnaOS
                    );

                // Pozycja lokalna do obliczania fal.
                wynik.pozycjaOS = dane.pozycjaOS.xyz;

                return wynik;
            }

            // Shader fragmentow - obliczanie wygladu.
            half4 Fragmenty(DaneFragmentu dane) : SV_Target
            {
                // NORMALNA I KIERUNEK KAMERY -------------------------------------------------------
                float3 normalna =
                    normalize(dane.normalnaWS);

                float3 kierunekKamery =
                    normalize(
                        GetCameraPositionWS()
                        - dane.pozycjaWS
                    );
                // EFEKT FRESNELA -------------------------------------------------------
                float iloczynSkalarny =
                    saturate(
                        dot(normalna, kierunekKamery)
                    );

                float fresnel = pow(
                    1.0 - iloczynSkalarny,
                    _PotegaFresnela
                );
                // ANIMOWANE PASMA ENERGII -------------------------------------------------------
                float fala = sin(
                    dane.pozycjaOS.y * _CzestotliwoscFali
                    + _Time.y * _PredkoscFali
                );

                // Zmiana zakresu sinusoidy z
                // [-1, 1] na [0, 1].
                fala = fala * 0.5 + 0.5;

                // LACZENIE EFEKTOW -------------------------------------------------------
                float swiecenie =
                    0.35
                    + fresnel * _SilaFresnela
                    + fala * 0.45;

                // Pulsowanie kontrolowane z C#.
                swiecenie *= _Pulsowanie;

                // KOLOR HDR -------------------------------------------------------
                float3 kolorKoncowy =
                    _KolorSwiecenia.rgb
                    * _Intensywnosc
                    * swiecenie;

                // PRZEZROCZYSTOSC -------------------------------------------------------
                float przezroczystosc =
                    saturate(_Widocznosc);

                return half4(
                    kolorKoncowy,
                    przezroczystosc
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
