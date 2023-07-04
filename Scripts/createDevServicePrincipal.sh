#!/bin/bash

source ./helpers.sh

basedir="$( dirname "$( readlink -f "$0" )" )"

#CONFIG_FILE="${basedir}/./config.json"
CONFIG_FILE="./config.json"

if [ ! -f "$CONFIG_FILE" ]; then
    echo "Creating new config file"
    cp ./config-template.json "${CONFIG_FILE}"
fi



jsonpath=".initConfig.resourceGroupName"
resourceGroupName="$( get-value  "${jsonpath}" )"
servicePrincipalName="ProxyDeveloment-$resourceGroupName"
echo $servicePrincipalName

jsonpath=".initConfig.subscriptionId"
subscriptionId="$( get-value  "${jsonpath}" )"



#az group list --query "[].{name:name}" --output tsv

#az account list --query "[].{name:name, id:id}" --output tsv


#sp=$(az ad sp create-for-rbac -n DevelopmentCredentials |jq .)


scope="/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName"
echo $scope

sp=$(az ad sp create-for-rbac --name "${servicePrincipalName}" \
                        --role 'Owner' \
                        --scopes $scope | jq .)

echo $sp
put-value      '.Development.ServicePrincipal.ClientId' "$(echo "${sp}" | jq -r '.appId' )" 
put-value      '.Development.ServicePrincipal.Secret' "$(echo "${sp}" | jq -r '.password' )" 
put-value      '.Development.ServicePrincipal.TenantId' "$(echo "${sp}" | jq -r '.tenant' )" 
put-value      '.Development.ServicePrincipal.DisplayName' "$(echo "${sp}" | jq -r '.displayName' )" 

assignee="$(echo "${sp}" | jq -r '.appId' )"
echo $assignee




#todo: Create loop over permissions already in config.json, roles







az role assignment create --assignee $assignee \
    --role  'b24988ac-6180-42a0-ab88-20f7382dd24c' \
    --scope $scope


az role assignment create --assignee $assignee \
    --role  '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b' \
    --scope $scope


az role assignment create --assignee $assignee \
    --role  '516239f1-63e1-4d78-a4de-a74fb236a071' \
    --scope $scope

az role assignment create --assignee $assignee \
    --role  '4633458b-17de-408a-b874-0445c86b69e6' \
    --scope $scope


az role assignment create --assignee $assignee \
    --role  '21090545-7ca7-4776-b22c-e363652d74d2' \
    --scope $scope

az role assignment create --assignee $assignee \
    --role  '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0' \
    --scope $scope


az role assignment create --assignee $assignee \
    --role  '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b' \
    --scope $scope


