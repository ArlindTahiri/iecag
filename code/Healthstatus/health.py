import os
from flask import Flask, jsonify
from azure.identity import DefaultAzureCredential
from azure.mgmt.web import WebSiteManagementClient
from azure.data.tables import TableServiceClient
from datetime import datetime

app = Flask(__name__)

subscription_id = '409d7a11-b871-4827-8e32-43801826897e'
resource_group_name = "IeCAG-INFCA"
connection_string = "DefaultEndpointsProtocol=https;EndpointSuffix=core.windows.net;AccountName=iecagstorage;AccountKey=BRwtv3f9inw7XtCPxsXr2ALqcg7eBaMzs6HvsgkY6CC8DCF7tjJ/NEb3edd8nSxzKT3Ph6M6+EqN+AStoxIzYA==;BlobEndpoint=https://iecagstorage.blob.core.windows.net/;FileEndpoint=https://iecagstorage.file.core.windows.net/;QueueEndpoint=https://iecagstorage.queue.core.windows.net/;TableEndpoint=https://iecagstorage.table.core.windows.net/"

if not subscription_id or not resource_group_name or not connection_string:
    raise ValueError("Azure subscription ID, resource group name, or storage connection string is not set in the environment variables.")
else:
    print(f"Using subscription ID: {subscription_id}")
    print(f"Using resource group name: {resource_group_name}")
    print(f"Using connection string: {connection_string}")

# Azure Credential und Clients initialisieren
credential = DefaultAzureCredential()
web_client = WebSiteManagementClient(credential, subscription_id)
table_service_client = TableServiceClient.from_connection_string(conn_str=connection_string)
logs_table_client = table_service_client.get_table_client("logs")

def list_web_apps():
    try:
        return web_client.web_apps.list_by_resource_group(resource_group_name)
    except Exception as e:
        app.logger.error(f"Error listing web apps: {e}")
        raise

def list_functions():
    try:
        return web_client.web_apps.list_by_resource_group(resource_group_name)
    except Exception as e:
        app.logger.error(f"Error listing functions: {e}")
        raise

def check_availability(app):
    try:
        return app.state == "Running"
    except Exception as e:
        app.logger.error(f"Error checking availability for {app.name}: {e}")
        raise

def log_app_info(app_name, app_type, is_available, check_time):
    try:
        logs_table_client.create_entity({
            "PartitionKey": "log",
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

        # Check Web Apps
        for app in list_web_apps():
            app_name = app.name
            is_available = check_availability(app)
            apps_info.append({"app_name": app_name, "app_type": "Web App", "is_available": is_available, "check_time": check_time.isoformat()})
            log_app_info(app_name, "Web App", is_available, check_time)

        # Check Functions
        for app in list_functions():
            app_name = app.name
            is_available = check_availability(app)
            apps_info.append({"app_name": app_name, "app_type": "Function", "is_available": is_available, "check_time": check_time.isoformat()})
            log_app_info(app_name, "Function", is_available, check_time)

        return jsonify(apps_info)
    except Exception as e:
        app.logger.error(f"Error in check_all_apps endpoint: {e}")
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=True)