---
title: "Installation"
weight: 10
bookCollapseSection: false
---
# Installation instructions

The application is typically installed using a docker image. Other installation types will be available in the future.

## Docker

To install the application with docker you need to create two volumes first:

| name      | path    | description                                 |
|-----------|---------|---------------------------------------------|
| config    | /config | Contains the application settings.          |
| manifests | /data   | Contains the manifest files                 |

As a simple `ApplicationSettings.yaml` file you can use the following:

```yaml
Logging:
  LogToConsole: true
  MinimumLogLevel: Information  
``` 

Copy your manifest file in the data directory and name it `Manifest.yaml`.

With that we can now start the docker container via:

```bash
docker run -d \
  --restart=always \
  -v MyVolume/config:/config \
  -v MyVolume/manifests:/data \
  "osgaga/mqtt2otel:latest"
```

or if you are using docker-compose:

```yaml
version: "3.9"

services:
  mqtt2otel:
    container_name: mqtt2otel
    image: "osgaga/mqtt2otel"
    volumes:
      - ./config:/config
      - ./manifests:/data
```

and then

```bash
docker-compose up -d
```