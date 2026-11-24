
Shader "WlasneEfekty/AtmosferycznaMgla"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "AtmosferycznaMgla"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Fragmenty

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Dostep do tekstury glebi kamery.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Odczyt obrazu kamery i rysowanie
            // efektu na calym ekranie.
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // =====================================
            // PARAMETRY STEROWANE PRZEZ C#
            // =====================================

            float4 _MglaKolor;

            float _MglaGestosc;
            float _MglaPoczatek;
            float _MglaMaksymalnaWidocznosc;

            float _MglaWysokosc;
            float _MglaZakresWysokosci;

            float _MglaSilaZawirowan;
            float _MglaSkalaSzum;
            float _MglaPredkosc;

            // =====================================
            // PROCEDURALNY SZUM
            // =====================================

            float ObliczSzum(float3 pozycja, float czas)
            {
                // Pierwsza fala.
                float fala1 = sin(
                    pozycja.x * 1.73
                    + pozycja.z * 1.21
                    + czas
                );

                // Druga fala o innym kierunku.
                float fala2 = sin(
                    pozycja.z * 2.37
                    - pozycja.y * 0.83
                    - czas * 0.7
                );

                // Trzecia fala nadajaca
                // dodatkowa nieregularnosc.
                float fala3 = sin(
                    (pozycja.x + pozycja.y) * 1.41
                    + pozycja.z * 0.91
                    + czas * 0.45
                );

                // Wynik w zakresie zblizonym do [-1, 1].
                return (
                    fala1 * 0.5
                    + fala2 * 0.3
                    + fala3 * 0.2
                );
            }

            // =====================================
            // SHADER FRAGMENTOW
            // =====================================

            half4 Fragmenty(Varyings dane) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(dane);

                float2 uv = dane.texcoord;

                // 1. Pobranie oryginalnego obrazu.
                half4 kolorSceny = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                // Gdy mgla jest wylaczona,
                // nie wykonujemy dalszych obliczen.
                if (_MglaGestosc <= 0.00001)
                {
                    return kolorSceny;
                }

                // 2. Odczyt glebi.
                float surowaGlebia = SampleSceneDepth(uv);

                // Pomijamy tlo nieba, gdzie nie
                // ma geometrii zapisanej w glebi.
                #if UNITY_REVERSED_Z
                    if (surowaGlebia <= 0.00001)
                        return kolorSceny;
                #else
                    if (surowaGlebia >= 0.99999)
                        return kolorSceny;
                #endif

                // 3. Przeliczenie na liniowa odleglosc.
                float glebiaLiniowa = LinearEyeDepth(
                    surowaGlebia,
                    _ZBufferParams
                );

                // 4. Poczatkowy dystans mgly.
                float odlegloscMgly = max(
                    0.0,
                    glebiaLiniowa - _MglaPoczatek
                );

                // 5. Odtworzenie pozycji w swiecie.
                float glebiaNDC = surowaGlebia;

                #if !UNITY_REVERSED_Z
                    glebiaNDC = lerp(
                        UNITY_NEAR_CLIP_VALUE,
                        1.0,
                        surowaGlebia
                    );
                #endif

                float3 pozycjaWS =
                    ComputeWorldSpacePosition(
                        uv,
                        glebiaNDC,
                        UNITY_MATRIX_I_VP
                    );

                // 6. Wplyw wysokosci.
                // Nizej: gestsza mgla.
                // Wyzej: delikatniejsze zadymienie.
                float zakres = max(
                    0.01,
                    _MglaZakresWysokosci
                );

                float wspolczynnikWysokosci =
                    saturate(
                        (_MglaWysokosc - pozycjaWS.y)
                        / zakres
                    );

                // Zostawiamy minimalna gestosc
                // rowniez w gornej czesci baru.
                float gestoscWysokosci =
                    0.25 + wspolczynnikWysokosci * 0.75;

                // 7. Proceduralne zawirowania.
                float czas = _Time.y * _MglaPredkosc;

                float3 pozycjaSzum =
                    pozycjaWS * _MglaSkalaSzum;

                float szum = ObliczSzum(
                    pozycjaSzum,
                    czas
                );

                float modyfikatorSzum = max(
                    0.1,
                    1.0 + szum * _MglaSilaZawirowan
                );

                // 8. Gestosc calkowita.
                float gestosc = max(
                    0.0,
                    _MglaGestosc
                    * gestoscWysokosci
                    * modyfikatorSzum
                );

                // 9. Wykladniczy model zadymienia.
                float widocznosc = 1.0 - exp(
                    -odlegloscMgly * gestosc
                );

                // Ograniczenie intensywnosci
                // aby bar pozostal widoczny.
                widocznosc = min(
                    saturate(widocznosc),
                    saturate(_MglaMaksymalnaWidocznosc)
                );

                // 10. Polaczenie koloru sceny
                // z kolorem mgly.
                float3 kolorKoncowy = lerp(
                    kolorSceny.rgb,
                    _MglaKolor.rgb,
                    widocznosc
                );

                return half4(
                    kolorKoncowy,
                    kolorSceny.a
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
