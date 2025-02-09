using Microsoft.Extensions.Logging;
using OSGeo.OSR;

namespace RunnersPal.Elevation;

public class ElevationLookup(
    ILogger<ElevationLookup> logger,
    IElevationSummaryDataSource elevationSummaryDataSource)
    : IElevationLookup
{
    static ElevationLookup()
    {
        MaxRev.Gdal.Core.GdalBase.ConfigureAll();
    }

    private const double _seaLevel = 0;

    public async IAsyncEnumerable<double> LookupAsync(IEnumerable<ElevationPoint> points)
    {
        foreach (var point in points)
            yield return await LookupAsync(point);
    }

    public async Task<double> LookupAsync(ElevationPoint point)
    {
        var tifFile = await elevationSummaryDataSource.GetFilenameForPointAsync(point);
        logger.LogTrace("Using TIF file [{TifFile}] for point [{Point}]", tifFile, point);

        // TODO: refactor this so we cache the dataset

        using var ds = OSGeo.GDAL.Gdal.Open(tifFile, OSGeo.GDAL.Access.GA_ReadOnly);
        logger.LogTrace("Loaded file {Description}, raster size ({RasterXSize}, {RasterYSize})", ds.GetDescription(), ds.RasterXSize, ds.RasterYSize);

        SpatialReference spatialReferenceRaster = new(ds.GetProjection());
        SpatialReference spatialReference = new(null);
        spatialReference.ImportFromEPSG(4326);
        var coordinateTransform = Osr.CreateCoordinateTransformation(spatialReference, spatialReferenceRaster, new());
        double[] geoTransform = new double[6];
        ds.GetGeoTransform(geoTransform);
        var dev = geoTransform[1] * geoTransform[5] - geoTransform[2] * geoTransform[4];
        double[] geoTransformInv = [geoTransform[0], geoTransform[5] / dev, -geoTransform[2] / dev, geoTransform[3], -geoTransform[4] / dev, geoTransform[1] / dev];

        double[] points = new double[3];
        coordinateTransform.TransformPoint(points, point.Longitude, point.Latitude, 0);
        logger.LogTrace("Transformed point to: {Points}", string.Join(',', points));

        var u = points[0] - geoTransformInv[0];
        var v = points[1] - geoTransformInv[3];
        var xpix = geoTransformInv[1] * u + geoTransformInv[2] * v;
        var ylin = geoTransformInv[4] * u + geoTransformInv[5] * v;
        logger.LogTrace("Calculated raw xpix,ylin: {Xpix},{Ylin}", xpix, ylin);

        int xpixConv = Convert.ToInt32(Math.Floor(xpix));
        int ylinConv = Convert.ToInt32(Math.Floor(ylin));
        logger.LogTrace("Using raster band lookup {XpixConv},{YlinConv}", xpixConv, ylinConv);

        if (xpixConv < 0 || xpixConv >= ds.RasterXSize ||
            ylinConv < 0 || ylinConv >= ds.RasterYSize)
        {
            throw new InvalidOperationException($"Invalid offset, cannot lookup point {point} from raster file {tifFile}.");
        }

        var b = ds.GetRasterBand(1);
        double[] buffer = new double[1];
        var readResult = b.ReadRaster(xpixConv, ylinConv, 1, 1, buffer, 1, 1, 0, 0);
        if (readResult != OSGeo.GDAL.CPLErr.CE_None)
            throw new InvalidOperationException($"Could not lookup point {point} from raster file {tifFile}. Error returned: {readResult}");

        var valueAtPoint = buffer[0];
        logger.LogTrace("Got elevation: {ValueAtPoint}", valueAtPoint);

        return valueAtPoint == -32768 ? _seaLevel : valueAtPoint;
    }
}
