import os
import requests
from flask import Flask, jsonify
from azure.data.tables import TableServiceClient
from datetime import datetime

app = Flask(__name__)

# Umgebungsvariablen abrufen
connection_string = "DefaultEndpointsProtocol=https;EndpointSuffix=core.windows.net;AccountName=iecagstorage;AccountKey=BRwtv3f9inw7XtCPxsXr2ALqcg7eBaMzs6HvsgkY6CC8DCF7tjJ/NEb3edd8nSxzKT3Ph6M6+EqN+AStoxIzYA==;BlobEndpoint=https://iecagstorage.blob.core.windows.net/;FileEndpoint=https://iecagstorage.file.core.windows.net/;QueueEndpoint=https://iecagstorage.queue.core.windows.net/;TableEndpoint=https://iecagstorage.table.core.windows.net/"
#connection_string = os.getenv("AZURE_STORAGE_CONNECTION_STRING")

if not connection_string:
    raise ValueError("Azure storage connection string is not set in the environment variables.")
else:
    print(f"Using connection string: {connection_string}")

# URLs der Web Apps und Functions
apps = [
    {"name": "APIWrapper", "url": "https://iecagapiwrapper.azurewebsites.net/", "type": "Web App"},
    {"name": "iecag", "url": "https://iecag.azurewebsites.net/", "type": "Web App"},
    # Fügen Sie hier weitere Apps und Funktionen hinzu
    #{"name": "MyFunctionApp1", "url": "https://myfunctionapp1.azurewebsites.net/api/healthcheck", "type": "Function"},
]

# Azure Table Service Client initialisieren
try:
    table_service_client = TableServiceClient.from_connection_string(conn_str=connection_string)
    logs_table_client = table_service_client.get_table_client("logs")
except Exception as e:
    app.logger.error(f"Error creating TableServiceClient: {e}")
    raise

def check_availability(url):
    try:
        response = requests.get(url)
        return response.status_code == 200
    except Exception as e:
        app.logger.error(f"Error checking availability for {url}: {e}")
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
        app.logger.error(f"Error logging info for app {app_name}: {e}")
        raise

@app.route('/check_all_apps', methods=['GET'])
def check_all_apps():
    try:
        apps_info = []
        check_time = datetime.utcnow()

        # Überprüfen Sie die Verfügbarkeit der Apps
        for app in apps:
            app_name = app["name"]
            app_url = app["url"]
            app_type = app["type"]
            is_available = check_availability(app_url)
            apps_info.append({"app_name": app_name, "app_type": app_type, "is_available": is_available, "check_time": check_time.isoformat()})
            log_app_info(app_name, app_type, is_available, check_time)

        return jsonify(apps_info)
    except Exception as e:
        app.logger.error(f"Error in check_all_apps endpoint: {e}")
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=True)
