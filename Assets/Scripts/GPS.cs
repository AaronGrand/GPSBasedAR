using System;
using System.Collections;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;

/// <summary>
/// The Main Class of the Tool.
/// This Class sets all the variables for the terrain and AR-options.
/// </summary>
[RequireComponent(typeof(XROrigin))]
public class GPS : MonoBehaviour
{
    public static GPS Instance { set; get; }

    #region Class Variables

    private GpsData currentGpsLocation;

    [SerializeField] public GpsData active;

    [Header("AR Settings")]
    private XROrigin xrOrigin;
    [SerializeField] private GameObject cameraOffset;
    [SerializeField] private Transform origin;
    [SerializeField] private Transform Camera;
    [SerializeField] private float cameraHeightOffset;
    [SerializeField] private Transform glacier;

    [Header("Terrain")]
    [SerializeField] private HeightModels heightModel;
    [SerializeField] private string openTopographyAPIKey;
    [SerializeField] private TerrainDataLoader terrainDataLoader;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] public double meshRangeInMeters = 11790;

    [Header("Mesh Generation")]
    [SerializeField] public Transform meshTerrainParent;
    [SerializeField] private GameObject terrainPrefab;

    [Header("Terrain Generation")]
    [SerializeField] public Transform unityTerrainParent;
    [SerializeField] private Terrain terrain;
    [SerializeField] private TerrainCollider terrainCollider;

    private MeshFilter meshFilter;


    [Header("Debug")]
    [SerializeField] private bool debugVertices;
    [SerializeField] private GameObject vertexPrefab;
    [SerializeField] private bool gpsAltitudeDebugging;

    [SerializeField] public bool simulateGpsLocation;
    [SerializeField] public GpsData simulatedGpsLocation;

    [SerializeField] private bool geotiff = false;


    [SerializeField] private GameObject objectToResize;


    [NonSerialized]public bool started = false;

    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        xrOrigin = origin.GetComponent<XROrigin>();


    }

    private void Update()
    {
        if (started)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Moved)
                {
                    float rotationAmount = touch.deltaPosition.x * -0.1f;
                    cameraOffset.transform.Rotate(0, rotationAmount, 0);
                }
            }
        }
    }

    #endregion

    #region Class Methods
    /// <summary>
    /// Main Programm. Starts the Compass and the GPS. Gets the TerrainData from TerrainDataLoader and converts it into a Terrain, and places it accordingly.
    /// </summary>
    public IEnumerator StartGPSAndCompassServiceAsync()
    {
        //GPS READING
        {
            // Check if the user has location service enabled.
            if (!Input.location.isEnabledByUser)
                Debug.Log("Location not enabled on device or app does not have permission to access location");

            // Starts the location service.
            Input.location.Start();

            // Waits until the location service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // If the service didn't initialize in 20 seconds this cancels location service use.
            if (maxWait < 1)
            {
                Debug.Log("Timed out");
                yield break;
            }

            // If the connection failed this cancels location service use.
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("Unable to determine device location");
                yield break;
            }
            else
            {
                currentGpsLocation = new GpsData(Input.location.lastData.latitude, Input.location.lastData.longitude, Input.location.lastData.altitude);

                if (simulateGpsLocation)
                {
                    currentGpsLocation = simulatedGpsLocation;
                    Debug.Log("Simulated gps location: " + currentGpsLocation.lat + " " + currentGpsLocation.lon + " " + currentGpsLocation.alt);
                } else
                {
                    Debug.Log("Current gps location: " + currentGpsLocation.lat + " " + currentGpsLocation.lon + " " + currentGpsLocation.alt);
                }
            }
        }

        // Load Ascii Grid from api
        GetTerrainData((result) =>
        {

            // Terrain Creation
            {
                terrain.terrainData = TerrainDataLoader.CreateTerrainDataFromAsciiGrid(TerrainDataLoader.GetHeightsFromAsciiGrid(result, heightModel));
                
                // Set "resolution"
                terrain.heightmapPixelError = 20;

                terrain.materialTemplate = terrainMaterial;

                // assign the data to the collider
                terrainCollider.terrainData = terrain.terrainData;

                float terrainSizeInMeters = terrain.terrainData.size.x;

                // center the terrain
                terrain.transform.position = new Vector3(
                    -terrainSizeInMeters / 2,
                    terrain.transform.position.y,
                    -terrainSizeInMeters / 2);

                // set height
                Vector3 terrainHeight = CalculatePositionOnTerrain(origin, cameraHeightOffset);
                unityTerrainParent.position = new Vector3(unityTerrainParent.position.x, -terrainHeight.y, unityTerrainParent.position.z);
            }
            
            // Mesh Creation
            {
                foreach (Transform child in meshTerrainParent.transform)
                {
                    Destroy(child.gameObject);
                }

                Mesh[] terrainPieces = TerrainDataLoader.CreateMultipleMeshesFromAsciiGrid(TerrainDataLoader.GetHeightsFromAsciiGrid(result, heightModel), currentGpsLocation);

                for (int i = 0; i < terrainPieces.Length; i++)
                {
                    GameObject terrainPiece = Instantiate(terrainPrefab, meshTerrainParent, true);
                    terrainPiece.name = "TerrainPiece_" + i;
                    terrainPiece.transform.localPosition = new Vector3(0f, meshTerrainParent.transform.position.y, 0f);
                    terrainPiece.transform.localScale = Vector3.one;

                    // Add MeshFilter component
                    MeshFilter meshFilter = terrainPiece.AddComponent<MeshFilter>();
                    meshFilter.mesh = terrainPieces[i];

                    // Add MeshRenderer component
                    MeshRenderer meshRenderer = terrainPiece.AddComponent<MeshRenderer>();
                    meshRenderer.material = terrainMaterial;

                    // Add MeshCollider component
                    MeshCollider meshCollider = terrainPiece.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = terrainPieces[i];
                }

                
                // set height
                float playerHeightOffset = -(CalculatePositionOnMesh(origin).y + cameraHeightOffset);

                meshTerrainParent.position = new Vector3(meshTerrainParent.position.x, meshTerrainParent.position.y + playerHeightOffset, meshTerrainParent.position.z);                
            }

            //EQUIRECTANGULAR
            Vector2 glacierUnityLocation = CoordinateConverter.calculateRelativePositionEquirectangular2D(active, currentGpsLocation);
            glacier.position = new Vector3((float)(glacierUnityLocation.x), glacier.position.y, glacierUnityLocation.y);

            glacier.position = CalculatePositionOnTerrain(glacier);
            glacier.position = new Vector3(glacier.position.x, glacier.position.y + unityTerrainParent.position.y, glacier.position.z);


            //Debug
            if (debugVertices)
            {
                Mesh mesh = meshFilter.mesh;
                Vector3[] vertices = mesh.vertices;

                foreach (Vector3 vertex in vertices)
                {
                    // Convert local vertex position to world space
                    Vector3 worldVertexPosition = transform.TransformPoint(vertex);
                    Instantiate(vertexPrefab, worldVertexPosition, Quaternion.identity, transform);
                }
            }
        });


        // Enable the compass
        Input.compass.enabled = true;

        // Wait a bit for the compass to start
        yield return new WaitForSeconds(1f);

        float heading = Input.compass.magneticHeading;

        //rotate Origin
        xrOrigin.MatchOriginUpCameraForward(Vector3.up, CoordinateConverter.HeadingToForwardVector(heading));

        started = true;
    }

    /// <summary>
    /// Calculates the Position on a Mesh (with MeshCollider).
    /// </summary>
    public Vector3 CalculatePositionOnMesh(Transform t, float heightOffset = 0f)
    {
        Vector3 pos = new Vector3(t.position.x, t.position.y + 50000f, t.position.z);

        Debug.Log("Ray: " + t.name.ToString() + " " + pos.ToString());

        int layer = terrainLayer;
        RaycastHit ray;
        if (Physics.Raycast(pos, Vector3.down, out ray, 100000, layer))
        {
            Debug.DrawRay(pos, t.TransformDirection(Vector3.down) * ray.distance, Color.yellow, 100000f);
        }
        else
        {
            Physics.Raycast(pos, Vector3.up, out ray, 100000, layer);
            Debug.DrawRay(pos, t.TransformDirection(Vector3.up) * ray.distance, Color.yellow, 100000f);
        }

        Debug.Log("RayDistance: " + t.name.ToString() + " " + ray.distance);

        Vector3 temp = ray.point;
        return new Vector3(temp.x, temp.y + heightOffset, temp.z);
    }

    /// <summary>
    /// Calculates the Position on the Terrain.
    /// </summary>
    public Vector3 CalculatePositionOnTerrain(Transform t, float heightOffset = 0f)
    {
        float terrainHeight = terrain.SampleHeight(t.position);

        Vector3 positionOnTerrain = new Vector3(t.position.x, terrainHeight + heightOffset, t.position.z);

        Debug.Log("Terrain Position: " + t.name + " " + positionOnTerrain);

        return positionOnTerrain;
    }

    #endregion

    #region ASYNC Methods
    /// <summary>
    /// Gets the TerrainData from TerrainDataLoader.
    /// </summary>
    private async void GetTerrainData(Action<string[]> callback)
    {
        double range = meshRangeInMeters / 2;


        double latDegreeDistance = range / 111000.0; // Convert range to latitude degrees
        double lonDegreeDistance = range / (Math.Cos(currentGpsLocation.lat * Math.PI / 180) * 111000.0); // Convert range to longitude degrees

        string data = await TerrainDataLoader.GetTerrainData(
            currentGpsLocation.lat + latDegreeDistance,
            currentGpsLocation.lat - latDegreeDistance,
            currentGpsLocation.lon - lonDegreeDistance,
            currentGpsLocation.lon + lonDegreeDistance,
            openTopographyAPIKey, heightModel);

        if (geotiff)
        {
            string path = await TerrainDataLoader.GetTerrainDataGeoTiff(
                currentGpsLocation.lat + latDegreeDistance,
                currentGpsLocation.lat - latDegreeDistance,
                currentGpsLocation.lon - lonDegreeDistance,
                currentGpsLocation.lon + lonDegreeDistance,
                openTopographyAPIKey, heightModel);
        }


        string[] fileArray = data.Split(new char[] { '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        Debug.Log("TerrainData from API: " + data);
        callback(fileArray);
    }

    #endregion
}