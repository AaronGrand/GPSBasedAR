public enum HeightModels
{
    SRTMGL3,
    SRTMGL1
}

public static class HeightModelUtility
{
    /// <summary>
    /// Gets the API reference to a HeightModel.
    /// </summary>
    public static string GetAPIReference(HeightModels heightModel)
    {
        switch (heightModel)
        {
            case HeightModels.SRTMGL3:
                return "SRTMGL3";
            case HeightModels.SRTMGL1:
                return "SRTMGL1";
        }
        return string.Empty;
    }

    /// <summary>
    /// Gets the GridSize from a HeightModel.
    /// </summary>
    public static int GetGridSize(HeightModels heightModel)
    {
        switch (heightModel)
        {
            case HeightModels.SRTMGL3:
                return 90;
            case HeightModels.SRTMGL1:
                return 30;
        }
        return 0;
    }
}