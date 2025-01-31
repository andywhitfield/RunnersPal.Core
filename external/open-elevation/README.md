# Setup

## Local

```sh
cd external/open-elevation
docker pull openelevation/open-elevation
mkdir data
xattr -w com.dropbox.ignored 1 'external/open-elevation/data'
docker run -t -i -v $(pwd)/data:/code/data openelevation/open-elevation /code/create-dataset.sh
```

The create-dataset.sh command failed - had to manually move the tif files back to the data folder:

```sh
cd data
mv ./SRTM_SE_250m_TIF/SRTM_SE_250m.tif .
mv ./SRTM_W_250m_TIF/SRTM_W_250m.tif .
mv ./SRTM_NE_250m_TIF/SRTM_NE_250m.tif .
cd ..
```

Re-running the create-dataset.sh script then worked

Lastly start the app:

```sh
docker run -t -i -v $(pwd)/data:/code/data -p 50000:8080 openelevation/open-elevation
```

This also creates an entry in docker desktop so can start/stop from there.
