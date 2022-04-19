## execution

npm install -g azurite

azurite --silent --location c:\azurite --debug c:\azurite\debug.log

func start

## local.settings.json

{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "sendgrid_key":"XXXXXXXXXXXXXXXXXXXXXXXXXXX",
    "sqldb_connection": "DataSource=C:/Users/Code/netcore/azure-workflow-function/app.db;Version=3;Cache=Shared"
  },
  "Host":{
    "LocalHttpPort":7071,
    "CORS":"*"
  },
  "ConnectionStrings": {
    "sqldb_connection": "DataSource=app.db;Cache=Shared"
  }
}
