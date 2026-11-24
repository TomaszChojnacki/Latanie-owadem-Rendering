
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SwiecacyPunktKontrolny : MonoBehaviour
{
    [Header("Punktacja")]
    [SerializeField] private int liczbaPunktow = 10;

    [Header("Ponowna aktywacja")]
    [SerializeField] private float czasOdnowienia = 10f;
    [SerializeField] private float czasZnikania = 0.5f;

    [Header("Animacja punktu")]
    [SerializeField] private float predkoscObrotu = 45f;
    [SerializeField] private float wysokoscUnoszenia = 0.15f;
    [SerializeField] private float predkoscUnoszenia = 2f;
    [SerializeField] private float predkoscPulsowania = 4f;

    [Header("Obiekt wizualny")]
    [SerializeField] private Transform modelPunktu;
    [SerializeField] private Renderer rendererPunktu;

    // Komponent odpowiedzialny za wykrywanie owada.
    private SphereCollider obszarZbierania;

    private OddychajacaAuraPunktu oddychajacaAura; // ----------- 2 oddech

    // Pozwala zmieniac parametry shadera
    // bez tworzenia osobnych kopii materialu.
    private MaterialPropertyBlock parametryMaterialu;

    // Pozycja poczatkowa modelu wzgledem
    // glownego obiektu punktu kontrolnego.
    private Vector3 pozycjaPoczatkowa;

    // Losowe przesuniecie animacji.
    private float przesuniecieAnimacji;

    // Czy punkt moze zostac aktualnie zebrany?
    private bool czyAktywny = true;

    // Identyfikatory parametrow shadera.
    private static readonly int IdPulsowania =
        Shader.PropertyToID("_Pulsowanie");

    private static readonly int IdWidocznosci =
        Shader.PropertyToID("_Widocznosc");

    // Tymczasowy system punktacji dla calej gry.
    private static int wynikCalkowity = 0;
    private static int liczbaZebran = 0;

    public static int WynikCalkowity => wynikCalkowity;
    public static int LiczbaZebran => liczbaZebran;

    // Reset danych przy rozpoczeciu nowej gry.
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void WyzerujWyniki()
    {
        wynikCalkowity = 0;
        liczbaZebran = 0;
    }

    private void Awake()
    {
        oddychajacaAura = GetComponent<OddychajacaAuraPunktu>();
        // Pobieramy komponent kolizji.
        obszarZbierania = GetComponent<SphereCollider>();

        // Punkt kontrolny ma wykrywac wejscie,
        // a nie blokowac ruch owada.
        obszarZbierania.isTrigger = true;

        // Automatycznie szukamy modelu w dzieciach.
        if (modelPunktu == null)
        {
            Renderer znalezionyRenderer =
                GetComponentInChildren<Renderer>(true);

            if (znalezionyRenderer != null)
            {
                modelPunktu =
                    znalezionyRenderer.transform;
            }
        }

        // Szukamy renderera, jezeli nie jest przypisany.
        if (rendererPunktu == null && modelPunktu != null)
        {
            rendererPunktu =
                modelPunktu.GetComponent<Renderer>();
        }

        // Sprawdzamy poprawnosc konfiguracji.
        if (modelPunktu == null || rendererPunktu == null)
        {
            Debug.LogError(
                "Brak modelu lub renderera punktu: "
                + gameObject.name,
                this
            );

            enabled = false;
            return;
        }

        // Tworzymy zestaw parametrow shadera.
        parametryMaterialu = new MaterialPropertyBlock();

        // Zapamietujemy lokalna pozycje modelu.
        pozycjaPoczatkowa = modelPunktu.localPosition;

        // Rozne punkty nie musza pulsowac identycznie.
        przesuniecieAnimacji =
            Random.Range(0f, Mathf.PI * 2f);

        // Ustawienia poczatkowe.
        czyAktywny = true;
        UstawWidocznosc(1f);
        UstawPulsowanie(1f);

        rendererPunktu.enabled = true;
    }

    private void Update()
    {
        if (!czyAktywny)
        {
            return;
        }

        AnimujPunkt();
    }

    // ANIMACJA PUNKTU KONTROLNEGO -------------------------------------------------------
    private void AnimujPunkt()
    {
        // Obracanie modelu wokol osi Y.
        modelPunktu.Rotate(
            Vector3.up,
            predkoscObrotu * Time.deltaTime,
            Space.Self
        );

        // Obliczanie unoszenia za pomoca sinusa.
        float przesuniecieY = Mathf.Sin(
            Time.time * predkoscUnoszenia
            + przesuniecieAnimacji
        ) * wysokoscUnoszenia;

        // Zmieniamy tylko pozycje modelu.
        // Glowny trigger pozostaje na miejscu.
        modelPunktu.localPosition =
            pozycjaPoczatkowa
            + Vector3.up * przesuniecieY;

        // Obliczanie pulsowania.
        float pulsowanie =
            0.7f + 0.3f * Mathf.Sin(
                Time.time * predkoscPulsowania
                + przesuniecieAnimacji
            );

        UstawPulsowanie(pulsowanie);
    }


    // WYKRYWANIE OWADA -------------------------------------------------------
    private void OnTriggerEnter(Collider innyObiekt)
    {
        if (!czyAktywny)
        {
            return;
        }

        // Sprawdzenie, czy w trigger wlecial gracz.
        bool czyGracz = innyObiekt.CompareTag("Player");

        // Obsluga colliderow znajdujacych sie
        // na obiektach potomnych owada.
        if (!czyGracz && innyObiekt.attachedRigidbody != null)
        {
            czyGracz =
                innyObiekt.attachedRigidbody.CompareTag("Player");
        }

        if (!czyGracz)
        {
            return;
        }

        ZbierzPunkt();
    }

    // ZEBRANIE PUNKTU -------------------------------------------------------
    private void ZbierzPunkt()
    {
        // Blokujemy wielokrotne zebranie.
        czyAktywny = false;

        // Rozpoczynamy wygaszanie otaczajacej aury.
        if (oddychajacaAura != null)
        {
            oddychajacaAura.UstawAktywnosc(false);
        }
        obszarZbierania.enabled = false;


        // Aktualizacja punktacji.
        wynikCalkowity += liczbaPunktow;
        liczbaZebran++;

        Debug.Log(
            "Zebrano punkt kontrolny! "
            + "Dodano: " + liczbaPunktow
            + " pkt. Wynik: " + wynikCalkowity
            + ". Liczba zebran: " + liczbaZebran
        );

        // Rozpoczynamy zanikanie i odnawianie.
        StartCoroutine(OdnowPunkt());
    }

    // ZNIKANIE I PONOWNA AKTYWACJA -------------------------------------------------------
    private IEnumerator OdnowPunkt()
    {
        float czas = 0f;

        float czasAnimacji =
            Mathf.Max(0.01f, czasZnikania);

        // Stopniowo zmniejszamy przezroczystosc.
        while (czas < czasAnimacji)
        {
            czas += Time.deltaTime;

            float widocznosc = 1f - Mathf.Clamp01(
                czas / czasAnimacji
            );

            UstawWidocznosc(widocznosc);

            yield return null;
        }

        // Ukrywamy model po zakonczeniu zanikania.
        UstawWidocznosc(0f);
        rendererPunktu.enabled = false;

        // Oczekiwanie na odnowienie.
        yield return new WaitForSeconds(
            Mathf.Max(0f, czasOdnowienia)
        );

        // Przywracamy polozenie modelu.
        modelPunktu.localPosition = pozycjaPoczatkowa;

        // Przywracamy wyglad.
        UstawWidocznosc(1f);
        UstawPulsowanie(1f);

        rendererPunktu.enabled = true;

        // Ponownie aktywujemy punkt kontrolny.
        obszarZbierania.enabled = true;

        // Przywracamy pulsujaca aure po odnowieniu punktu.
        if (oddychajacaAura != null)
        {
            oddychajacaAura.UstawAktywnosc(true);
        }


        czyAktywny = true;
    }


    // ZMIANA PULSOWANIA SHADERA -------------------------------------------------------
    private void UstawPulsowanie(float wartosc)
    {
        rendererPunktu.GetPropertyBlock(parametryMaterialu);

        parametryMaterialu.SetFloat(
            IdPulsowania,
            wartosc
        );

        rendererPunktu.SetPropertyBlock(parametryMaterialu);
    }


    // ZMIANA WIDOCZNOSCI SHADERA -------------------------------------------------------
    private void UstawWidocznosc(float wartosc)
    {
        rendererPunktu.GetPropertyBlock(parametryMaterialu);

        parametryMaterialu.SetFloat(
            IdWidocznosci,
            wartosc
        );

        rendererPunktu.SetPropertyBlock(parametryMaterialu);
    }
}
