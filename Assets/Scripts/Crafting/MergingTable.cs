using UnityEngine;

public class MergingTable : MonoBehaviour
{
    [Header("Miejsca na stole")]
    public Transform ingotPreview; 
    public Transform handlePreview; 

    [Header("Kamery i Sterowanie")]
    public GameObject assemblyCamera; 
    public MonoBehaviour[] scriptsToDisable;

    [Header("Crafting - Łączenie")]
    public Transform craftSpawnPoint; 
    public GameObject craftingUI; 

    [Header("Grip - Pozycja w dłoni (jeśli przywrócisz ludków)")]
    public Vector3 gripPositionOffset = Vector3.zero;
    public Vector3 gripRotation = new Vector3(0f, -90f, -30f);

    private GameObject mainPlayerCamera;
    private bool isAssemblyMode = false;

    private MetalPiece placedMetal;
    private WoodPiece placedWood;

    private Transform draggedObject;
    private float dragY;
    private Vector3 dragOffset;
    public float snapThreshold = 0.5f;

    private System.Collections.Generic.Dictionary<string, int> pendingDeductions = new System.Collections.Generic.Dictionary<string, int>();

    void Start()
    {
        if (craftingUI != null) craftingUI.SetActive(false);
    }

    void Update()
    {
        if (!isAssemblyMode) return;

        // Mechanika przeciągania na myszce i łączenia
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = assemblyCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                MetalPiece m = hit.collider.GetComponentInParent<MetalPiece>();
                WoodPiece w = hit.collider.GetComponentInParent<WoodPiece>();

                if (m != null) draggedObject = m.transform;
                else if (w != null) draggedObject = w.transform;

                if (draggedObject != null)
                {
                    // Chwytamy dokładnie na wysokości osi Y na której leżał, by obiekty się nie krzywiły w pionie
                    dragY = draggedObject.position.y; 
                    
                    Plane dragPlane = new Plane(Vector3.up, new Vector3(0, dragY, 0));
                    if (dragPlane.Raycast(ray, out float distance))
                    {
                        dragOffset = draggedObject.position - ray.GetPoint(distance);
                    }

                    Rigidbody rb = draggedObject.GetComponent<Rigidbody>();
                    if (rb != null) 
                    {
                        rb.isKinematic = true; 
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }
        }
        else if (Input.GetMouseButton(0) && draggedObject != null)
        {
            // Płynne ciągnięcie nad stołem w osi XZ
            Ray ray = assemblyCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            Plane dragPlane = new Plane(Vector3.up, new Vector3(0, dragY, 0));
            if (dragPlane.Raycast(ray, out float distance))
            {
                Vector3 targetPos = ray.GetPoint(distance) + dragOffset;
                draggedObject.position = new Vector3(targetPos.x, dragY, targetPos.z);
            }

            // Obracanie przy pomocy scrolla myszy (obrót na stole wokół osi Y)
            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0)
            {
                draggedObject.Rotate(Vector3.up, scroll * 15f, Space.World);
            }
        }
        else if (Input.GetMouseButtonUp(0) && draggedObject != null)
        {
            // Upuszczenie elementu łagodnie
            Rigidbody rb = draggedObject.GetComponent<Rigidbody>();
            if (rb != null) 
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            CheckCombination(draggedObject);
            draggedObject = null;
        }
    }

    public void ToggleAssemblyCamera(GameObject playerCam)
    {
        if (isAssemblyMode) return;
        mainPlayerCamera = playerCam; 
        mainPlayerCamera.SetActive(false);
        assemblyCamera.SetActive(true);
        isAssemblyMode = true;

        if (craftingUI != null) craftingUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitAssemblyMode()
    {
        if (!isAssemblyMode) return;

        // Jeśli wyszliśmy, puść obiekt
        if (draggedObject != null)
        {
            Rigidbody rb = draggedObject.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
            draggedObject = null;
        }

        mainPlayerCamera.SetActive(true);
        assemblyCamera.SetActive(false);
        isAssemblyMode = false;

        // --- USUWANIE Z EQ ---
        if (pendingDeductions.Count > 0 && gameManager.Instance != null)
        {
            foreach (var kvp in pendingDeductions)
            {
                gameManager.Instance.RemoveResource(kvp.Key, kvp.Value);
            }
            pendingDeductions.Clear();
            Debug.Log("[MergingTable] Zaktualizowano EQ gracza (usunięto zużyte materiały) po wyjściu ze stołu.");
        }
        // ---------------------

        if (craftingUI != null) craftingUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void DropItemOntoTable(Transform item, Vector3 hitPoint)
    {
        // Puszczamy nową sztabkę z dużym marginesem przestrzeni
        item.SetParent(null); 
        item.position = hitPoint + Vector3.up * 0.15f;
        
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb == null) rb = item.gameObject.AddComponent<Rigidbody>();

        if (rb != null) 
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void CheckCombination(Transform droppedObj)
    {
        Collider[] myCols = droppedObj.GetComponentsInChildren<Collider>();
        if (myCols.Length == 0) return;

        System.Collections.Generic.List<Transform> partsToCombine = new System.Collections.Generic.List<Transform>();
        partsToCombine.Add(droppedObj);

        // Pobieramy wszystko w promieniu obszaru roboczego
        Collider[] candidates = Physics.OverlapSphere(droppedObj.position, 1.5f);
        
        foreach (Collider candCol in candidates)
        {
            // Pomijamy własne wnętrze sklejki
            if (candCol.transform == droppedObj || candCol.transform.IsChildOf(droppedObj)) continue;

            // Upewniamy się, czy fizyczne siatki REALNIE na siebie wpadają (lub leżą tak blisko, że to łączenie)
            bool isTouching = false;
            foreach (Collider myCol in myCols)
            {
                if (myCol.bounds.Intersects(candCol.bounds))
                {
                    // Liczy precyzyjniej niż sześcian otaczający (Bounding Box), szuka punktów wspólnych mniejszych niż 5cm!
                    Vector3 closestDrop = myCol.ClosestPoint(candCol.bounds.center);
                    Vector3 closestCand = candCol.ClosestPoint(closestDrop);
                    if (Vector3.Distance(closestDrop, closestCand) < 0.05f)
                    {
                        isTouching = true;
                        break;
                    }
                }
            }

            if (isTouching)
            {
                Transform rootPart = null;
                if (candCol.GetComponentInParent<MetalPiece>()) rootPart = candCol.GetComponentInParent<MetalPiece>().transform;
                else if (candCol.GetComponentInParent<WoodPiece>()) rootPart = candCol.GetComponentInParent<WoodPiece>().transform;
                else if (candCol.GetComponentInParent<FinishedObject>()) rootPart = candCol.GetComponentInParent<FinishedObject>().transform;

                // Dodaj tylko, jeśli fizycznie coś dotknęliśmy obok i jeszcze nie jest na liście w fazie montażu
                if (rootPart != null && !partsToCombine.Contains(rootPart))
                {
                    partsToCombine.Add(rootPart);
                }
            }
        }

        // Fuzja dokonuje się wyłącznie wtedy gdy obiekty mocno naruszą swoją osobistą przestrzeń
        if (partsToCombine.Count > 1)
        {
            CustomCombine(partsToCombine);
        }
    }

    private void CustomCombine(System.Collections.Generic.List<Transform> parts)
    {
        string weaponName = "Sklejona_Wariacja_" + Random.Range(100, 999);
        GameObject craftedWeapon = new GameObject(weaponName);
        craftedWeapon.transform.position = parts[0].position; // Środkiem nowej broni będzie upuszczony bloczek

        // Zapisujemy rotację bazową (rączki), by móc potem wyzerować model do defaultu
        Quaternion baseRot = Quaternion.Euler(-90, 0, 90f);
        foreach (Transform p in parts)
        {
            if (p.GetComponent<WoodPiece>() != null || p.GetComponent<FinishedObject>() != null)
            {
                baseRot = p.rotation;
                break;
            }
        }
        craftedWeapon.transform.rotation = baseRot;

        Vector3 gripLocalPos = Vector3.zero;

        foreach (Transform part in parts)
        {
            part.SetParent(craftedWeapon.transform, true); 

            // Kasujemy ich niezależne siły dociążające
            Rigidbody rb = part.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            
            FinishedObject oldPart = part.GetComponent<FinishedObject>();
            if (oldPart != null) Destroy(oldPart);

            // Odpinamy kowalskie cechy by stały się martwym komponentem broni
            MetalPiece metal = part.GetComponent<MetalPiece>();
            if (metal != null) 
            {
                string key = metal.metalTier.ToString();
                if (pendingDeductions.ContainsKey(key)) pendingDeductions[key]++;
                else pendingDeductions[key] = 1;

                metal.ForceCoolDown();
                Destroy(metal);
            }

            WoodPiece wood = part.GetComponent<WoodPiece>();
            if (wood != null)
            {
                string key = string.IsNullOrEmpty(wood.partType) ? "SwordHandle" : wood.partType;
                if (part.name.Contains("Siekier") || part.name.Contains("Axe")) key = "AxeHandle";

                if (pendingDeductions.ContainsKey(key)) pendingDeductions[key]++;
                else pendingDeductions[key] = 1;

                // Powrót do pierwotnej niezawodnej metody - bierzemy punkt Pivotu drewna (najlepsze do trzymania)
                gripLocalPos = part.localPosition;
                Destroy(wood);
            }
            else
            {
                // Ważne: Jeśli dokładamy materiał do JUŻ sklejonej broni z poprzedniej akcji,
                // jej skrypt WoodPiece został nadpisany. Posiada ona własny stary "GripPoint".
                // Musimy go ukraść, przeliczyć na nowy układ osi i przepisać wyżej!
                Transform oldGrip = part.Find("GripPoint");
                if (oldGrip != null)
                {
                    gripLocalPos = craftedWeapon.transform.InverseTransformPoint(oldGrip.position);
                    Destroy(oldGrip.gameObject); // Niszczymy starego kandydata rączki (i tak powstanie nowy)
                }
            }

            FinishedObject oldWeapon = part.GetComponent<FinishedObject>();
            if (oldWeapon != null)
            {
                // Jeśli dołączamy kawałek metalu do już sklejonej wcześniej broni: usuwamy stary system trzymania
                Destroy(oldWeapon);
                foreach (Transform child in part)
                {
                    if (child.name == "GripPoint") Destroy(child.gameObject);
                }
            }
        }

        // UWAGA: Usunięto tutaj zerowanie rotacji! Dzięki temu po sklejeniu 
        // obiekt nie 'skacze' na stole, tylko zostaje w 100% z taką rotacją, 
        // jaką gracz ułożył sobie przed dołączeniem!

        // Cała wariacja otrzymuje JEDEN wspólny silnik fizyczny
        Rigidbody weaponRb = craftedWeapon.AddComponent<Rigidbody>();
        weaponRb.mass = 1.5f * parts.Count; // Waży tym więcej, im więcej syfu nakleisz
        weaponRb.interpolation = RigidbodyInterpolation.Interpolate;

        // UWAGA: Nie dodajemy tu sztucznego BoxCollidera! Fizyka Unity sama zbierze MeshCollidery od każdego dziecka
        // i spakuje je w jedno perfekcyjnie zniekształcone ciało, idealnie uderzające np. koślawą stroną w stół.

        FinishedObject finishedObj = craftedWeapon.AddComponent<FinishedObject>();

        // Punkt złapania w powietrzu do ręki
        GameObject grip = new GameObject("GripPoint");
        grip.transform.SetParent(craftedWeapon.transform);
        grip.transform.localPosition = gripLocalPos + gripPositionOffset;
        grip.transform.localRotation = Quaternion.Euler(gripRotation);

        Debug.Log($"[Drag&Drop Sandbox] Posklejano idealnie {parts.Count} swobodnie rzuconych części w 1 warianką kombinację!");
    }
}