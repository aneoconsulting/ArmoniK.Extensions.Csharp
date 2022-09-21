#!/bin/bash
set -e

export MODE="All"
export SERVER_NFS_IP=""
export STORAGE_TYPE="HostPath"
configuration=Debug
FRAMEWORK=net6.0
OUTPUT_JSON="nofile"
TO_BUCKET=false
PACKAGE_NAME="ArmoniK.EndToEndTests-v1.0.0-700.zip"
RELATIVE_PROJECT="./EndToEnd.Tests"
RELATIVE_CLIENT=""
DEFAULT=FALSE
args=""

BASEDIR=$(dirname "$0")
pushd $BASEDIR
BASEDIR=$(pwd -P)
popd

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color
DRY_RUN="${DRY_RUN:-0}"

pushd $(dirname $0) >/dev/null 2>&1
BASEDIR=$(pwd -P)
popd >/dev/null 2>&1

TestDir=${BASEDIR}/$RELATIVE_PROJECT

cd ${TestDir}
kubectl get svc -n armonik -o wide
export CPIP=$(kubectl get svc ingress -n armonik -o custom-columns="IP:.spec.clusterIP" --no-headers=true)
export CPPort=$(kubectl get svc ingress -n armonik -o custom-columns="PORT:.spec.ports[1].port" --no-headers=true)
export Grpc__Endpoint=http://$CPIP:$CPPort
export Grpc__SSLValidation="false"
export Grpc__CaCert=""
export Grpc__ClientCert=""
export Grpc__ClientKey=""
export Grpc__mTLS="false"
export ZipFolder="${HOME}/data"

nuget_cache=$(dotnet nuget locals global-packages --list | awk '{ print $2 }')

function SSLConnection()
{
    export Grpc__mTLS="true"
    export Grpc__Endpoint=https://$CPIP:$CPPort
    export Grpc__SSLValidation="disable"
    export Grpc__CaCert=${BASEDIR}/../../../infrastructure/quick-deploy/localhost/armonik/generated/certificates/ingress/ca.crt
    export Grpc__ClientCert=${BASEDIR}/../../../infrastructure/quick-deploy/localhost/armonik/generated/certificates/ingress/client.crt
    export Grpc__ClientKey=${BASEDIR}/../../../infrastructure/quick-deploy/localhost/armonik/generated/certificates/ingress/client.key
}

function GetGrpcEndPointFromFile()
{
  OUTPUT_JSON=$1
  if [ -f ${OUTPUT_JSON} ]; then
    #Test if ingress exists
    link=`cat ${OUTPUT_JSON} | jq -r -e '.armonik.ingress.control_plane'`
    if [ "$?" == "1" ]; then
      link=`cat ${OUTPUT_JSON} | jq -r -e '.armonik.control_plane_url'`
      if [ "$?" == "1" ]; then
        echo "Error : cannot read Endpoint from file ${OUTPUT_JSON}"
        exit 1
      fi
    fi
    export Grpc__Endpoint=$link
  fi
  echo "Running with endPoint ${Grpc__Endpoint} from output.json"
}

function GetGrpcEndPoint()
{
    export Grpc__Endpoint=$1
    echo "Running with endPoint ${Grpc__Endpoint}"
}

function createZipFolderIfNeeded()
{
  if [ ! -d "{$ZipFolder}" ]; then
    mkdir -p {$ZipFolder}
    echo "Folder {$ZipFolder} created."
  else
    echo "Folder {$ZipFolder} already exists."
  fi
}

function build() {
  cd ${TestDir}/
  echo rm -rf ${nuget_cache}/armonik.*
  rm -rf $(dotnet nuget locals global-packages --list | awk '{ print $2 }')/armonik.*
  find \( -iname obj -o -iname bin \) -exec rm -rf {} + || true
  dotnet publish --self-contained -c ${configuration} -r linux-x64 -f ${FRAMEWORK} .
}

function deploy() {
  cd ${TestDir}
  if [[ ${TO_BUCKET} == true ]]; then
    export S3_BUCKET=$(aws s3api list-buckets --output json | jq -r '.Buckets[0].Name')
    echo "Copy of S3 Bucket ${TO_BUCKET}"
    aws s3 cp "../packages/${PACKAGE_NAME}" "s3://$S3_BUCKET"
  else
    cp -v "../packages/${PACKAGE_NAME}" "${ZipFolder}"
  fi
  kubectl delete -n armonik $(kubectl get pods -n armonik -l service=compute-plane --no-headers=true -o name) || true
}

