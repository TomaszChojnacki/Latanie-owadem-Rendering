using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class AnimowanyPlomienLampy : MonoBehaviour
{
    [Header("Punkt mocowania")]
    [SerializeField]
    private Transform punktMocowania;

    [SerializeField]
    private Vector3 lokalnePrzesuniecie =
        new Vector3(0f, 0.02f, 0f);

    [SerializeField]
    private Vector2 rozmiarPlomienia =
        new Vector2(0.04f, 0.09f);

    [Header("Material plomienia")]
    [SerializeField]
    private Material materialPlomienia;

    [SerializeField]
    private bool utworzPlomienAutomatycznie = true;

    [Header("Animacja plomienia")]
    [SerializeField]
    private bool obrocDoKamery = true;

    [SerializeField]
    private float bazowaJasnoscPlomienia = 1.3f;

    [SerializeField]
    private float silaMigotaniaPlomienia = 0.18f;

    [SerializeField]
    private float predkoscMigotaniaPlomienia = 6f;

    [Header("Opcjonalne swiatlo lampy")]
    [SerializeField]
    private Light swiatloLampy;

    [SerializeField]
    private float bazowaIntensywnoscSwiatla = 1.8f;

    [SerializeField]
    private float silaMigotaniaSwiatla = 0.25f;

    [SerializeField]
    private float bazowyZasiegSwiatla = 2.4f;

    [SerializeField]
    private float silaMigotaniaZasiegu = 0.15f;

    private const string NazwaObiektuPlomienia =
        "PlomienLampy";

    private Transform obiektPlomienia;
    private Renderer rendererPlomienia;
    private MaterialPropertyBlock blokMaterialu;

    private float przesuniecieFazy;

    private static readonly int IdMnoznikaJasnosci =
        Shader.PropertyToID("_MnoznikJasnosci");

    private static readonly int IdPrzesunieciaFazy =
        Shader.PropertyToID("_PrzesuniecieFazy");

    private void OnEnable()
    {
        PrzygotujWszystko();
    }

    private void Start()
    {
        PrzygotujWszystko();
    }

    private void OnValidate()
    {
        rozmiarPlomienia.x = Mathf.Max(0.001f, rozmiarPlomienia.x);
        rozmiarPlomienia.y = Mathf.Max(0.001f, rozmiarPlomienia.y);

        bazowaJasnoscPlomienia = Mathf.Max(0f, bazowaJasnoscPlomienia);
        silaMigotaniaPlomienia = Mathf.Max(0f, silaMigotaniaPlomienia);
        predkoscMigotaniaPlomienia = Mathf.Max(0f, predkoscMigotaniaPlomienia);

        bazowaIntensywnoscSwiatla = Mathf.Max(0f, bazowaIntensywnoscSwiatla);
        silaMigotaniaSwiatla = Mathf.Max(0f, silaMigotaniaSwiatla);
        bazowyZasiegSwiatla = Mathf.Max(0f, bazowyZasiegSwiatla);
        silaMigotaniaZasiegu = Mathf.Max(0f, silaMigotaniaZasiegu);

        if (isActiveAndEnabled)
        {
            PrzygotujWszystko();
            AktualizujTransformPlomienia();
        }
    }

    private void LateUpdate()
    {
        if (materialPlomienia == null)
            return;

        if (obiektPlomienia == null || rendererPlomienia == null)
        {
            PrzygotujWszystko();
        }

        if (obiektPlomienia == null || rendererPlomienia == null)
            return;

        AktualizujTransformPlomienia();

        if (obrocDoKamery)
        {
            ObracajPlomienDoKamery();
        }

        AktualizujAnimacje();
    }

    [ContextMenu("Utworz lub odswiez plomien")]
    private void PrzygotujWszystko()
    {
        if (punktMocowania == null)
        {
            punktMocowania = transform;
        }

        if (blokMaterialu == null)
        {
            blokMaterialu = new MaterialPropertyBlock();
        }

        if (przesuniecieFazy == 0f)
        {
            przesuniecieFazy =
                Mathf.Repeat(Mathf.Abs(GetInstanceID()) * 0.173f, 100f);
        }

        ZnajdzLubUtworzPlomien();

        if (rendererPlomienia != null && materialPlomienia != null)
        {
            rendererPlomienia.sharedMaterial = materialPlomienia;
            rendererPlomienia.shadowCastingMode = ShadowCastingMode.Off;
            rendererPlomienia.receiveShadows = false;
            rendererPlomienia.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;
        }

        AktualizujTransformPlomienia();
    }

    private void ZnajdzLubUtworzPlomien()
    {
        if (punktMocowania == null)
            return;

        Transform znaleziony =
            punktMocowania.Find(NazwaObiektuPlomienia);

        if (znaleziony != null)
        {
            obiektPlomienia = znaleziony;
            rendererPlomienia = obiektPlomienia.GetComponent<Renderer>();
            return;
        }

        if (!utworzPlomienAutomatycznie)
            return;

        GameObject nowyPlomien =
            GameObject.CreatePrimitive(PrimitiveType.Quad);

        nowyPlomien.name = NazwaObiektuPlomienia;

        nowyPlomien.transform.SetParent(punktMocowania, false);

        Collider kolizja = nowyPlomien.GetComponent<Collider>();

        if (kolizja != null)
        {
            if (Application.isPlaying)
                Destroy(kolizja);
            else
                DestroyImmediate(kolizja);
        }

        obiektPlomienia = nowyPlomien.transform;
        rendererPlomienia = nowyPlomien.GetComponent<Renderer>();
    }

    private void AktualizujTransformPlomienia()
    {
        if (obiektPlomienia == null)
            return;

        obiektPlomienia.localPosition = lokalnePrzesuniecie;
        obiektPlomienia.localScale = new Vector3(
            rozmiarPlomienia.x,
            rozmiarPlomienia.y,
            1f
        );
    }

    private void ObracajPlomienDoKamery()
    {
        Camera kamera = Camera.main;

        if (kamera == null)
            return;

        obiektPlomienia.rotation = kamera.transform.rotation;
    }

    private void AktualizujAnimacje()
    {
        if (rendererPlomienia == null)
            return;

        float czas = Time.realtimeSinceStartup + przesuniecieFazy;

        float migotanieA =
            Mathf.Sin(czas * predkoscMigotaniaPlomienia);

        float migotanieB =
            Mathf.Sin(czas * predkoscMigotaniaPlomienia * 1.73f + 1.1f);

        float migotaniePlomienia =
            1f
            + migotanieA * silaMigotaniaPlomienia
            + migotanieB * silaMigotaniaPlomienia * 0.5f;

        float skalaX =
            rozmiarPlomienia.x * (1f + migotaniePlomienia * 0.04f);

        float skalaY =
            rozmiarPlomienia.y * (1f + migotaniePlomienia * 0.08f);

        obiektPlomienia.localScale = new Vector3(
            Mathf.Max(0.001f, skalaX),
            Mathf.Max(0.001f, skalaY),
            1f
        );

        rendererPlomienia.GetPropertyBlock(blokMaterialu);

        blokMaterialu.SetFloat(
            IdMnoznikaJasnosci,
            bazowaJasnoscPlomienia * Mathf.Max(0.1f, migotaniePlomienia)
        );

        blokMaterialu.SetFloat(
            IdPrzesunieciaFazy,
            przesuniecieFazy
        );

        rendererPlomienia.SetPropertyBlock(blokMaterialu);

        if (swiatloLampy != null)
        {
            float migotanieSwiatla =
                1f
                + migotanieA * silaMigotaniaSwiatla
                + migotanieB * silaMigotaniaSwiatla * 0.5f;

            float migotanieZasiegu =
                1f
                + migotanieA * silaMigotaniaZasiegu
                + migotanieB * silaMigotaniaZasiegu * 0.5f;

            swiatloLampy.intensity =
                Mathf.Max(0f, bazowaIntensywnoscSwiatla * migotanieSwiatla);

            swiatloLampy.range =
                Mathf.Max(0.01f, bazowyZasiegSwiatla * migotanieZasiegu);
        }
    }
}