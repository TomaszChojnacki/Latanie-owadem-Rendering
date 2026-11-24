
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(ParticleSystemRenderer))]
public class UnoszacySieKurz : MonoBehaviour
{
    [Header("Material")]
    [SerializeField]
    private Material materialKurzu;

    [Header("Obszar wystepowania kurzu")]
    [SerializeField]
    private Vector3 rozmiarObszaru =
        new Vector3(12f, 3f, 12f);

    [Header("Liczba czasteczek")]
    [Range(1f, 50f)]
    [SerializeField]
    private float liczbaNaSekunde = 15f;

    [Range(20, 1000)]
    [SerializeField]
    private int maksymalnaLiczbaCzasteczek = 220;

    [Header("Czas zycia")]
    [SerializeField]
    private float minimalnyCzasZycia = 8f;

    [SerializeField]
    private float maksymalnyCzasZycia = 14f;

    [Header("Wielkosc")]
    [SerializeField]
    private float minimalnaWielkosc = 0.025f;

    [SerializeField]
    private float maksymalnaWielkosc = 0.09f;

    [Header("Ruch kurzu")]
    [SerializeField]
    private float minimalnaPredkoscWznoszenia = 0.005f;

    [SerializeField]
    private float maksymalnaPredkoscWznoszenia = 0.025f;

    [SerializeField]
    private float ruchPoziomy = 0.018f;

    [Header("Nieregularny ruch powietrza")]
    [SerializeField]
    private float silaZawirowan = 0.08f;

    [SerializeField]
    private float czestotliwoscZawirowan = 0.4f;

    [SerializeField]
    private float predkoscZmianyZawirowan = 0.15f;

    private ParticleSystem systemKurzu;
    private ParticleSystemRenderer rendererKurzu;

    private void Awake()
    {
        systemKurzu = GetComponent<ParticleSystem>();

        rendererKurzu =
            GetComponent<ParticleSystemRenderer>();

        if (materialKurzu == null)
        {
            Debug.LogError(
                "Nie przypisano materialu kurzu: "
                + gameObject.name,
                this
            );

            enabled = false;
            systemKurzu.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            return;
        }

        PrzygotujSystemKurzu();
    }

    // =====================================
    // GLOWNA KONFIGURACJA
    // =====================================

    private void PrzygotujSystemKurzu()
    {
        // Zatrzymujemy system przed konfiguracja.
        systemKurzu.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        KonfigurujGlowneUstawienia();
        KonfigurujEmisje();
        KonfigurujObszar();
        KonfigurujRuch();
        KonfigurujZawirowania();
        KonfigurujZanikanie();
        KonfigurujRenderer();

        systemKurzu.Play();

        Debug.Log(
            "Uruchomiono efekt unoszacego sie kurzu: "
            + gameObject.name
        );
    }

    // =====================================
    // PODSTAWOWE PARAMETRY CZASTECZEK
    // =====================================

    private void KonfigurujGlowneUstawienia()
    {
        var glowne = systemKurzu.main;

        glowne.loop = true;
        glowne.playOnAwake = false;

        // Czasteczki pozostaja wewnatrz
        // lokalnego obszaru systemu.
        glowne.simulationSpace =
            ParticleSystemSimulationSpace.Local;

        glowne.startLifetime =
            new ParticleSystem.MinMaxCurve(
                Mathf.Max(0.1f, minimalnyCzasZycia),
                Mathf.Max(
                    minimalnyCzasZycia,
                    maksymalnyCzasZycia
                )
            );

        glowne.startSize =
            new ParticleSystem.MinMaxCurve(
                Mathf.Max(0.001f, minimalnaWielkosc),
                Mathf.Max(
                    minimalnaWielkosc,
                    maksymalnaWielkosc
                )
            );

        // Poczatkowa predkosc jest sterowana
        // osobno w module Velocity over Lifetime.
        glowne.startSpeed = 0f;

        glowne.startColor = Color.white;

        glowne.maxParticles =
            maksymalnaLiczbaCzasteczek;

        glowne.gravityModifier = 0f;

        // Nie potrzebujemy oswietlenia czasteczek.
        glowne.startRotation = 0f;
    }

    // =====================================
    // EMISJA NOWYCH CZASTECZEK
    // =====================================

    private void KonfigurujEmisje()
    {
        var emisja = systemKurzu.emission;

        emisja.enabled = true;

        emisja.rateOverTime =
            new ParticleSystem.MinMaxCurve(
                liczbaNaSekunde
            );

        // Nie tworzymy dodatkowych eksplozji.
        emisja.rateOverDistance = 0f;

        emisja.SetBursts(
            new ParticleSystem.Burst[0]
        );
    }

