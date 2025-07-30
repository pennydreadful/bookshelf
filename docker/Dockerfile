# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM ghcr.io/linuxserver/baseimage-alpine:3.21

ARG TARGETPLATFORM

ENV XDG_CONFIG_HOME="/config/xdg" \
    COMPlus_EnableDiagnostics=0 \
    TMPDIR=/run/readarr-temp

RUN \
    echo "**** install packages ****" && \
    apk add -U --upgrade --no-cache \
    icu-libs \
    sqlite-libs \
    xmlstarlet

ARG GIT_BRANCH
ARG COMMIT_HASH
ARG BUILD_DATE

ARG LINUX_X64_URL
ARG LINUX_ARM_URL
ARG OSX_ARM_URL
ARG OSX_X64_URL
ARG WIN_X64_URL
ARG UI_URL

RUN \
    echo "**** install readarr ****" && \
    mkdir -p /app/readarr/bin && \
    case "$TARGETPLATFORM" in \
    "linux/amd64") curl -o /app/readarr/bin/readarr.gz "${LINUX_X64_URL}" ;; \
    "linux/arm64") curl -o /app/readarr/bin/readarr.gz "${LINUX_ARM_URL}" ;; \
    "darwin/amd64") curl -o /app/readarr/bin/readarr.gz "${OSX_X64_URL}" ;; \
    "darwin/arm64") curl -o /app/readarr/bin/readarr.gz "${OSX_ARM_URL}" ;; \
    "windows/amd64") curl -o /app/readarr/bin/readarr.gz "${WIN_X64_URL}" ;; \
    *) echo "Unknown platform: $TARGETPLATFORM" && exit 1 ;; \
    esac

RUN curl -o /app/readarr/bin/readarr-ui.gz"${UI_URL}"

RUN gunzip /app/readarr/bin/readarr.gz
RUN gunzip /app/readarr/bin/readarr-ui.gz

RUN echo -e "UpdateMethod=docker\nBranch=${GIT_BRANCH}\nPackageVersion=${COMMIT_HASH}}\nPackageAuthor=pennydreadful" > /app/readarr/package_info && \
    printf "version: ${COMMIT_HASH}}\nBuild-date: ${BUILD_DATE}" > /build_version

RUN echo "**** cleanup ****" && \
    rm -rf \
    /app/readarr/bin/Readarr.Update \
    /tmp/*

RUN ls -la /app/readarr/bin/

# copy local files
COPY root/ /

# ports and volumes
EXPOSE 8787

VOLUME /config
