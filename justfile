platform     := ""
push         := "true"
load         := "true"
tag          := "0.0.0.0-local"

build imageTag dockerFile target="":
  #!/usr/bin/env bash

  target_parameter=""
  if [ "{{target}}" != "" ]; then
    target_parameter="--target {{target}}"
  fi
  platform_parameter=""
  if [ "{{platform}}" != "" ]; then
    platform_parameter="--platform {{platform}}"
  fi
  push_parameter=""
  if [ "{{push}}" == "true" ]; then
    push_parameter="--push"
  fi
  load_parameter=""
  if [ "{{load}}" == "true" ]; then
    load_parameter="--load"
  fi

  set -x
  docker buildx build --progress=plain --build-arg VERSION=${VERSION} --platform=linux/arm64,linux/amd64 --load=false --push=true -t dockerhubaneo/armonik_worker_dll:${VERSION} -f Worker/src/DLLWorker/Dockerfile .
