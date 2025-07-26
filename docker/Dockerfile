# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM ghcr.io/linuxserver/baseimage-alpine:3.21

ARG TARGETPLATFORM

ARG GIT_BRANCH
ARG COMMIT_HASH
ARG RUN_NUMBER
ARG BUILD_DATE

# environment settings
ARG READARR_BRANCH="develop"
ENV XDG_CONFIG_HOME="/config/xdg" \
    COMPlus_EnableDiagnostics=0 \
    TMPDIR=/run/readarr-temp

RUN \
    echo "**** install packages ****" && \
    apk add -U --upgrade --no-cache \
    icu-libs \
    sqlite-libs \
    xmlstarlet && \
    echo "**** install readarr ****" && \
    mkdir -p /app/readarr/bin && \
    case "$TARGETPLATFORM" in \
    "linux/amd64") curl -o /tmp/readarr.tar.gz "https://github.com/pennydreadful/reader/actions/runs/${RUN_NUMBER}/artifacts/Readarr.linux-core-x64.tar.gz" ;; \
    "linux/arm64") curl -o /tmp/readarr.tar.gz "https://github.com/pennydreadful/reader/actions/runs/${RUN_NUMBER}/artifacts/Readarr.linux-core-arm64.tar.gz" ;; \
    "darwin/amd64") curl -o /tmp/readarr.tar.gz "https://github.com/pennydreadful/reader/actions/runs/${RUN_NUMBER}/artifacts/Readarr.osx-core-x64.tar.gz" ;; \
    "windows/amd64") curl -o /tmp/readarr.tar.gz "https://github.com/pennydreadful/reader/actions/runs/${RUN_NUMBER}/artifacts/Readarr.windows-core-x64.zip" ;; \
    *) echo "Unknown platform: $TARGETPLATFORM" && exit 1 ;; \
    esac

RUN tar xzf \
    /tmp/readarr.tar.gz -C \
    /app/readarr/bin --strip-components=1 && \
    echo -e "UpdateMethod=docker\nBranch=${GIT_BRANCH}\nPackageVersion=${COMMIT_HASH}}\nPackageAuthor=pennydreadful" > /app/readarr/package_info && \
    printf "version: ${COMMIT_HASH}}\nBuild-date: ${BUILD_DATE}" > /build_version && \
    echo "**** cleanup ****" && \
    rm -rf \
    /app/readarr/bin/Readarr.Update \
    /tmp/*

RUN ls -la /app/readarr/bin/

# copy local files
COPY root/ /

# ports and volumes
EXPOSE 8787

VOLUME /config
