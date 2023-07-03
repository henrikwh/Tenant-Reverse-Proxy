# Reverse Proxy - managing and routing tenants 

Tenant routing rules, claims processing, and authentication protocol translations are often needed for exposing a backend to any number of frontends and clients. A reverse proxy serves to hide some of this complexity for the frontends connecting as well as centralizing the management.  Scenarios are amongst:

* Frontend leverages modern OAuth, backend services do not. Transformation is needed for downstream calls. Like OICD/OAuth transformation to other legacy auth used downstream from proxy
* Token claim enrichments or creating new tokens to be used downstream - i.e. add additional claims, like membership, etc.
* Existing backend has specific tenant routing requirements like subdomains (**customer1**.contoso.com), query parameters, path segments, headers, or claims from bearer tokens. 
* Specific handling of tenants in a multitenant backend (like special treatment for paid vs unpaid tenants) or handling limits pr tenant, pr client, or other dimensions

A reverse proxy or gateway can solve these and other challenges, as it sits between client devices and one or more backend servers, forwarding client requests to servers and then returning the server's response back to the client. The [Gateway Routing pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/gateway-routing) speaks to and addresses these challenges.

Current sample uses [YARP proxy](https://microsoft.github.io/reverse-proxy/articles/getting-started.html), as the reverse proxy implementation. Services like Application Gateway and API Management, provide much of the same, and more, functionality. These existing PaaS services do not offer token-based routing or claims transformation, like adding claims or mapping to other auth protocols. YARP provides that flexibility, as the pipeline is fully extendable. 

### Building and deploying the sample

Install scripts are tested on Ubuntu on WSL.

*Main principle for the scripts* is to store variables in `config.json` once scripts run. That will result in specific deployment parameters, which is used in bicep to provision and configure. `config.json` is also used directly by asp.net core configuration, if the sample is running locally.

1. Clone the repository
2. Go to the scripts folder
3. Optional: run `./preReqs.sh` to install needed prerequisites. 
   For manual installation of the prerequisites: `apt install jq zip azure-cli dotnet-sdk-7.0`
4. run `./aadApp.sh` to create application registration in AAD. This will create a multitenant AAD app.
5. run `./provision.sh` to provision Azure resources, using bicep.
6. run `./deploy.sh` to build solution and deploy the solution
7. run `./addCurrentTenantToRepository.sh`, this will create a registration which will route your tenant the the weather api installed for testing 
   routing information is stored in App Configuration under `TenantDirectory:Tenants$Production` with this format:

    ```json
    [
        {
            "Tid": "{Id of the AAD tenant, for which the proxy will route requests}",
            "Destination": "https://{Replace with weather api prefix}.azurewebsites.net",
            "Alias": "{Tenant-Something}",
            "State":"Enabled"
        }
    ]
    ```

#### Setting up Postman to call the APIs

An Postman environment is generated to help setup the authentication and to make the request against the proxy.

1) In scripts folder, run `createPostmanEnvironment.sh`
2) Open Postman and import the generated files `TenantProxy-dev.postman_environment.json` and `TenantProxy.postman_collection.json`
   ![image-20230629114232652](https://hwhfta.blob.core.windows.net/typoraimages/2023%2F06%2F29%2F11%2F42%2Fimage-20230629114232652----S0CFV86241SW42FCY2J9M8XZKW.png)
3) In postman select the imported environment, to make sure the right variables are used.
4) Get a new access token, and press "Use Token"
    ![image-20230629115606224](https://hwhfta.blob.core.windows.net/typoraimages/2023%2F06%2F29%2F11%2F56%2Fimage-20230629115606224----ESDPZN5KTFA75AW4JF257ZSSDG.png)
5) Call the API, using the token
    ![image-20230629115216440](https://hwhfta.blob.core.windows.net/typoraimages/2023%2F06%2F29%2F11%2F52%2Fimage-20230629115216440----Y4NA41CBGY64GE4EHRA5Q1CFN8.png)
6) Enjoy the weather, served through the proxy, by the backend API the tenant is routed to

