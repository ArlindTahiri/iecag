import os
import requests
import azure.functions as func
from azure.data.tables import TableServiceClient
from datetime import datetime

connection_string = os.getenv("AZURE_STORAGE_CONNECTION_STRING")
if not connection_string:
    raise ValueError("Azure Storage connection string is not set in the environment variables.")

table_service_client = TableServiceClient.from_connection_string(conn_str=connection_string)
logs_table_client = table_service_client.get_table_client("logs")

apps = [
    {"name": "APIWrapper", "url": "https://iecagapiwrapper.azurewebsites.net/", "type": "Web App"},
    {"name": "iecag", "url": "https://iecag.azurewebsites.net/", "type": "Web App"},
    {"name": "iecagHealthCheckFunction", "url": "https://iecaghealthcheckfunction.azurewebsites.net", "type": "Function"}

]

def check_availability(url):
    try:
        response = requests.get(url)
        return response.status_code == 200
    except Exception as e:
        return False

def log_app_info(app_name, app_type, is_available, check_time):
    try:
        logs_table_client.create_entity({
            "PartitionKey": "log_apps",
            "RowKey": f"{app_name}_{check_time.isoformat()}",
            "AppName": app_name,
            "AppType": app_type,
            "IsAvailable": is_available,
            "CheckTime": check_time.isoformat()
        })
    except Exception as e:
        raise

def main(req: func.HttpRequest) -> func.HttpResponse:
    try:
        apps_info = []
        check_time = datetime.utcnow()

        for app in apps:
            app_name = app["name"]
            app_url = app["url"]
            app_type = app["type"]
            is_available = check_availability(app_url)
            apps_info.append({"app_name": app_name, "app_type": app_type, "is_available": is_available, "check_time": check_time.isoformat()})
            log_app_info(app_name, app_type, is_available, check_time)

        return func.HttpResponse(body=json.dumps(apps_info), status_code=200, mimetype="application/json")

    except Exception as e:
        return func.HttpResponse(body=json.dumps({"error": str(e)}), status_code=500, mimetype="application/json")
