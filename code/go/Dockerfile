# syntax=docker/dockerfile:1

FROM golang:1.25.1-trixie AS builder
WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download
COPY main.go ./
RUN CGO_ENABLED=0 go build -o app .

FROM debian:trixie-slim
WORKDIR /app
COPY --from=builder /src/app .

ENTRYPOINT ["./app"]

ARG IMAGE_CREATED=2025-01-01T00:00:00Z
ARG IMAGE_VERSION=0.1
ARG IMAGE_REVISION=4b825dc642cb6eb9a060e54bf8d69288fbee4904
# https://github.com/opencontainers/image-spec/blob/main/annotations.md
LABEL org.opencontainers.image.created="${IMAGE_CREATED}" \
  org.opencontainers.image.authors="Vladimir Chirikov" \
  org.opencontainers.image.url="https://github.com/vchirikov/berloga" \
  org.opencontainers.image.documentation="https://github.com/vchirikov/berloga" \
  org.opencontainers.image.source="https://github.com/vchirikov/berloga" \
  org.opencontainers.image.version="${IMAGE_VERSION}" \
  org.opencontainers.image.revision="${IMAGE_REVISION}" \
  org.opencontainers.image.licenses="MIT" \
  org.opencontainers.image.title="go-json-bench" \
  org.opencontainers.image.description="Simple go/GIN app to benchmark vs dotnet/aspnetcore"
