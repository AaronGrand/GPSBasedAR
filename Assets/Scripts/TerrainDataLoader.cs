using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Loads Terrain Data from OpenTopography.org.
/// </summary>
public class TerrainDataLoader
{

    public static int MAX_VERTICES_PER_MESH = 65535;
    public static int EarthRadius = 6371000;

    private void Start()
    {
        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
    }

    /// <summary>
    /// Creates a Mesh[] from AsciiHeightData and the centered GPS-Data.
    /// </summary>
    public static Mesh[] CreateMultipleMeshesFromAsciiGrid(AsciiHeightData asciiHeightData, GpsData currentGpsLocation)
    {
        //fixing aspect ratio
        int desiredAspectRatio = 1;
        double currentAspectRatio = (double)asciiHeightData.ncols / asciiHeightData.nrows;
        double scalingFactor = desiredAspectRatio / currentAspectRatio;

        double rowOffsetToMiddle = asciiHeightData.nrows * asciiHeightData.gridSizeInMeter / 2;
        double colOffsetToMiddle = asciiHeightData.ncols * scalingFactor * asciiHeightData.gridSizeInMeter / 2;

        Debug.Log("Mesh data parsed");

        // Calculating full pieces and the remainder for both rows and columns
        int numRowsOrColsPerPiece = Mathf.FloorToInt(Mathf.Sqrt(MAX_VERTICES_PER_MESH)) - 1;
        int numFullPiecesRows = asciiHeightData.nrows / numRowsOrColsPerPiece;
        int remainderRows = asciiHeightData.nrows % numRowsOrColsPerPiece;
        int numFullPiecesCols = asciiHeightData.ncols / numRowsOrColsPerPiece;
        int remainderCols = asciiHeightData.ncols % numRowsOrColsPerPiece;

        List<Mesh> meshPieces = new List<Mesh>();

        // Create each sub-mesh piece
        for (int pieceRow = 0; pieceRow <= numFullPiecesRows; pieceRow++)
        {
            for (int pieceCol = 0; pieceCol <= numFullPiecesCols; pieceCol++)
            {
                // Calculate the starting indices and size of the sub-mesh
                int rowStart = pieceRow * numRowsOrColsPerPiece;
                int colStart = pieceCol * numRowsOrColsPerPiece;

                int rowsToProcess = (pieceRow < numFullPiecesRows) ? numRowsOrColsPerPiece + 1: remainderRows;
                int colsToProcess = (pieceCol < numFullPiecesCols) ? numRowsOrColsPerPiece + 1: remainderCols;

                // Adjust for the edge pieces to include the last row/column of the previous piece
                if (pieceRow > 0 && rowsToProcess < numRowsOrColsPerPiece) rowStart -= 1;
                if (pieceCol > 0 && colsToProcess < numRowsOrColsPerPiece) colStart -= 1;

                // Adjust the number of rows and columns to process
                // for the edge pieces to ensure they include the shared edge.
                if (pieceRow > 0 && rowsToProcess < numRowsOrColsPerPiece) rowsToProcess += 1;
                if (pieceCol > 0 && colsToProcess < numRowsOrColsPerPiece) colsToProcess += 1;

                List<Vector3> subMeshVertices = new List<Vector3>();
                List<Vector2> subMeshUV = new List<Vector2>();
                List<int> subMeshTriangles = new List<int>();


                // Create vertices, uv, and triangles for the sub-mesh piece
                for (int row = rowStart; row < rowStart + rowsToProcess; row++)
                {
                    for (int col = colStart; col < colStart + colsToProcess; col++)
                    {
                        float posX = (float)(col * scalingFactor * asciiHeightData.gridSizeInMeter - colOffsetToMiddle);
                        float posZ = (float)(row * asciiHeightData.gridSizeInMeter - rowOffsetToMiddle);

                        // Vertex position based on grid size and offsets
                        subMeshVertices.Add(new Vector3(posX, asciiHeightData.heights[row, col], posZ));


                        subMeshUV.Add(new Vector2((float)col / (asciiHeightData.ncols - 1), (float)row / (asciiHeightData.nrows - 1)));

                        // Add triangles only if not on the edge of a sub-mesh
                        if (row < rowStart + rowsToProcess - 1 && col < colStart + colsToProcess - 1)
                        {
                            int current = (row - rowStart) * colsToProcess + (col - colStart);
                            int down = current + colsToProcess;
                            int right = current + 1;
                            int downRight = down + 1;

                            subMeshTriangles.AddRange(new[] { current, down, right, right, down, downRight });
                        }
                    }
                }

                // Create the sub-mesh
                Mesh subMesh = new Mesh();
                subMesh.vertices = subMeshVertices.ToArray();
                subMesh.uv = subMeshUV.ToArray();
                subMesh.triangles = subMeshTriangles.ToArray();
                subMesh.RecalculateNormals();
                meshPieces.Add(subMesh);
            }
        }

        return meshPieces.ToArray();
    }