#### Running locally

From the scripts folder do the following:

1) Make sure that the above setup steps has been performed, to provision resources on azure
2) From scripts folder run `createDevServicePrincipal.sh`. This will create a service principal with permissions identical to the managed identity. Values are stored in `config.json` and read as part of the startup.
3) If testing with postman, use the request in the localhost folder. 

## Main components

The solution consists of the following:

1. YARP proxy implementation which routes tenants to different backends based on the AAD tenant id. Deployed on an Azure web application. 

1. A Tenant Repository (```ITenantRepository```), is used to serve information regarding tenants. For data persistence, it's using Azure App Configuration as the store for known tenants. Any data store could be used. Azure App Configuration provides some benefits like versioning and restore of configurations and change events.

1. Backend API, which tenants are routed to. This is the weather API, but *configured to authorize using the token issued from the proxy*. 

1. A tenant management service (API). This acts as the interface to update the tenant repository. Not deployed with scripts above.

## Tenant routing

The end-to-end flow for performing a request from a client (tenant) to a designated backend involved these steps:

1. Proxy receives an authorized request

2. Proxy inspects the bearer token and extracts claim needed. In this case only `http://schemas.microsoft.com/identity/claims/tenantid` claim is used

3. Proxy uses tenant id to retrieve tenant information in Tenant Repository. Tenant Repository contains hosting Uri and state of the tenant (enabled or disabled)

4. Additional permissions are read from the 'Permission Service', depending on what the downstream services need

5. Proxy creates a new self-signed JWT and uses that downstream when proxying to the hosting location for the tenant. This could be any transformation required for calling downstream services. In this sample, the  permission service is called to add additional claims to the JWT

6. Request is proxied based on information from the "tenant repository".

Sequence diagram below illustrates the flow.

```mermaid
sequenceDiagram
autonumber
Client->>AAD : Login
AAD-->>Client : token
Client->>Proxy : Call with Token (access_as_user)
Proxy->> Tenant Repository : Lookup tenant claim:tenantId 
alt tenant is registered in Tenant Repository
Tenant Repository -->> Proxy : Tenant hosting address / metadata
Proxy->> Permission Service : Lookup permissions for tenant
Permission Service -->> Proxy : Permissions 
Proxy ->> Proxy : Create JWT for calling downstream
Proxy ->> App Service : Proxy request to tenant hosting location, using created JWT
App Service ->> App Service : Validate token from proxy
App Service -->> Proxy : response

else tenants is not known
Tenant Repository -->> Proxy : 401 
end
Proxy ->> Proxy : Response transformations
Proxy -->> Client : response
```


## Tenant management

