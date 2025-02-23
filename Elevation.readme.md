# Elevation data

To populate the elevation data, run the CLI app:

dotnet run --project RunnersPal.Elevation.Cli

This will download and extract the TIF files, create tiles from the master files, and a summary json file to map coordinates to a tile.
In the appsettings file, then set the path to the tiles parent directory, e.g.

```json
{
    "ElevationPath": "/.../RunnersPal.Core/external/elevation",
}
```

The extracted files are quite large, around 12GB.