function execute() {
  echo "cd ${TestDir}/${RELATIVE_CLIENT}/"
  cd "${TestDir}/${RELATIVE_CLIENT}/"
  echo dotnet run --self-contained -r linux-x64 -f ${FRAMEWORK} -c ${configuration} $@
  dotnet run --self-contained -r linux-x64 -f ${FRAMEWORK} -c ${configuration} $@
}

function setlocalwslrunconfig(){
  #On local env we reused 'ARMONIK_SHARED_HOST_PATH' env variable if setted.
  export ZipFolder=${ARMONIK_SHARED_HOST_PATH:=${ZipFolder}}
  #if you are in your wsl = outside of kubernetes cluster you need ingress outside IP
  export CPIP=$(kubectl get svc ingress -n armonik -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
}

function usage() {
  echo "Usage: $0 [option...]  with : " >&2
  echo
  cat <<-EOF
        no option           : To build and Run tests
        -ssl                : To run in SSL mode
        -e http://endPoint  : change GRPC endpoint
        -f output_path.json : Load EndPoint from Armonik/generated/output.json
        -b | --build        : To build only test and package
        -d | --deploy       : Only Deploy package
        -r | --run          : To run only deploy package and test
        -a                  : To run only deploy package and test
        -s3                 : Need S3 copy with aws cp
        -l | --localwslrun: To test on your local WSL. ARMONIK_SHARED_HOST_PATH will be used if any for data folder
EOF
  echo
  exit 0
}

function printConfiguration() {
  echo
  echo "******* Configuration used : *******"
  echo "Running script $0 $@"
  echo "SSL is actived ? ${Grpc__mTLS}"
  echo "SSL check strong auth server [${Grpc__SSLValidation}]"
  echo "SSL Client file [${Grpc__ClientCert}]"
  echo "Data folder : [${ZipFolder}]"
  echo "Control plane IP : [${CPIP}]"
  echo "************************************"
  echo
}

function main() {
args=()

while [ $# -ne 0 ]; do
  echo "NB Arguments : $#"

    case "$1" in
    -ssl)
      SSLConnection
      shift
      ;;
    -s3)
      TO_BUCKET=true
      shift
      ;;
    -e | --endpoint)
      GetGrpcEndPoint "$2"
      shift
      shift
      ;;

    -f | --file)
      GetGrpcEndPointFromFile "$2"
      shift
      shift
      ;;

    -h | --help)
      usage
      exit
      ;;

    -l | --localwslrun)
      setlocalwslrunconfig
      shift
      ;;
    *)
      echo "Add Args '$1' to list ${args[*]}"
      args+=("$1")
      shift
      ;;
    esac
  done

  createZipFolderIfNeeded
  printConfiguration

echo "List of args : ${args[*]}"

 if [[ "${#args[@]}" == 0 ]]; then
    echo "Execute default run"
    build
    deploy
    execute "${args[@]}"
    exit 0
  fi

  while [ ${#args[@]} -ne 0 ]; do
    echo "Parse ${args[0]}"
    case "${args[0]}" in
    -r | --run)
      args=("${args[@]:1}") # past argument=value
      echo "Only execute without build '${args[@]}'"
      execute "${args[@]}"
      break
      ;;
    -b | --build)
      args=("${args[@]:1}") # past argument=value
      echo "Only execute without build '${args[@]}'"
      build
      break
      ;;
    -d | --deploy)
      args=("${args[@]:1}") # past argument=value
      echo "Only deploy package'${args[@]}'"
      deploy
      break
      ;;
    -a)
      # all build and execute
	    args=("${args[@]:1}") # past argument=value
      echo "Build and execute '${args[@]}'"
      break
      build
      deploy
      execute "${args[@]}"
      ;;
    *)
	# unknown option
      echo "Running all with args ['${args[@]}']"
      echo "Build and execute '${args[@]}'"
      build
      deploy
      execute "${args[@]}"
      exit 0
      ;;
    esac
  done
}

main "$@"
