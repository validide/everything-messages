FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim

ENV TZ=UTC

# Copy `runtime-image-details.sh`
COPY runtime-image-details.sh /opt/runtime-image-details.sh

RUN apt-get update && \
    # timezone support
    apt-get -y install tzdata && \
    cp -nu /usr/share/zoneinfo/${TZ} /etc/localtime && \
    echo $TZ > /etc/timezone && \
    # clean
    apt-get clean && rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/* && \
    chmod 755 /opt/runtime-image-details.sh


# Run the runtime start-up script
CMD [ "/bin/sh", "/opt/runtime-image-details.sh" ]
