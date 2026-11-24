
using UnityEngine;

[ExecuteAlways]
public class PromienieWolumetryczne : MonoBehaviour
{
    [Header("Sterowanie")]
    [SerializeField]
    private bool wlaczPromienie = true;

    [Header("Kolor i intensywnosc")]
    [SerializeField]
    private Color kolorPromieni =
        new Color(1f, 0.72f, 0.38f, 1f);

    [Range(0f, 5f)]
    [SerializeField]
    private float jasnoscPromieni = 1.6f;

    [Range(0f, 0.5f)]
    [SerializeField]
    private float gestoscOsrodka = 0.09f;

    [Range(0f, 1f)]
    [SerializeField]
    private float kierunkowoscSwiatla = 0.5f;

    [Header("Proceduralne zawirowania")]
    [Range(0.01f, 3f)]
    [SerializeField]
    private float skalaSzumu = 0.3f;

    [Range(0f, 1f)]
    [SerializeField]
    private float silaSzumu = 0.3f;

    [Range(0f, 2f)]
    [SerializeField]
    private float predkoscSzumu = 0.08f;

    [Header("Jakosc renderowania")]
    [Range(4, 32)]
    [SerializeField]
    private int liczbaProbek = 12;

    [Min(0.1f)]
    [SerializeField]
    private float maksymalnyZasieg = 35f;

    [Header("Obszar zadymienia")]
    [SerializeField]
    private Vector3 rozmiarObszaru =
        new Vector3(25f, 10f, 25f);

    // Identyfikatory parametrow shadera.
    private static readonly int IdKoloru =
        Shader.PropertyToID("_KolorPromieni");

    private static readonly int IdGestosci =
        Shader.PropertyToID("_GestoscOsrodka");

    private static readonly int IdJasnosci =
        Shader.PropertyToID("_JasnoscPromieni");

    private static readonly int IdKierunkowosci =
        Shader.PropertyToID("_KierunkowoscSwiatla");

    private static readonly int IdSkaliSzumu =
        Shader.PropertyToID("_SkalaSzumu");

    private static readonly int IdSilySzumu =
        Shader.PropertyToID("_SilaSzumu");

    private static readonly int IdPredkosciSzumu =
        Shader.PropertyToID("_PredkoscSzumu");

    private static readonly int IdLiczbyProbek =
        Shader.PropertyToID("_LiczbaProbek");

    private static readonly int IdZasiegu =
        Shader.PropertyToID("_MaksymalnyZasieg");

    private static readonly int IdSrodka =
        Shader.PropertyToID("_SrodekObszaru");

    private static readonly int IdPolowyRozmiaru =
        Shader.PropertyToID("_PolowaRozmiaru");

    // INICJALIZACJA -----------------------------------------
    private void OnEnable()
    {
        AktualizujParametry();
    }

    private void Update()
    {
        AktualizujParametry();
    }

    // AKTUALIZACJA PARAMETROW GPU -----------------------------------------
    private void AktualizujParametry()
    {
        if (!isActiveAndEnabled)
            return;

        Shader.SetGlobalColor(
            IdKoloru,
            kolorPromieni
        );

        Shader.SetGlobalFloat(
            IdGestosci,
            wlaczPromienie ? gestoscOsrodka : 0f
        );

        Shader.SetGlobalFloat(
            IdJasnosci,
            jasnoscPromieni
        );

        Shader.SetGlobalFloat(
            IdKierunkowosci,
            kierunkowoscSwiatla
        );

        Shader.SetGlobalFloat(
            IdSkaliSzumu,
            skalaSzumu
        );

        Shader.SetGlobalFloat(
            IdSilySzumu,
            silaSzumu
        );

        Shader.SetGlobalFloat(
            IdPredkosciSzumu,
            predkoscSzumu
        );

        Shader.SetGlobalFloat(
            IdLiczbyProbek,
            liczbaProbek
        );

        Shader.SetGlobalFloat(
            IdZasiegu,
            maksymalnyZasieg
        );

        // Pozycja obiektu oznacza srodek
        // obszaru, w ktorym znajduje sie mgla.
        Vector3 srodek = transform.position;

        Shader.SetGlobalVector(
            IdSrodka,
            new Vector4(
                srodek.x,
                srodek.y,
                srodek.z,
                0f
            )
        );

        Vector3 polowaRozmiaru =
            rozmiarObszaru * 0.5f;

        Shader.SetGlobalVector(
            IdPolowyRozmiaru,
            new Vector4(
                polowaRozmiaru.x,
                polowaRozmiaru.y,
                polowaRozmiaru.z,
                0f
            )
        );
    }

    // WALIDACJA USTAWIEN -----------------------------------------
    private void OnValidate()
    {
        rozmiarObszaru.x = Mathf.Max(
            0.1f,
            rozmiarObszaru.x
        );

        rozmiarObszaru.y = Mathf.Max(
            0.1f,
            rozmiarObszaru.y
        );

        rozmiarObszaru.z = Mathf.Max(
            0.1f,
            rozmiarObszaru.z
        );

        maksymalnyZasieg = Mathf.Max(
            0.1f,
            maksymalnyZasieg
        );

        liczbaProbek = Mathf.Clamp(
            liczbaProbek,
            4,
            32
        );
    }

    // WLACZANIE I WYLACZANIE -----------------------------------------
    public void WlaczEfekt()
    {
        wlaczPromienie = true;
        AktualizujParametry();
    }

    public void WylaczEfekt()
    {
        wlaczPromienie = false;
        AktualizujParametry();
    }

    // STEROWANIE JAKOSCIA -----------------------------------------
    public void UstawLiczbeProbek(int nowaLiczba)
    {
        liczbaProbek = Mathf.Clamp(
            nowaLiczba,
            4,
            32
        );

        AktualizujParametry();
    }

    // WIZUALIZACJA OBSZARU W EDYTORZE -----------------------------------------
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(
            1f, 0.75f, 0.2f, 1f
        );

        // Obszar jest wyrownany do osi swiata.
        Gizmos.DrawWireCube(
            transform.position,
            rozmiarObszaru
        );
    }

    // WYLACZENIE KOMPONENTU -----------------------------------------
    private void OnDisable()
    {
        Shader.SetGlobalFloat(
            IdGestosci,
            0f
        );
    }
}
