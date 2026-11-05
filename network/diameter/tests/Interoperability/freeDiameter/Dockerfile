FROM debian:bookworm-slim AS build

ARG FREEDIAMETER_COMMIT=5403feb543ed5e720165a4b3a3b4a365cdee28fb

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        bison \
        build-essential \
        ca-certificates \
        cmake \
        flex \
        git \
        libgcrypt20-dev \
        libgnutls28-dev \
        libidn2-dev \
    && git init /src/freeDiameter \
    && git -C /src/freeDiameter remote add origin https://github.com/freeDiameter/freeDiameter.git \
    && git -C /src/freeDiameter fetch --depth 1 origin "${FREEDIAMETER_COMMIT}" \
    && git -C /src/freeDiameter checkout --detach FETCH_HEAD \
    && cmake -S /src/freeDiameter -B /src/build \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_INSTALL_PREFIX=/opt/freeDiameter \
        -DBUILD_TESTING=OFF \
        -DDIAMID_IDNA_REJECT=ON \
        -DDISABLE_SCTP=ON \
    && cmake --build /src/build --parallel 2 \
    && cmake --install /src/build

FROM debian:bookworm-slim

LABEL org.opencontainers.image.source="https://github.com/freeDiameter/freeDiameter"
LABEL org.opencontainers.image.version="1.6.0"
LABEL org.opencontainers.image.revision="5403feb543ed5e720165a4b3a3b4a365cdee28fb"

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        libgcrypt20 \
        libgnutls30 \
        openssl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /etc/freeDiameter/tls \
    && openssl req -x509 -newkey rsa:2048 -nodes -days 3650 \
        -subj /CN=relay.interop.test \
        -keyout /etc/freeDiameter/tls/relay.key \
        -out /etc/freeDiameter/tls/relay.pem

COPY --from=build /opt/freeDiameter /opt/freeDiameter
COPY freeDiameter.conf /etc/freeDiameter/freeDiameter.conf
COPY acl_wl.conf /etc/freeDiameter/acl_wl.conf

EXPOSE 3868/tcp

ENTRYPOINT ["/opt/freeDiameter/bin/freeDiameterd"]
CMD ["-c", "/etc/freeDiameter/freeDiameter.conf"]
