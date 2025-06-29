# syntax=docker/dockerfile:1

FROM ghcr.io/linuxserver/baseimage-alpine:3.21

# set version label
ARG BUILD_DATE
ARG VERSION
ARG READARR_RELEASE
LABEL build_version="Linuxserver.io version:- ${VERSION} Build-date:- ${BUILD_DATE}"
LABEL maintainer="Roxedus,thespad"

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
  if [ -z ${READARR_RELEASE+x} ]; then \
    READARR_RELEASE=$(curl -sL "https://readarr.servarr.com/v1/update/${READARR_BRANCH}/changes?runtime=netcore&os=linuxmusl" \
    | jq -r '.[0].version'); \
  fi && \
  curl -o \
  /tmp/readarr.tar.gz -L \
    "https://github.com/tmayoff/Readarr/releases/download/0.4.18.2805/Readarr.develop.0.4.18.2805-linux-musl-x64.tar.gz"

RUN tar xzf \
    /tmp/readarr.tar.gz -C \
    /app/readarr/bin --strip-components=1 && \
  echo -e "UpdateMethod=docker\nBranch=${READARR_BRANCH}\nPackageVersion=${VERSION}\nPackageAuthor=[linuxserver.io](https://www.linuxserver.io/)" > /app/readarr/package_info && \
  printf "Linuxserver.io version: ${VERSION}\nBuild-date: ${BUILD_DATE}" > /build_version && \
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