The sample includes a simple management abstraction that allows for configuring tenant settings/routing.  Configuration storage is handled in Azure App Configuration as the [external configuration store](https://learn.microsoft.com/en-us/azure/architecture/patterns/external-configuration-store), but any other backing store could be implemented. This would typically be deployed as part of the SaaS [control plane](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/considerations/control-planes). A tenant management component/service handles scenarios like:

- Adding, removing/blocking tenants
- Assigning capacity, limits pr tenant, limits pr hosting service
- Reassigning tenants to new hosts, by changing destination for tenants

Here changes to the routing configuration are pushed to subscribing proxies. 

###### The high level sequence diagram for calling the backend looks like:

```mermaid
sequenceDiagram
autonumber
actor Client

#RP ->> CS : Proxy startup, read lastest configuration
box Data plane
participant RP as Proxy
participant BEA as Backend A
participant BEB as Backend B
end
RP ->> +CS : Proxy startup, get configuration
CS -->> -RP : Lastest configuration
Client ->>+RP : Request, tenant1
RP ->> RP : Process incomping token, create token for downstream
RP->> BEA : GET
BEA ->> RP : Ok
RP ->> -Client : Ok

box Control plane
participant CS as Tenant Repository
participant MS as Management Service
end
Actor DevOps
DevOps ->> MS : Request to disable tenant 
MS->> CS : Update tenant information
CS->> RP : Notify proxies, that tenant information is updated
RP ->> CS : Get new values
CS -->> RP : Tenant and routing information
RP->RP : Refresh configuration, due to configuration change notification
Client ->>+RP : Request, tenant1
RP ->> RP : Process incomping token
RP ->> -Client : 404

```

Some key considerations are that the proxy requires hosting, maintenance, and operations and that the proxy is on the critical path. Application Gateway or API Management are hosted services and takes these responsibilities. Besides that, adding a proxy, or gateway, have a cost and adds an additional layer of processing which will have some performance impact.  Networking configuration has not been included in this sample, where for most cases, deployments would be performed into separate subnets and a web application firewall would front the proxy. 


### Observability

The sample is configured to send to application insights. This gives the operational telemetry needed to inspect how proxy and downstream services is performing and operating. 

> **Note:**  Trace levels are configured centrally in App Configuration, in the configuration `API:Settings${environment}`. Other application insights functionality, like dependency tracing, can also be centrally enabled or disabled.

Having visibility into the end-to-end request is essential. The KQL below shows how a specific request can be traced using operation id.

```kql
let opid ="INSERT OPERATIOb_ID";
union requests,traces,dependencies, exceptions
| where operation_Id == opid or operation_ParentId  == opid
| extend CategoryName_ = tostring(customDimensions.CategoryName)
| extend executingProcess = strcat(cloud_RoleName, '-',cloud_RoleInstance)
| project-reorder timestamp,itemType, operation_Id, executingProcess, name, message,CategoryName_, url, duration
| order by timestamp asc  		
```

![Distributed tracing](https://hwhfta.blob.core.windows.net/typoraimages/2023%2F06%2F13%2F14%2F28%2FDistributedTracing----ATZCD14VKBAZHJ1ZHJMHQSQNCR.png)

Requests, traces and dependencies are logged. Above show request to the proxy, trace for the proxy (Proxying to..), the dependency trace, the request to the weather API, and finally the trace response. 

#### Application map

The application map, shows the communication between services. The obvious are calls from proxy and to downstream weather API. There's also a few other dependencies shown, that illustrates how the service operates:

<img src="https://hwhfta.blob.core.windows.net/typoraimages/2023%2F06%2F23%2F11%2F56%2Fimage-20230623115634652----VNYYS0RZAWXWXG4NF6FW282HNC.png" alt="image-20230623115634652" style="zoom:50%;" />

* `127.0.0.1:*` - looking into that reveals `GET 127.0.0.1:41793/msi/token/`. This is the service getting a token for the managed identity used to connect other services
* `proxyservicebus-*` is the subscription to the Service Bus topic which nodes subscribe to, to get notified of configuration changes
* `configurationclient`  - calls to configuration client are triggered once a node receives a message on the Service Bus. 

### Summary

A reverse proxy works as an intermediary, in this case between clients and backends. Having a proxy or gateway in between enables inspection and rewrite of requests. For this sample, the scenario is to route tenants identified by the tenant id claim in the bearer token, to the right hosting endpoint in the backend. Tenant management is implemented to hold the individual configurations of the tenants.   

#### Resources

[YARP Documentation (microsoft.github.io)](https://microsoft.github.io/reverse-proxy/)

[Backends for Frontends pattern - Azure Architecture Center | Microsoft Learn](https://learn.microsoft.com/en-us/azure/architecture/patterns/backends-for-frontends)

Gateway routing: https://learn.microsoft.com/en-us/azure/architecture/patterns/gateway-routing

[External Configuration Store pattern - Azure Architecture Center | Microsoft Learn](https://learn.microsoft.com/en-us/azure/architecture/patterns/external-configuration-store)

[Federated Identity pattern - Azure Architecture Center | Microsoft Learn](https://learn.microsoft.com/en-us/azure/architecture/patterns/federated-identity)

