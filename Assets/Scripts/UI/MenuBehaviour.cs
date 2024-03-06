using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;

public class MenuBehaviour : MonoBehaviour
{
    //height according to:
    //https://api.open-elevation.com/api/v1/lookup?locations=
    //
    private static GpsData termen = new GpsData("termen", 46.325912, 8.02196, 933);
    private static GpsData bettmerhorn = new GpsData("bettmerhorn", 46.4142, 8.0801, 2804.0);
    private static GpsData glishorn = new GpsData("glishorn", 46.2837, 7.9916, 2493.0);
    private static GpsData bielstudio = new GpsData("bielstudio", 47.140940, 7.246439, 440.0);
    private static GpsData meetingRoom = new GpsData("meetingRoom", 47.142433, 7.242970, 498.0);
    private static GpsData BFHNord = new GpsData("BFHNord", 47.14271, 7.24337, 498.0);

    private GpsData active;

    [Header("UI")]
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject inGameMenu;

    [SerializeField] private Toggle simulateGPS;
    [SerializeField] public TMP_InputField lat;
    [SerializeField] public TMP_InputField lon;

    [SerializeField] private Toggle unityTerrain;
    [SerializeField] private Toggle meshTerrain;

    private double meshSizeInMeters = 2000.0;

    private void Start()
    {
        menu.SetActive(true);
        inGameMenu.SetActive(false);

        unityTerrain.onValueChanged.AddListener(delegate
        {
            OnUnityTerrainToggle(unityTerrain);
        });

        meshTerrain.onValueChanged.AddListener(delegate
        {
            OnMeshTerrainToggle(meshTerrain);
        });
    }

    private static readonly Regex LatRegex = new Regex(@"^-?([1-8]?\d(\.\d+)?|90(\.0+)?)$", RegexOptions.Compiled);
    private static readonly Regex LonRegex = new Regex(@"^-?((1[0-7]\d|1[0-8]0|[1-9]?\d)(\.\d+)?|180(\.0+)?)$", RegexOptions.Compiled);

    private bool IsValidCoordinate(string coordinate, Regex pattern)
    {
        return pattern.IsMatch(coordinate);
    }

    public void OnLoad()
    {
        //bool isLatValid = IsValidCoordinate(lat.text, LatRegex);
        //bool isLonValid = IsValidCoordinate(lon.text, LonRegex);

        //if (!simulateGPS.isOn || (simulateGPS.isOn && isLatValid && isLonValid))
        {
            GPS.Instance.active = active;
            inGameMenu.SetActive(true);
            menu.SetActive(false);

            if (simulateGPS.isOn)
            {
                GPS.Instance.simulatedGpsLocation = new GpsData(Double.Parse(lat.text), Double.Parse(lon.text), 0.0d);
            }

            GPS.Instance.meshRangeInMeters = meshSizeInMeters;

            StartCoroutine(GPS.Instance.StartGPSAndCompassServiceAsync());
        }
    }



    #region UIElements

    public void OnSizeSlider(float value)
    {
        meshSizeInMeters = (double)value;
    }


    public void OnButtonClicked()
    {
        StartCoroutine(GPS.Instance.StartGPSAndCompassServiceAsync());
    }

    public void OnStudioClicked()
    {
        active = bielstudio;
        OnLoad();
    }

    public void OnMeetingRoomClicked()
    {
        active = meetingRoom;
        OnLoad();
    }

    public void OnTermenClicked()
    {
        active = termen;
        OnLoad();
    }

    public void OnGlishornClicked()
    {
        active = glishorn;
        OnLoad();
    }

    public void OnBettmerhornClicked()
    {
        active = bettmerhorn;
        OnLoad();
    }

    public void OnBFHNordClicked()
    {
        active = BFHNord;
        OnLoad();
    }

    public void OnMenuClicked()
    {
        menu.SetActive(true);
        inGameMenu.SetActive(false);
    }

    public void onSimulateCoordinatesClicked()
    {
        GPS.Instance.simulateGpsLocation = simulateGPS.isOn;
    }

    public void OnUnityTerrainToggle(Toggle toggle)
    {
        Debug.Log("Unity Terrain Toggle: " + toggle);
        GPS.Instance.unityTerrainParent.gameObject.SetActive(toggle.isOn);
        Debug.Log("Unity Terrain Active: " + GPS.Instance.unityTerrainParent.gameObject.activeSelf);
    }

    public void OnMeshTerrainToggle(Toggle toggle)
    {
        Debug.Log("Mesh Terrain Toggle: " + toggle);
        GPS.Instance.meshTerrainParent.gameObject.SetActive(toggle.isOn);
        Debug.Log("Mesh Terrain Active: " + GPS.Instance.meshTerrainParent.gameObject.activeSelf);
    }

    #endregion
}