    private const string BASE_URL = "https://portal.opentopography.org/API/globaldem";
    /// <summary>
    /// Gets a GeoTiff file in string format from GPS-boundaries from OpenTopography.org API.
    /// </summary>
    public static async Task<string> GetTerrainData(double gpsNorth, double gpsSouth, double gpsWest, double gpsEast, string apiKey, HeightModels heightModel)
    {
        string heightM = HeightModelUtility.GetAPIReference(heightModel);

        string fullUrl = $"{BASE_URL}?demtype=" + heightM + "&south=" + gpsSouth + "&north=" + gpsNorth + "&west=" + gpsWest + "&east=" + gpsEast + "&outputFormat=AAIGrid&API_Key=" + apiKey;
        Debug.Log(fullUrl);

        using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
        {
            UnityWebRequestAsyncOperation asyncOp = www.SendWebRequest();

            while (!asyncOp.isDone)
            {
                await Task.Delay(50); // Wait for a short duration before checking again
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error: " + www.error);
                return null;
            }
            else
            {
                return www.downloadHandler.text;
            }
        }
    }

    /// <summary>
    /// Gets a GeoTiff file from GPS-boundaries from OpenTopography.org API.
    /// </summary>
    public static async Task<string> GetTerrainDataGeoTiff(double gpsNorth, double gpsSouth, double gpsWest, double gpsEast, string apiKey, HeightModels heightModel)
    {
        string heightM = HeightModelUtility.GetAPIReference(heightModel);

        string fullUrl = $"{BASE_URL}?demtype=" + heightM + "&south=" + gpsSouth + "&north=" + gpsNorth + "&west=" + gpsWest + "&east=" + gpsEast + "&outputFormat=GTiff&API_Key=" + apiKey;
        Debug.Log(fullUrl);

        using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
        {
            UnityWebRequestAsyncOperation asyncOp = www.SendWebRequest();

            while (!asyncOp.isDone)
            {
                await Task.Delay(50); // Wait for a short duration before checking again
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error: " + www.error);
                return null;
            }
            else
            {
                // Handle the binary data for GeoTIFF
                byte[] data = www.downloadHandler.data;
                string filePath = Path.Combine(Application.dataPath, "DownloadedTerrain.tif");

                // Save the file in the Assets folder
                File.WriteAllBytes(filePath, data);
                Debug.Log($"File saved to {filePath}");

                return filePath;
            }
        }
    }

    /// <summary>
    /// Creates AsciiHeightData from a given AsciiGrid[] and a heightModel.
    /// </summary>
    public static AsciiHeightData GetHeightsFromAsciiGrid(string[] asciiLines, HeightModels heightModel)
    {
        int gridSizeInMeter = HeightModelUtility.GetGridSize(heightModel);

        // Parse metadata
        int ncols = int.Parse(asciiLines[1]);
        int nrows = int.Parse(asciiLines[3]);

        double xllcorner = double.Parse(asciiLines[5]);
        double yllcorner = double.Parse(asciiLines[7]);

        double cellsize = double.Parse(asciiLines[9]);
        double nodata_value = double.Parse(asciiLines[11]);

        // Extracting the height data
        float[,] heights = new float[nrows, ncols];
        for (int row = 0; row < nrows; row++)
        {
            for (int col = 0; col < ncols; col++)
            {
                heights[row, col] = float.Parse(asciiLines[12 + row * ncols + col]);
            }
        }
        
        //fixing aspect ratio
        int desiredAspectRatio = 1;
        double currentAspectRatio = (double)ncols / nrows;
        double scalingFactor = desiredAspectRatio / currentAspectRatio;

        double rowOffsetToMiddle = nrows * gridSizeInMeter / 2;
        double colOffsetToMiddle = ncols * scalingFactor * gridSizeInMeter / 2;
        
        AsciiHeightData data;

        data.heights = heights;
        data.colScalingFactor = scalingFactor;
        data.gridSizeInMeter = gridSizeInMeter;

        data.nrows = nrows;
        data.ncols = ncols;

        return data;
    }

