using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// This class contains all utility, to convert coordinate systems into one another.
/// </summary>
public static class CoordinateConverter
{

    public const double EARTH_RADIUS_A = 6378137;
    public const double EARTH_ECCENTRICTIY = 8.1819190842622e-2;
    public static readonly double EARTH_RADIUS_A_SQR = EARTH_RADIUS_A * EARTH_RADIUS_A;
    public static readonly double EARTH_ECCENTRICTIY_SQR = EARTH_ECCENTRICTIY * EARTH_ECCENTRICTIY;
    public static readonly double EARTH_RADIUS_B = Math.Sqrt(EARTH_RADIUS_A_SQR * (1 - EARTH_ECCENTRICTIY_SQR));
    public static readonly double EARTH_RADIUS_B_SQR = EARTH_RADIUS_B * EARTH_RADIUS_B;
    public static readonly double EARTH_ECCENTRICTIY2 = Math.Sqrt((EARTH_RADIUS_A_SQR - EARTH_RADIUS_B_SQR) / EARTH_RADIUS_B_SQR);
    public static readonly double EARTH_ECCENTRICTIY2_SQR = EARTH_ECCENTRICTIY2 * EARTH_ECCENTRICTIY2;

    /// <summary>
    /// Converts WGS-84 coordinates into Earth Centered Earth Fixed coordinates.
    /// </summary>
    public static Vector3d WGS84ToECEF(GpsData latLonAlt)
    {
        double latitude = latLonAlt.lat * Mathf.Deg2Rad;
        double longitude = latLonAlt.lon * Mathf.Deg2Rad;
        double altitude = latLonAlt.alt;
        double a = EARTH_RADIUS_A;
        double esq = EARTH_ECCENTRICTIY_SQR;
        double cosLat = Math.Cos(latitude);

        double N = a / Math.Sqrt(1 - esq * Math.Pow(Math.Sin(latitude), 2));

        double x = (N + altitude) * cosLat * Math.Cos(longitude);
        double y = (N + altitude) * cosLat * Math.Sin(longitude);
        double z = ((1 - esq) * N + altitude) * Math.Sin(latitude);

        return new Vector3d(x, y, z);
    }

    /// <summary>
    /// Converts Earth Centered Earth Fixed coordinates into East North Up coordinates.
    /// </summary>
    public static Vector3d ECEFToENU(GpsData reference, Vector3d ecef)
    {
        double radLat = reference.lat * Mathf.Deg2Rad;
        double radLon = reference.lon * Mathf.Deg2Rad;

        Vector3d refECEF = WGS84ToECEF(reference);

        Vector3d deltaECEF = new Vector3d(ecef.x - refECEF.x, ecef.y - refECEF.y, ecef.z - refECEF.z);

        double east = -Mathf.Sin((float)radLon) * deltaECEF.x + Mathf.Cos((float)radLon) * deltaECEF.y;
        double north = -Mathf.Sin((float)radLat) * Mathf.Cos((float)radLon) * deltaECEF.x - Mathf.Sin((float)radLat) * Mathf.Sin((float)radLon) * deltaECEF.y + Mathf.Cos((float)radLat) * deltaECEF.z;
        double up = Mathf.Cos((float)radLat) * Mathf.Cos((float)radLon) * deltaECEF.x + Mathf.Cos((float)radLat) * Mathf.Sin((float)radLon) * deltaECEF.y + Mathf.Sin((float)radLat) * deltaECEF.z;

        return new Vector3d(east, north, up);
    }

    /// <summary>
    /// Converts a heading angle in degrees to a forward vector in 3D space.
    /// </summary>
    public static Vector3 HeadingToForwardVector(float headingDegrees)
    {
        float headingRadians = headingDegrees * Mathf.Deg2Rad; // Convert to radians
        float x = Mathf.Sin(headingRadians); // Calculate the x component
        float z = Mathf.Cos(headingRadians); // Calculate the z component
        return new Vector3(x, 0, z); // Construct and return the forward vector
    }

    /// <summary>
    /// Calculates the Relative Position of two GPSData (WGS-84) in 2d space (x, z). Using an approximative approach.
    /// </summary>
    public static Vector2 calculateRelativePositionEquirectangular2D(GpsData wgs84position, GpsData mywgs84position)
    {
        // Calculate the differences in coordinates
        double deltaLat = wgs84position.lat - mywgs84position.lat;
        double deltaLon = wgs84position.lon - mywgs84position.lon;

        // Convert latitude and longitude differences to radians
        double deltaLatRad = GpsData.ToRadians(deltaLat);
        double deltaLonRad = GpsData.ToRadians(deltaLon);

        // Calculate relative positions in meters
        double relativePosX = EARTH_RADIUS_A * deltaLonRad * Math.Cos(GpsData.ToRadians(mywgs84position.lat));
        double relativePosY = EARTH_RADIUS_A * deltaLatRad;

        return new Vector2((float)relativePosX, (float)relativePosY);
    }

    /// <summary>
    /// Saves a byte[] to a given filePath.
    /// </summary>
    private static void SaveRawData(string filePath, byte[] data)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
        {
            foreach (ushort value in data)
            {
                writer.Write(value);
            }
        }
    }
}

/// <summary>
/// A simple Vector3 class, but with doubles instead of floats.
/// </summary>
public class Vector3d
{
    public double x;
    public double y;
    public double z;

    public Vector3d(double _x, double _y, double _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }

    public double Distance(Vector3d a)
    {
        double dx = this.x - a.x;
        double dy = this.y - a.y;
        double dz = this.z - a.z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}

/// <summary>
/// Helper Class
/// </summary>
[System.Serializable]
public class ElevationResult
{
    public Location[] results;
}
/// <summary>
/// Helper Class
/// </summary>
[System.Serializable]
public class Location
{
    public float latitude;
    public float longitude;
    public float elevation;
}