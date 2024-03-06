using System;
using UnityEngine;

[System.Serializable]
public class GpsData
{
    public string name;
    public double lat;
    public double lon;
    public double alt;

    public GpsData(string name, double latitude, double longitude, double altitude)
    {
        this.name = name;
        lat = latitude;
        lon = longitude;
        alt = altitude;
    }

    public GpsData(double latitude, double longitude, double altitude)
    {
        this.name = null;
        lat = latitude;
        lon = longitude;
        alt = altitude;
    }

    public GpsData()
    {
        this.name = null;
        lat = 0;
        lon = 0;
        alt = 0;
    }

    /// <summary>
    /// Converts a double from degrees to radians.
    /// </summary>
    public static double ToRadians(double degrees)
    {
        return (Math.PI / 180) * degrees;
    }

    /// <summary>
    /// Calculate the distance in meters between this GpsData and another GpsData
    /// </summary>
    public double DistanceTo(GpsData other)
    {
        var earthRadius = 6378137; // Radius of the earth in meters
        var dLat = ToRadians(other.lat - this.lat);
        var dLon = ToRadians(other.lon - this.lon);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(this.lat)) * Math.Cos(ToRadians(other.lon)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = earthRadius * c;

        return distance;
    }

    /// <summary>
    /// Calculate the bearing from this GpsData to another GpsData in degrees
    /// </summary>
    public double BearingTo(GpsData other)
    {
        var lat1 = ToRadians(this.lat);
        var lat2 = ToRadians(other.lat);
        var dLon = ToRadians(other.lon - this.lon);

        var y = Math.Sin(dLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) -
                Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        var bearingRadians = Math.Atan2(y, x);
        var bearingDegrees = (Math.Abs(bearingRadians * (180.0 / Math.PI)) + 360) % 360; // Normalize to 0-360

        return bearingDegrees;
    }

    public override string ToString()
    {
        return $"Name: {name}, Latitude: {lat}, Longitude: {lon}, Altitude: {alt}";
    }
}