    /// <summary>
    /// Creates TerrainData from a given AsciiHeightData.
    /// </summary>
    public static TerrainData CreateTerrainDataFromAsciiGrid(AsciiHeightData data)
    {

        TerrainData terrainData = new TerrainData();
        // max Heightmap Resolution
        terrainData.heightmapResolution = 4097;

        // resample the heightmap, so it stretches the map to the Resolution
        float[,] heights = ResampleHeightmap(data.heights, terrainData.heightmapResolution);

        int originalRows = heights.GetLength(0); // Number of rows in the resampled heightmap
        int originalCols = heights.GetLength(1); // Number of columns in the resampled heightmap

        int mRow = data.nrows * data.gridSizeInMeter;

        //find max height
        int maxHeight = 0;
        int minHeight = (int)heights[0, 0];
        for (int i = 0; i < originalRows; i++)
        {
            for (int j = 0; j < originalCols; j++)
            {
                if (heights[i, j] > maxHeight)
                {
                    maxHeight = (int)heights[i, j];
                }

                if (heights[i, j] < minHeight)
                {
                    minHeight = (int)heights[i, j];
                }
            }
        }

        //set terrain size
        terrainData.size = new Vector3(mRow, (maxHeight - minHeight), mRow);

        // Adjusting dimensions for rotated matrix
        int newRows = originalCols;
        int newCols = originalRows;
        float[,] fHeights = new float[newRows, newCols];

        for (int i = 0; i < originalRows; i++)
        {
            for (int j = 0; j < originalCols; j++)
            {
                // Rotating 90 degrees counter-clockwise
                fHeights[newRows - 1 - j, i] = (heights[i, j] - minHeight) / (maxHeight - minHeight) + (minHeight / (maxHeight - minHeight));
            }
        }

        terrainData.SetHeights(0, 0, fHeights);

        return terrainData;
    }

    /// <summary>
    /// Resamples a two-dimensional height-array to a new resolution using bilinear interpolation.
    /// </summary>
    private static float[,] ResampleHeightmap(float[,] originalHeights, int newResolution)
    {
        int originalWidth = originalHeights.GetLength(1);
        int originalHeight = originalHeights.GetLength(0);
        float[,] resampledHeights = new float[newResolution, newResolution];

        for (int i = 0; i < newResolution; i++)
        {
            for (int j = 0; j < newResolution; j++)
            {
                float xRatio = i / (float)(newResolution - 1);
                float yRatio = j / (float)(newResolution - 1);

                float x = xRatio * (originalWidth - 1);
                float y = yRatio * (originalHeight - 1);

                int xFloor = (int)x;
                int yFloor = (int)y;
                int xCeil = xFloor == originalWidth - 1 ? xFloor : xFloor + 1;
                int yCeil = yFloor == originalHeight - 1 ? yFloor : yFloor + 1;

                // Interpolation weights
                float xWeight = x - xFloor;
                float yWeight = y - yFloor;

                // Bilinear interpolation
                float topLeft = originalHeights[yFloor, xFloor];
                float topRight = originalHeights[yFloor, xCeil];
                float bottomLeft = originalHeights[yCeil, xFloor];
                float bottomRight = originalHeights[yCeil, xCeil];

                float top = topLeft * (1 - xWeight) + topRight * xWeight;
                float bottom = bottomLeft * (1 - xWeight) + bottomRight * xWeight;

                resampledHeights[i, j] = top * (1 - yWeight) + bottom * yWeight;
            }
        }

        return resampledHeights;
    }

}


/// <summary>
/// Contains all information for the HeightData
/// </summary>
public struct AsciiHeightData
{
    public float[,] heights;
    public double colScalingFactor;
    public int gridSizeInMeter;

    public int ncols;
    public int nrows;
}