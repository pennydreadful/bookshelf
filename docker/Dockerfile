# syntax=docker/dockerfile:1.2

FROM --platform=$BUILDPLATFORM ghcr.io/linuxserver/baseimage-alpine:3.21

ARG TARGETPLATFORM

ENV XDG_CONFIG_HOME="/config/xdg" \
    COMPlus_EnableDiagnostics=0 \
    TMPDIR=/run/readarr-temp

RUN apk add -U --upgrade --no-cache \
    icu-libs \
    sqlite-libs \
    xmlstarlet

ARG GIT_BRANCH
ARG COMMIT_HASH
ARG BUILD_DATE

ENV METADATA_URL "https://api.bookinfo.pro"

RUN mkdir -p /app/readarr/bin

RUN --mount=type=bind,source=_output,target=_output \
    cp -r /_output/UI /app/readarr/bin/UI && \
    case "$TARGETPLATFORM" in \
    "linux/amd64") cp -r /_output/net6.0/linux-x64 /app/readarr/bin/Readarr ;; \
    "linux/arm64") cp -r /_output/net6.0/linux-arm64 /app/readarr/bin/Readarr ;; \
    "darwin/amd64") cp -r /_output/net6.0/osx-x64 /app/readarr/bin/Readarr ;; \
    "darwin/arm64") cp -r /_output/net6.0/osx-arm64 /app/readarr/bin/Readarr ;; \
    "windows/amd64") cp -r /_output/net6.0/win-x64 /app/readarr/bin/Readarr ;; \
    *) echo "Unknown platform: $TARGETPLATFORM" && exit 1 ;; \
    esac

RUN echo -e "UpdateMethod=docker\nBranch=${GIT_BRANCH}\nPackageVersion=${COMMIT_HASH}}\nPackageAuthor=pennydreadful" > /app/readarr/package_info && \
    printf "version: ${COMMIT_HASH}}\nBuild-date: ${BUILD_DATE}" > /build_version

# copy local files
COPY docker/root/ /

# ports and volumes
EXPOSE 8787

VOLUME /config
