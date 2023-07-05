#!/bin/bash
source ./helpers.sh

basedir="$( dirname "$( readlink -f "$0" )" )"

#CONFIG_FILE="${basedir}/./config.json"
CONFIG_FILE="./config.json"

if [ ! -f "$CONFIG_FILE" ]; then
    cp ./config-template.json "${CONFIG_FILE}"
fi


cp ./postman/* .
mv Template-TenantProxy-dev.postman_environment.json TenantProxy-dev.postman_environment.json
mv Template-TenantProxy.postman_collection.json TenantProxy.postman_collection.json



proxyClientId="$( get-value  ".Proxy.Aad.ClientId" )"
proxySecret="$( get-value  ".Proxy.Aad.Secret" )"
url="$( get-value  ".Proxy.Endpoint" )"
rg="$( get-value  ".initConfig.resourceGroupName" )"

echo $rg


cat <<< $(jq --arg a1 "https://$url" '(.values[] | select(.key == "url") | .value) = $a1 ' ./TenantProxy-dev.postman_environment.json ) > TenantProxy-dev.postman_environment.json 
cat <<< $(jq --arg a1 "$proxyClientId" '(.values[] | select(.key == "clientId") | .value) = $a1 ' ./TenantProxy-dev.postman_environment.json ) > TenantProxy-dev.postman_environment.json 
cat <<< $(jq --arg a1 "$proxySecret" '(.values[] | select(.key == "clientSecret") | .value) = $a1 ' ./TenantProxy-dev.postman_environment.json ) > TenantProxy-dev.postman_environment.json 
cat <<< $(jq --arg a1 "access_as_user" '(.values[] | select(.key == "scope") | .value) = $a1 ' ./TenantProxy-dev.postman_environment.json ) > TenantProxy-dev.postman_environment.json 
cat <<< $(jq --arg a1 "TenantProxy:$rg" '.name = $a1 ' ./TenantProxy-dev.postman_environment.json ) > TenantProxy-dev.postman_environment.json 



cat <<< $(jq --arg a1 "TenantProxy:$rg" '.info.name = $a1 ' ./TenantProxy.postman_collection.json ) > TenantProxy.postman_collection.json 



echo $res