
using UnityEngine;

[ExecuteAlways]
public class AtmosferycznaMgla : MonoBehaviour
{
    [Header("Kolor zadymienia")]
    [SerializeField]
    private Color kolorMgly = new Color(
        0.55f, 0.43f, 0.30f, 1f
    );

    [Header("Gestosc i zasieg")]
    [Range(0f, 0.5f)]
    [SerializeField]
    private float gestoscMgly = 0.075f;

    [Min(0f)]
    [SerializeField]
    private float poczatekMgly = 1.5f;

    [Range(0f, 1f)]
    [SerializeField]
    private float maksymalnaWidocznosc = 0.65f;

    [Header("Zaleznosc od wysokosci")]
    [SerializeField]
    private float wysokoscMgly = 3f;

    [Min(0.01f)]
    [SerializeField]
    private float zakresWysokosci = 3f;

    [Header("Animacja zadymienia")]
    [Range(0f, 1f)]
    [SerializeField]
    private float silaZawirowan = 0.3f;

    [Range(0.01f, 5f)]
    [SerializeField]
    private float skalaSzumu = 0.35f;

    [Range(0f, 2f)]
    [SerializeField]
    private float predkoscMgly = 0.12f;

    [Header("Sterowanie efektem")]
    [SerializeField]
    private bool wlaczMgla = true;

    // Identyfikatory zmiennych wykorzystywanych
    // przez autorski shader HLSL.
    private static readonly int IdKoloru =
        Shader.PropertyToID("_MglaKolor");

    private static readonly int IdGestosci =
        Shader.PropertyToID("_MglaGestosc");

    private static readonly int IdPoczatku =
        Shader.PropertyToID("_MglaPoczatek");

    private static readonly int IdMaksimum =
        Shader.PropertyToID("_MglaMaksymalnaWidocznosc");

    private static readonly int IdWysokosci =
        Shader.PropertyToID("_MglaWysokosc");

    private static readonly int IdZakresu =
        Shader.PropertyToID("_MglaZakresWysokosci");

    private static readonly int IdSilyZawirowan =
        Shader.PropertyToID("_MglaSilaZawirowan");

    private static readonly int IdSkaliSzumu =
        Shader.PropertyToID("_MglaSkalaSzum");

    private static readonly int IdPredkosci =
        Shader.PropertyToID("_MglaPredkosc");

    // =====================================
    // INICJALIZACJA
    // =====================================

    private void OnEnable()
    {
        AktualizujParametry();
    }

    // =====================================
    // AKTUALIZACJA PARAMETROW
    // =====================================

    private void Update()
    {
        // Parametry globalne sa aktualizowane
        // takze podczas edycji sceny.
        AktualizujParametry();
    }

    private void OnValidate()
    {
        gestoscMgly = Mathf.Max(0f, gestoscMgly);
        poczatekMgly = Mathf.Max(0f, poczatekMgly);
        zakresWysokosci = Mathf.Max(
            0.01f,
            zakresWysokosci
        );

        skalaSzumu = Mathf.Max(
            0.01f,
            skalaSzumu
        );
    }

    // =====================================
    // PRZEKAZYWANIE DANYCH DO GPU
    // =====================================

    private void AktualizujParametry()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        Shader.SetGlobalColor(
            IdKoloru,
            kolorMgly
        );

        Shader.SetGlobalFloat(
            IdGestosci,
            wlaczMgla ? gestoscMgly : 0f
        );

        Shader.SetGlobalFloat(
            IdPoczatku,
            poczatekMgly
        );

        Shader.SetGlobalFloat(
            IdMaksimum,
            maksymalnaWidocznosc
        );

        Shader.SetGlobalFloat(
            IdWysokosci,
            wysokoscMgly
        );

        Shader.SetGlobalFloat(
            IdZakresu,
            zakresWysokosci
        );

        Shader.SetGlobalFloat(
            IdSilyZawirowan,
            silaZawirowan
        );

        Shader.SetGlobalFloat(
            IdSkaliSzumu,
            skalaSzumu
        );

        Shader.SetGlobalFloat(
            IdPredkosci,
            predkoscMgly
        );
    }

    // =====================================
    // WLACZANIE ZADYMIENIA
    // =====================================

    public void WlaczZadymienie()
    {
        wlaczMgla = true;
        AktualizujParametry();
    }

    // =====================================
    // WYLACZANIE ZADYMIENIA
    // =====================================

    public void WylaczZadymienie()
    {
        wlaczMgla = false;
        AktualizujParametry();
    }

    // =====================================
    // ZMIANA GESTOSCI
    // =====================================

    public void UstawGestosc(float nowaGestosc)
    {
        gestoscMgly = Mathf.Clamp(
            nowaGestosc,
            0f,
            0.5f
        );

        AktualizujParametry();
    }

    // =====================================
    // WYLACZENIE KOMPONENTU
    // =====================================

    private void OnDisable()
    {
        // Po usunieciu komponentu mgla nie
        // powinna pozostac na ekranie.
        Shader.SetGlobalFloat(IdGestosci, 0f);
    }
}
