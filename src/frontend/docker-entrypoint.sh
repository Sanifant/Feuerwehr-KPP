/#!/bin/sh
set -e

# Replace env vars in template
envsubst '${UPSTREAM_HOST}:${UPSTREAM_PORT}' \
    < /etc/nginx/templates/default.conf.template \
    > /etc/nginx/conf.d/default.conf
