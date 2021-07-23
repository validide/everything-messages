# everything-messages
A place to play around with messaging

## Setup local images

Create the local runtime image
``` sh
docker build --tag runtime-image ./src/Dockers/runtime/

# Check image details
docker run -it --rm runtime-image
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
