#!/bin/bash
source ./helpers.sh

basedir="$( dirname "$( readlink -f "$0" )" )"

#CONFIG_FILE="${basedir}/./config.json"
CONFIG_FILE="./config.json"

if [ ! -f "$CONFIG_FILE" ]; then
    echo "Creating new config file"
    cp ./config-template.json "${CONFIG_FILE}"
fi

jsonpath=".Proxy.Aad.ClientId"
clientId="$( get-value  "${jsonpath}" )"

#deleteRequest=$(az ad app delete --id $clientId | jq -r . )
#echo $deleteRequest

display_name="TenantProxyDemo7"
redirect_uri=https://localhost:6001
web_redirect_uri="https://localhost:7001 https://localhost:7001/signin-oidc https://oauth.pstmn.io/v1/callback"

query=$(az ad app list --display-name $display_name  | jq -r . )

#echo $query | jq .

installedClientId="$(echo "${query}" | jq -r '.[0].appId' )"  


if [ $installedClientId != 'null' ] 
then
    put-value      '.Proxy.Aad.ClientId' "$(echo "${query}" | jq -r '.[0].appId' )"  
    echo "Application already created"
#    exit 1;
else
    echo "Creating Application"
    json=$( az ad app create \
                --display-name $display_name \
                --public-client-redirect-uris $redirect_uri \
                --web-redirect-uris $web_redirect_uri \
                --only-show-errors \
                --enable-access-token-issuance true \
                --enable-id-token-issuance true \
                --sign-in-audience AzureADMultipleOrgs \
                --query "{Id:id, AppId:appId}"  | jq -r . )

    echo $json | jq .   
    put-value      '.Proxy.Aad.ClientId' "$(echo "${json}" | jq -r '.AppId' )"  
    installedClientId="$(echo "${json}" | jq -r '.AppId' )"  
    echo $installedClientId 


    #echo $installedClientId
    apiId="api://${installedClientId}"
    #echo $apiId

    api=$(echo '{
        "acceptMappedClaims": null,
        "knownClientApplications": [],
        "oauth2PermissionScopes": [{
            "adminConsentDescription": "test",
            "adminConsentDisplayName": "test",
            "id": "'$installedClientId'",
            "isEnabled": true,
            "type": "User",
            "userConsentDescription": "test",
            "userConsentDisplayName": "test",
            "value": "access_as_user"
        }],
        "preAuthorizedApplications": [],
        "requestedAccessTokenVersion": 2
    }' | jq .)
    echo $api | jq .

    echo "Updating app"
    updateResponse=$(az ad app update \
                --id $installedClientId \
                --set signInAudience="AzureADMultipleOrgs" \
                --enable-access-token-issuance true \
                --enable-id-token-issuance true \
                --identifier-uris $apiId \
                --web-redirect-uris $web_redirect_uri \
                --set api="$api" | jq -r .)


    apiPermission="${installedClientId}=Scope"
    echo  $apiPermission
    echo $installedClientId

    echo "Adding permissions"
    az ad app permission add --id $installedClientId --api $installedClientId   --api-permissions $apiPermission

    scope="api//${installedClientId}/access_as_user"
    echo $scope
    echo "Adding assiging permissions"
    az ad app permission grant --id $installedClientId --api $installedClientId --scope 'access_as_user'

    echo "Creating service principal"
    az ad sp create --id $installedClientId 
fi

echo "Granting concent"
response=$(az ad app permission admin-consent --id $installedClientId | jq .)
echo $response 

