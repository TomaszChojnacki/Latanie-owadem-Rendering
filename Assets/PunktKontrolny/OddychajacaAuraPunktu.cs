
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OddychajacaAuraPunktu : MonoBehaviour
{
    [Header("Centralna kula")]
    [SerializeField]
    private Transform modelPunktu;

    [Header("Material czasteczek")]
    [SerializeField]
    private Material materialCzasteczek;

    [Header("Liczba i wielkosc")]
    [Range(8, 100)]
    [SerializeField]
    private int liczbaCzasteczek = 36;

    [SerializeField]
    private float wielkoscCzasteczek = 0.065f;

    [Range(0f, 1f)]
    [SerializeField]
    private float roznorodnoscWielkosci = 0.5f;

    [Header("Oddychanie aury")]
    [SerializeField]
    private float promienAury = 0.85f;

    [SerializeField]
    private float silaOddychania = 0.25f;

    [SerializeField]
    private float predkoscOddychania = 1.8f;

    [Header("Ruch czasteczek")]
    [SerializeField]
    private float predkoscWirowania = 12f;

    [SerializeField]
    private float silaDrgania = 0.045f;

    [SerializeField]
    private float predkoscDrgania = 2.5f;

    [Header("Pulsowanie")]
    [SerializeField]
    private float silaPulsowania = 0.3f;

    [Header("Zanikanie")]
    [SerializeField]
    private float szybkoscZanikania = 5f;

    // Dane jednej czasteczki.
    private struct DaneCzasteczki
    {
        public Vector3 kierunek;
        public float wielkosc;
        public float przesuniecie;
    }

    // Wszystkie czasteczki sa czescia jednej siatki.
    private DaneCzasteczki[] daneCzasteczek;

    private readonly List<Vector3> wierzcholki =
        new List<Vector3>();

    private readonly List<Vector3> normalne =
        new List<Vector3>();

    private Mesh siatkaAury;
    private MeshRenderer rendererAury;
    private Transform kontenerAury;

    private MaterialPropertyBlock parametryMaterialu;

    private bool czyAuraAktywna = true;
    private float widocznoscAury = 1f;

    private static readonly int IdWidocznosci =
        Shader.PropertyToID("_Widocznosc");

    // Kierunki wierzcholkow malego osmiscianu.
    // Zastepuje on duza, wielokatna kule.
    private static readonly Vector3[] kierunkiBryly =
    {
        Vector3.up,
        Vector3.down,
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };

    // Osiem trojkatow jednego osmiscianu.
    private static readonly int[] indeksyBryly =
    {
        0, 4, 2,
        0, 3, 4,
        0, 5, 3,
        0, 2, 5,

        1, 2, 4,
        1, 4, 3,
        1, 3, 5,
        1, 5, 2
    };

    private void Start()
    {
        if (materialCzasteczek == null)
        {
            Debug.LogError(
                "Nie przypisano materialu czasteczek!",
                this
            );

            enabled = false;
            return;
        }

        // Automatyczne wyszukiwanie centralnej kuli.
        if (modelPunktu == null)
        {
            modelPunktu = transform.Find("ModelPunktu");
        }

        if (modelPunktu == null)
        {
            Debug.LogError(
                "Nie znaleziono obiektu ModelPunktu!",
                this
            );

            enabled = false;
            return;
        }

        parametryMaterialu = new MaterialPropertyBlock();

        PrzygotujCzasteczki();
        UtworzWspolnaSiatke();

        Debug.Log(
            "Utworzono aure. " +
            "Liczba czasteczek: " + liczbaCzasteczek
        );
    }

    // PRZYGOTOWANIE DANYCH CZASTECZEK -----------------------------
    private void PrzygotujCzasteczki()
    {
        daneCzasteczek =
            new DaneCzasteczki[liczbaCzasteczek];

        // Zloty kat zapewnia rownomierne
        // rozmieszczenie punktow na kuli.
        float zlotyKat =
            Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (int i = 0; i < liczbaCzasteczek; i++)
        {
            float wysokosc = 1f -
                2f * (i + 0.5f) / liczbaCzasteczek;

            float promienPoziomy = Mathf.Sqrt(
                Mathf.Max(0f, 1f - wysokosc * wysokosc)
            );

            float kat = i * zlotyKat;

            Vector3 kierunek = new Vector3(
                Mathf.Cos(kat) * promienPoziomy,
                wysokosc,
                Mathf.Sin(kat) * promienPoziomy
            );

            float losowaWielkosc = Random.Range(
                1f - roznorodnoscWielkosci,
                1f
            );

            daneCzasteczek[i] = new DaneCzasteczki
            {
                kierunek = kierunek.normalized,

                wielkosc =
                    wielkoscCzasteczek * losowaWielkosc,

                przesuniecie = Random.Range(
                    0f,
                    Mathf.PI * 2f
                )
            };
        }
    }

    // TWORZENIE JEDNEGO MODELU 3D -----------------------------
    private void UtworzWspolnaSiatke()
    {
        GameObject obiekt = new GameObject(
            "Aura"
        );

        obiekt.transform.SetParent(transform, false);

        kontenerAury = obiekt.transform;

        MeshFilter filtr =
            obiekt.AddComponent<MeshFilter>();

        rendererAury =
            obiekt.AddComponent<MeshRenderer>();

        rendererAury.sharedMaterial = materialCzasteczek;

        // Wylaczamy niepotrzebne cienie.
        rendererAury.shadowCastingMode =
            ShadowCastingMode.Off;

        rendererAury.receiveShadows = false;

        siatkaAury = new Mesh();
        siatkaAury.name = "ProceduralnaSiatkaAury";

        // Informujemy Unity o czestych zmianach siatki.
        siatkaAury.MarkDynamic();

        int liczbaWierzcholkow =
            liczbaCzasteczek * kierunkiBryly.Length;

        int[] trojkaty =
            new int[liczbaCzasteczek * indeksyBryly.Length];

        // Rezerwujemy pamiec tylko raz.
        wierzcholki.Capacity = liczbaWierzcholkow;
        normalne.Capacity = liczbaWierzcholkow;

        for (int i = 0; i < liczbaCzasteczek; i++)
        {
            int poczatekWierzcholkow =
                i * kierunkiBryly.Length;

            // Dodajemy wierzcholki malej bryly.
            for (int j = 0; j < kierunkiBryly.Length; j++)
            {
                wierzcholki.Add(Vector3.zero);

                // Normalne potrzebne dla Fresnela.
                normalne.Add(kierunkiBryly[j]);
            }

            // Dodajemy indeksy trojkatow.
            for (int j = 0; j < indeksyBryly.Length; j++)
            {
                int indeksDocelowy =
                    i * indeksyBryly.Length + j;

                trojkaty[indeksDocelowy] =
                    poczatekWierzcholkow + indeksyBryly[j];
            }
        }

        siatkaAury.SetVertices(wierzcholki);
        siatkaAury.SetNormals(normalne);
        siatkaAury.SetTriangles(trojkaty, 0, false);

        // Staly obszar zawierajacy wszystkie czasteczki.
        // Nie musimy liczyc granic w kazdej klatce.
        float maksymalnyPromien =
            Mathf.Max(0.01f, promienAury)
            + Mathf.Abs(silaOddychania)
            + Mathf.Abs(silaDrgania)
            + Mathf.Abs(wielkoscCzasteczek)
            * (1f + Mathf.Abs(silaPulsowania));

        siatkaAury.bounds = new Bounds(
            Vector3.zero,
            Vector3.one * (maksymalnyPromien * 2f + 0.5f)
        );

        filtr.sharedMesh = siatkaAury;
    }

    // AKTUALIZACJA PO ANIMACJI KULI -----------------------------
    private void LateUpdate()
    {
        if (kontenerAury == null || modelPunktu == null)
            return;

        // Aura podaza za unoszaca sie kula.
        kontenerAury.localPosition =
            transform.InverseTransformPoint(
                modelPunktu.position
            );

        float docelowaWidocznosc =
            czyAuraAktywna ? 1f : 0f;

        widocznoscAury = Mathf.MoveTowards(
            widocznoscAury,
            docelowaWidocznosc,
            szybkoscZanikania * Time.deltaTime
        );

        // Nie aktualizujemy geometrii, gdy
        // punkt jest calkowicie niewidoczny.
        if (!czyAuraAktywna && widocznoscAury <= 0.001f)
        {
            rendererAury.enabled = false;
            return;
        }

        rendererAury.enabled = true;

        AnimujSiatke();
        UstawWidocznoscMaterialu();
    }

    // ANIMACJA WSZYSTKICH CZASTECZEK -----------------------------
    private void AnimujSiatke()
    {
        float czas = Time.time;

        // Jeden wspolny rytm oddychania.
        float oddech = Mathf.Sin(
            czas * predkoscOddychania
        );

        float aktualnyPromien =
            promienAury + oddech * silaOddychania;

        Quaternion obrot = Quaternion.Euler(
            0f,
            czas * predkoscWirowania,
            0f
        );

        for (int i = 0; i < daneCzasteczek.Length; i++)
        {
            DaneCzasteczki dane = daneCzasteczek[i];

            // Obrot kierunku czasteczki.
            Vector3 kierunek = obrot * dane.kierunek;

            // Indywidualne, delikatne drganie.
            float drganie = Mathf.Sin(
                czas * predkoscDrgania
                + dane.przesuniecie
            ) * silaDrgania;

            float odleglosc = Mathf.Max(
                0.01f,
                aktualnyPromien + drganie
            );

            Vector3 srodekCzasteczki =
                kierunek * odleglosc;

            // Wspolne pulsowanie wielkosci.
            float pulsowanie =
                1f + oddech * silaPulsowania * 0.5f;

            float dodatkowyPuls = 1f +
                Mathf.Sin(
                    czas * 3f + dane.przesuniecie
                ) * 0.15f;

            float rozmiar =
                dane.wielkosc
                * pulsowanie
                * dodatkowyPuls
                * 0.5f;

            // Obliczamy 6 wierzcholkow
            // kazdej malej bryly.
            int poczatek = i * kierunkiBryly.Length;

            for (int j = 0; j < kierunkiBryly.Length; j++)
            {
                wierzcholki[poczatek + j] =
                    srodekCzasteczki
                    + kierunkiBryly[j] * rozmiar;
            }
        }

        // Jedna aktualizacja calej siatki.
        // Nie przeliczamy granic przy kazdej zmianie.
        siatkaAury.SetVertices(
            wierzcholki,
            0,
            wierzcholki.Count,
            MeshUpdateFlags.DontRecalculateBounds
        );
    }

    // STEROWANIE PRZEZROCZYSTOSCIA -----------------------------
    private void UstawWidocznoscMaterialu()
    {
        rendererAury.GetPropertyBlock(parametryMaterialu);

        parametryMaterialu.SetFloat(
            IdWidocznosci,
            widocznoscAury
        );

        rendererAury.SetPropertyBlock(parametryMaterialu);
    }

    // WYLACZANIE I WLACZANIE AURY -----------------------------
    public void UstawAktywnosc(bool aktywna)
    {
        czyAuraAktywna = aktywna;
    }

    // ZWALNIANIE PAMIECI -----------------------------
    private void OnDestroy()
    {
        // Usuwamy jedynie siatke utworzona
        // przez ten skrypt podczas gry.
        if (siatkaAury != null)
        {
            Destroy(siatkaAury);
        }
    }
}
