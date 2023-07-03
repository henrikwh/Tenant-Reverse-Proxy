#!/bin/bash
source ./helpers.sh

basedir="$( dirname "$( readlink -f "$0" )" )"

#CONFIG_FILE="${basedir}/./config.json"
CONFIG_FILE="./config.json"

if [ ! -f "$CONFIG_FILE" ]; then
    cp ./config-template.json "${CONFIG_FILE}"
fi


appConfigName="$( get-value  ".AppConfiguration.Name" )"
echo $appConfigName



payload=$(echo '[{
    "Tid": "'$( get-value  ".initConfig.tenantId" )'",
    "Destination" : "https://'$( get-value  ".API.Endpoint" )'",
    "Alias": "Test",
    "State" : "Enabled"
}]' | jq .)
echo $payload  > ./temp/tenantdirectory.json
echo $payload

az appconfig kv set --name $appConfigName --key "TenantDirectory:Tenants" --value "$payload" --content-type 'application/json' -y --label Production --tags dev=yes



payload=$(echo '[{
    "Tid": "'$( get-value  ".initConfig.tenantId" )'",
    "Destination" : "https://localhost:9090",
    "Alias": "Test",
    "State" : "Enabled"
}]' | jq .)
echo $payload  > ./temp/tenantdirectory.json

az appconfig kv set --name $appConfigName --key "TenantDirectory:Tenants" --value "$payload" --content-type 'application/json' -y --label Development --tags dev=yes



