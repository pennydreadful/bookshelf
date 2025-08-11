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
ARG METADATA_URL=https://api.bookinfo.pro
ARG HARDCOVER

LABEL maintainer="pennydreadful"

ENV METADATA_URL=${METADATA_URL}
ENV HARDCOVER=${HARDCOVER}

RUN mkdir -p /app/readarr

RUN --mount=type=bind,source=_artifacts,target=_artifacts \
    case "$TARGETPLATFORM" in \
    "linux/amd64") cp -r /_artifacts/linux-musl-x64/net6.0/Readarr /app/readarr/bin ;; \
    "linux/arm64") cp -r /_artifacts/linux-musl-arm64/net6.0/Readarr /app/readarr/bin ;; \
    "darwin/amd64") cp -r /_artifacts/osx-x64/net6.0/Readarr /app/readarr/bin ;; \
    "darwin/arm64") cp -r /_artifacts/osx-arm64/net6.0/Readarr /app/readarr/bin ;; \
    "windows/amd64") cp -r /_artifacts/win-x64/net6.0/Readarr /app/readarr/bin ;; \
    *) echo "Unknown platform: $TARGETPLATFORM" && exit 1 ;; \
    esac

RUN echo -e "UpdateMethod=docker\nBranch=${GIT_BRANCH}\nPackageVersion=${COMMIT_HASH}}\nPackageAuthor=pennydreadful" > /app/readarr/package_info && \
    printf "version: ${COMMIT_HASH}}\nBuild-date: ${BUILD_DATE}" > /build_version

# copy local files
COPY docker/root/ /

# ports and volumes
EXPOSE 8787

VOLUME /config
