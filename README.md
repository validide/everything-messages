# everything-messages
A place to play around with messaging

## Setup local images

Create the local runtime images
``` sh
# DotNET runtime image
docker build --tag em-dotnet-runtime-image ./src/Dockers/runtime/
# Check image details
docker run -it --rm em-dotnet-runtime-image


# Quartz.NET PostgreSQL image
docker build --tag em-scheduler-db ./src/Dockers/quartz-net-pgsql/

```

## Starting the system.
``` sh
docker-compose build

docker-compose up

# open http://localhost:15672/#/
# open http://localhost:7000/swagger/index.html


# docker compose stop http-endpoint
# docker compose start http-endpoint
```
