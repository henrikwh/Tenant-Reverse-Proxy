#!/bin/bash

set -u -e -o pipefail
echo "Building..."
source ./helpers.sh

basedir="$( dirname "$( readlink -f "$0" )" )"

#CONFIG_FILE="${basedir}/./config.json"
CONFIG_FILE="./config.json"

#get-value  ".webappEndpoint"
appName="$( get-value  ".Proxy.Endpoint" | cut -d "." -f 1)"
resourceGroupName="$( get-value  ".initConfig.resourceGroupName" | cut -d "." -f 1)"

echo "App: ${appName}"
echo "Resource group :${resourceGroupName}"
(dotnet publish "../Proxy/Proxy.csproj" -c DEBUG --output ./temp/publish ) \
    || echo fail \
        | exit 1

cd ./temp/publish ; zip -r ../myapp.zip * ; cd ../../


echo "Deploying..."
az webapp deploy --resource-group "${resourceGroupName}" --name "${appName}" --src-path ./temp/myapp.zip --type zip

rm ./temp/publish -rf

appName="$( get-value  ".API.Endpoint" | cut -d "." -f 1)"
resourceGroupName="$( get-value  ".initConfig.resourceGroupName" | cut -d "." -f 1)"

echo "App: ${appName}"
echo "Resource group :${resourceGroupName}"
(dotnet publish "../WeatherApi/WeatherApi.csproj" -c DEBUG --output ./temp/publish ) \
    || echo fail \
        | exit 1

cd ./temp/publish ; zip -r ../myweatherapi.zip * ; cd ../../


echo "Deploying..."
az webapp deploy --resource-group "${resourceGroupName}" --name "${appName}" --src-path ./temp/myweatherapi.zip --type zip


rm ./temp/publish -rf



