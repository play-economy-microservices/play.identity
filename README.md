# play.identity
Play Economy Identity microservice

## Add the GitHub package source
```powershell
$version="1.0.3"
$owner="play-economy-microservices"
$gh_pat="[PAT HERE]"

dotnet pack src/Play.Identity.Contracts/ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/play.identity -o ../packages

dotnet nuget push ../packages/Play.Identity.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```

## Build the docker image
```powershell
$env:GH_OWNER="play-economy-microservices"
$env:GH_PAT="[PAT HERE]"
$appname="playeconomycontainerregistry"
docker build --secret id=GH_OWNER --secret id=GH_PAT -t "$appname.azurecr.io/play.identity:$version" .
```

## Run the docker image
```powershell
$adminPass="[PASSWORD HERE]"
$cosmosDbConnString="[CONN STRING HERE]"
$serviceBusConnString="[CONN STRING HERE]"
docker run -it --rm -p 5002:5002 --name identity -e 
MongoDbSettings__ConnectionString=$cosmosDbConnString -e 
ServiceBusSettings__ConnectionString=$serviceBusConnString -e 
ServiceSettings__MessageBroker="SERVICEBUS" -e IdentitySettings__AdminUserPassword=$adminPass play.identity:$version
```

## Publishing Docker Image 
```powershell
$appname="playeconomycontainerregistry"
$version="1.0.3"
az acr login --name $appname 
docker tag play.identity:$version "$appname.azurecr.io/play.identity:$version"
docker push "playeconomycontainerregistry.azurecr.io/play.identity:$version"
```

## Create the Kubernetes Namespace
```powershell
$namespace="identity"
kubectl create namespace $namespace 
```

## Create Kubernetes Secrets
```powershell
kubectl create secret generic identity-secrets 
--from-literal=cosmosdb-connectionString=$cosmosDbConnString
--from-literal=servicebus-connectionString=$serviceBusConnString
--from-literal=admin-password=$adminPass -n $namespace
```