    // =====================================
    // OBSZAR WYSTEPOWANIA KURZU
    // =====================================

    private void KonfigurujObszar()
    {
        var ksztalt = systemKurzu.shape;

        ksztalt.enabled = true;

        // Kurz pojawia sie w prostopadloscianie.
        ksztalt.shapeType =
            ParticleSystemShapeType.Box;

        ksztalt.scale = rozmiarObszaru;

        ksztalt.position = Vector3.zero;

        ksztalt.rotation = Vector3.zero;
    }

    // =====================================
    // POWOLNY RUCH POWIETRZA
    // =====================================

    private void KonfigurujRuch()
    {
        var ruch = systemKurzu.velocityOverLifetime;

        ruch.enabled = true;

        ruch.space =
            ParticleSystemSimulationSpace.Local;

        // Losowy ruch w poziomie.
        ruch.x = new ParticleSystem.MinMaxCurve(
            -ruchPoziomy,
            ruchPoziomy
        );

        ruch.z = new ParticleSystem.MinMaxCurve(
            -ruchPoziomy,
            ruchPoziomy
        );

        // Delikatne unoszenie w gore.
        ruch.y = new ParticleSystem.MinMaxCurve(
            minimalnaPredkoscWznoszenia,
            maksymalnaPredkoscWznoszenia
        );
    }

    // =====================================
    // NATURALNE ZAWIROWANIA
    // =====================================

    private void KonfigurujZawirowania()
    {
        var szum = systemKurzu.noise;

        szum.enabled = true;

        // Subtelne zaburzenia ruchu.
        szum.strengthMultiplier =
            silaZawirowan;

        szum.frequency =
            czestotliwoscZawirowan;

        szum.scrollSpeed =
            predkoscZmianyZawirowan;

        szum.damping = true;

        // Nizsza jakosc szumu jest wystarczajaca
        // dla delikatnego kurzu w powietrzu.
        szum.quality =
            ParticleSystemNoiseQuality.Low;
    }

    // =====================================
    // PLYNNE POJAWIANIE I ZANIKANIE
    // =====================================

    private void KonfigurujZanikanie()
    {
        var kolorWCzasie =
            systemKurzu.colorOverLifetime;

        kolorWCzasie.enabled = true;

        Gradient gradient = new Gradient();

        // Kolor pozostaje neutralny.
        // Barwe nadaje wlasny shader.
        GradientColorKey[] kolory =
        {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f)
        };

        // Czasteczka na poczatku jest niewidoczna,
        // stopniowo sie pojawia, a na koncu zanika.
        GradientAlphaKey[] przezroczystosc =
        {
            new GradientAlphaKey(0f, 0f),
            new GradientAlphaKey(0.7f, 0.15f),
            new GradientAlphaKey(0.7f, 0.75f),
            new GradientAlphaKey(0f, 1f)
        };

        gradient.SetKeys(
            kolory,
            przezroczystosc
        );

        kolorWCzasie.color =
            new ParticleSystem.MinMaxGradient(
                gradient
            );
    }

    // =====================================
    // RENDEROWANIE
    // =====================================

    private void KonfigurujRenderer()
    {
        // Czasteczki sa plaskie i skierowane do kamery.
        rendererKurzu.renderMode =
            ParticleSystemRenderMode.Billboard;

        // Przypisanie autorskiego materialu.
        rendererKurzu.sharedMaterial = materialKurzu;

        // Wylaczenie rzucania i odbierania cieni.
        rendererKurzu.shadowCastingMode =
            ShadowCastingMode.Off;

        rendererKurzu.receiveShadows = false;

        // Lista danych przekazywanych do shadera.
        List<ParticleSystemVertexStream> strumienie =
            new List<ParticleSystemVertexStream>
            {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV
            };

        // Przekazanie listy do systemu czasteczek.
        rendererKurzu.SetActiveVertexStreams(strumienie);
    }

    // =====================================
    // ZMIANA INTENSYWNOSCI EFEKTU
    // =====================================

    public void UstawIntensywnosc(float mnoznik)
    {
        var emisja = systemKurzu.emission;

        emisja.rateOverTime =
            liczbaNaSekunde
            * Mathf.Max(0f, mnoznik);
    }

    // =====================================
    // ZATRZYMANIE EFEKTU
    // =====================================

    public void ZatrzymajKurz()
    {
        systemKurzu.Stop(
            true,
            ParticleSystemStopBehavior.StopEmitting
        );
    }

    // =====================================
    // PONOWNE URUCHOMIENIE
    // =====================================

    public void UruchomKurz()
    {
        systemKurzu.Play();
    }
}
