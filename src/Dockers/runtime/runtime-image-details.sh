#!/bin/bash

TIME_ZONE=$(cat /etc/timezone)
CURRENT_DATETIME=$(date --iso-8601=ns)
HOSTNAME=$(hostname)
echo "
+++++++++++++++++++++++++++++ IMAGE DETAILS +++++++++++++++++++++++++++++
CURRENT DATETIME: $CURRENT_DATETIME
TIME ZONE: $TIME_ZONE
HOSTNAME: $HOSTNAME
+++++++++++++++++++++++++++++ IMAGE DETAILS +++++++++++++++++++++++++++++
"
