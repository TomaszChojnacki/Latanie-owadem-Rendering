
Shader "WlasneEfekty/PromienieWolumetryczne"
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
            Name "RayMarchingSwiatla"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Fragmenty
            #pragma target 3.5

            // Warianty obslugi cieni glownego swiatla.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // PARAMETRY EFEKTU -----------------------------------------
            float4 _KolorPromieni;

            float _GestoscOsrodka;
            float _JasnoscPromieni;
            float _KierunkowoscSwiatla;

            float _SkalaSzumu;
            float _SilaSzumu;
            float _PredkoscSzumu;

            float _MaksymalnyZasieg;
            float _LiczbaProbek;

            float4 _SrodekObszaru;
            float4 _PolowaRozmiaru;

            // PROCEDURALNA GESTOSC POWIETRZA -----------------------------------------
            float ObliczSzum(float3 pozycja)
            {
                float czas = _Time.y * _PredkoscSzumu;

                float fala1 = sin(
                    pozycja.x * 1.37
                    + pozycja.z * 1.81
                    + czas
                );

                float fala2 = sin(
                    pozycja.y * 2.14
                    - pozycja.x * 0.93
                    - czas * 0.7
                );

                float fala3 = sin(
                    pozycja.z * 2.53
                    + pozycja.y * 1.47
                    + czas * 0.4
                );

                return
                    fala1 * 0.5 +
                    fala2 * 0.3 +
                    fala3 * 0.2;
            }

            // PRZECIECIE PROMIENIA Z OBSZAREM BARU -----------------------------------------
            bool ObliczPrzeciecieObszaru(
                float3 poczatek,
                float3 kierunek,
                out float odlegloscWejscia,
                out float odlegloscWyjscia
            )
            {
                float3 minimum =
                    _SrodekObszaru.xyz
                    - _PolowaRozmiaru.xyz;

                float3 maksimum =
                    _SrodekObszaru.xyz
                    + _PolowaRozmiaru.xyz;

                // Zabezpieczenie przed dzieleniem przez zero.
                float3 bezpiecznyKierunek = float3(
                    abs(kierunek.x) < 0.00001 ?
                        (kierunek.x >= 0 ? 0.00001 : -0.00001) :
                        kierunek.x,

                    abs(kierunek.y) < 0.00001 ?
                        (kierunek.y >= 0 ? 0.00001 : -0.00001) :
                        kierunek.y,

                    abs(kierunek.z) < 0.00001 ?
                        (kierunek.z >= 0 ? 0.00001 : -0.00001) :
                        kierunek.z
                );

                float3 odwrotnyKierunek =
                    1.0 / bezpiecznyKierunek;

                float3 t1 =
                    (minimum - poczatek) * odwrotnyKierunek;

                float3 t2 =
                    (maksimum - poczatek) * odwrotnyKierunek;

                float3 blizej = min(t1, t2);
                float3 dalej = max(t1, t2);

                odlegloscWejscia = max(
                    max(blizej.x, blizej.y),
                    blizej.z
                );

                odlegloscWyjscia = min(
                    min(dalej.x, dalej.y),
                    dalej.z
                );

                return
                    odlegloscWyjscia >
                    max(0.0, odlegloscWejscia);
            }

            // RENDEROWANIE WOLUMETRYCZNE -----------------------------------------
            half4 Fragmenty(Varyings dane) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(dane);

                float2 uv = dane.texcoord;

                // Odczyt gotowego obrazu kamery. ----
                half4 kolorSceny = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                if (_GestoscOsrodka <= 0.00001 ||
                    _JasnoscPromieni <= 0.00001)
                {
                    return kolorSceny;
                }

                // Odczyt glebi sceny. ----
                float glebia = SampleSceneDepth(uv);

                // Rozpoznawanie pikseli bez geometrii.
                bool czyNiebo;

                #if UNITY_REVERSED_Z
                    czyNiebo = glebia <= 0.00001;
                #else
                    czyNiebo = glebia >= 0.99999;
                #endif

                // Wyznaczanie kierunku promienia ----
                // przechodzacego przez dany piksel.
                float3 pozycjaKamery =
                    GetCameraPositionWS();

                float glebiaDalekiejPlaszczyzny;

                #if UNITY_REVERSED_Z
                    glebiaDalekiejPlaszczyzny = 0.0;
                #else
                    glebiaDalekiejPlaszczyzny = 1.0;
                #endif

                float3 punktDaleki =
                    ComputeWorldSpacePosition(
                        uv,
                        glebiaDalekiejPlaszczyzny,
                        UNITY_MATRIX_I_VP
                    );

                float3 kierunekPromienia = normalize(
                    punktDaleki - pozycjaKamery
                );

                float maksymalnaOdleglosc =
                    _MaksymalnyZasieg;

                // Promien konczy sie na najblizszej ----
                // nieprzezroczystej powierzchni.
                if (!czyNiebo)
                {
                    float3 pozycjaPowierzchni =
                        ComputeWorldSpacePosition(
                            uv,
                            glebia,
                            UNITY_MATRIX_I_VP
                        );

                    maksymalnaOdleglosc = min(
                        maksymalnaOdleglosc,
                        distance(
                            pozycjaKamery,
                            pozycjaPowierzchni
                        )
                    );
                }

                // Ograniczamy obliczenia do obszaru baru. ----
                float wejscie;
                float wyjscie;

                if (!ObliczPrzeciecieObszaru(
                    pozycjaKamery,
                    kierunekPromienia,
                    wejscie,
                    wyjscie))
                {
                    return kolorSceny;
                }

                float poczatek = max(0.0, wejscie);

                float koniec = min(
                    maksymalnaOdleglosc,
                    wyjscie
                );

                if (koniec <= poczatek)
                {
                    return kolorSceny;
                }

                // Parametry probkowania. ----
                int liczbaKrokow = clamp(
                    (int)_LiczbaProbek,
                    4,
                    32
                );

                float dlugoscKroku =
                    (koniec - poczatek) / liczbaKrokow;

                // Pobranie kierunku i koloru slonca. ----
                Light swiatlo = GetMainLight();

                float3 kierunekDoSwiatla =
                    normalize(swiatlo.direction);

                // Silniejsze rozpraszanie, kiedy
                // kamera patrzy w kierunku slonca.
                float zgodnoscKierunku = saturate(
                    dot(
                        kierunekPromienia,
                        kierunekDoSwiatla
                    ) * 0.5 + 0.5
                );

                float rozpraszanieKierunkowe =
                    pow(zgodnoscKierunku, 4.0) * 3.0;

                float wspolczynnikRozpraszania = lerp(
                    1.0,
                    rozpraszanieKierunkowe,
                    saturate(_KierunkowoscSwiatla)
                );

                // Akumulacja rozproszonego swiatla. ----
                float sumaSwiatla = 0.0;
                float przepuszczalnosc = 1.0;

                // GLOWNA PETLA RAY MARCHINGU -----------------------------------------
                [loop]
                for (int i = 0; i < 32; i++)
                {
                    if (i >= liczbaKrokow)
                        break;

                    // Próbkujemy srodek odcinka.
                    float dystans =
                        poczatek +
                        (i + 0.5) * dlugoscKroku;

                    float3 pozycjaProbki =
                        pozycjaKamery +
                        kierunekPromienia * dystans;

                    // Proceduralna gestosc osrodka.
                    float3 pozycjaSzum =
                        pozycjaProbki * _SkalaSzumu;

                    float szum = ObliczSzum(pozycjaSzum);

                    float gestoscLokalna = max(
                        0.0,
                        _GestoscOsrodka *
                        (1.0 + szum * _SilaSzumu)
                    );

                    // Sprawdzamy, czy probka lezy
                    // w swietle czy w cieniu.
                    float4 wspolrzedneCienia =
                        TransformWorldToShadowCoord(
                            pozycjaProbki
                        );

                    half oswietlenie =
                        MainLightRealtimeShadow(
                            wspolrzedneCienia
                        );

                    // Wykladniczy spadek transmisji.
                    float transmisjaKroku = exp(
                        -gestoscLokalna * dlugoscKroku
                    );

                    float rozproszenieKroku =
                        1.0 - transmisjaKroku;

                    // Dodajemy tylko oswietlone probki.
                    sumaSwiatla +=
                        przepuszczalnosc *
                        rozproszenieKroku *
                        oswietlenie;

                    // Kolejne probki sa coraz mniej
                    // widoczne przez osrodek.
                    przepuszczalnosc *= transmisjaKroku;
                }

                // Barwa i intensywnosc promieni. ----
                float3 kolorRozproszenia =
                    sumaSwiatla *
                    wspolczynnikRozpraszania *
                    _KolorPromieni.rgb *
                    swiatlo.color *
                    _JasnoscPromieni;

                // Polaczenie z obrazem sceny. ----
                float3 kolorWynikowy =
                    kolorSceny.rgb +
                    kolorRozproszenia;

                return half4(
                    kolorWynikowy,
                    kolorSceny.a
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
