#!/bin/bash
set -e

FRAMEWORK="net6.0"
RUNTIME="linux-x64"

outputFolder='_output'
testPackageFolder='_tests'

rm -rf $outputFolder
rm -rf $testPackageFolder

slnFile=src/Readarr.sln

platform=Posix
application=Readarr.dll

dotnet clean $slnFile -c Debug
dotnet clean $slnFile -c Release

dotnet msbuild -restore $slnFile -p:Configuration=Debug -p:Platform=$platform -p:RuntimeIdentifiers=$RUNTIME -t:PublishAllRids

dotnet new tool-manifest
dotnet tool install --version 6.6.2 Swashbuckle.AspNetCore.Cli

dotnet tool run swagger tofile --output ./src/Readarr.Api.V1/openapi.json "$outputFolder/$FRAMEWORK/$RUNTIME/$application" v1 &

sleep 45

kill %1

exit 0
