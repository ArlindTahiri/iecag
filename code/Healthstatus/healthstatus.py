import os
import requests
from flask import Flask, jsonify
from azure.data.tables import TableServiceClient, TableTransactionError
from datetime import datetime, timedelta

app = Flask(__name__)

# Umgebungsvariablen abrufen
connection_string = os.getenv("AZURE_STORAGE_CONNECTION_STRING", "DefaultEndpointsProtocol=https;EndpointSuffix=core.windows.net;AccountName=iecagstorage;AccountKey=BRwtv3f9inw7XtCPxsXr2ALqcg7eBaMzs6HvsgkY6CC8DCF7tjJ/NEb3edd8nSxzKT3Ph6M6+EqN+AStoxIzYA==;BlobEndpoint=https://iecagstorage.blob.core.windows.net/;FileEndpoint=https://iecagstorage.file.core.windows.net/;QueueEndpoint=https://iecagstorage.queue.core.windows.net/;TableEndpoint=https://iecagstorage.table.core.windows.net/")

if not connection_string:
    raise ValueError("Azure Storage connection string is not set in the environment variables.")
else:
    print(f"Using connection string: {connection_string}")

# Azure Table Service Client initialisieren
try:
    table_service_client = TableServiceClient.from_connection_string(conn_str=connection_string)
    logs_table_client = table_service_client.get_table_client("logs")
except Exception as e:
    app.logger.error(f"Error creating TableServiceClient: {e}")
    raise

# URLs der Web Apps und Functions
apps = [
    {"name": "APIWrapper", "url": "https://iecagapiwrapper.azurewebsites.net/", "type": "Web App"},
    {"name": "iecag", "url": "https://iecag.azurewebsites.net/", "type": "Web App"},
    # Fügen Sie hier weitere Apps und Funktionen hinzu
    # {"name": "MyFunctionApp1", "url": "https://myfunctionapp1.azurewebsites.net/api/healthcheck", "type": "Function"},
]

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

def list_tables():
    try:
        return table_service_client.list_tables()
    except Exception as e:
        app.logger.error(f"Error listing tables: {e}")
        raise

def count_table_rows(table_name):
    try:
        table_client = table_service_client.get_table_client(table_name)
        entities = table_client.list_entities()
        return sum(1 for _ in entities)
    except Exception as e:
        app.logger.error(f"Error counting rows in table {table_name}: {e}")
        raise

def log_table_info(table_name, row_count, check_time):
    try:
        logs_table_client.create_entity({
            "PartitionKey": "log_table",
            "RowKey": f"{table_name}_{check_time.isoformat()}",
            "TableName": table_name,
            "RowCount": row_count,
            "IsAvailable": True,
            "CheckTime": check_time.isoformat()
        })
    except Exception as e:
        app.logger.error(f"Error logging info for table {table_name}: {e}")
        raise

@app.route('/check_all_tables', methods=['GET'])
def check_all_tables():
    try:
        tables_info = []
        check_time = datetime.utcnow()
        for table in list_tables():
            table_name = table.name
            row_count = count_table_rows(table_name)
            tables_info.append({"table_name": table_name, "row_count": row_count, "is_available": True, "check_time": check_time.isoformat()})
            log_table_info(table_name, row_count, check_time)
        return jsonify(tables_info)
    except Exception as e:
        app.logger.error(f"Error in check_all_tables endpoint: {e}")
        return jsonify({"error": str(e)}), 500

@app.route('/check_all', methods=['GET'])
def check_all():
    try:
        all_info = {"apps": [], "tables": []}
        check_time = datetime.utcnow()

        # Überprüfen Sie die Verfügbarkeit der Apps
        for app in apps:
            app_name = app["name"]
            app_url = app["url"]
            app_type = app["type"]
            is_available = check_availability(app_url)
            all_info["apps"].append({"app_name": app_name, "app_type": app_type, "is_available": is_available, "check_time": check_time.isoformat()})
            log_app_info(app_name, app_type, is_available, check_time)

        # Überprüfen Sie die Tabellen
        for table in list_tables():
            table_name = table.name
            try:
                row_count = count_table_rows(table_name)
                is_available = True
            except Exception as e:
                app.logger.error(f"Error checking table {table_name}: {e}")
                row_count = 0
                is_available = False
            all_info["tables"].append({"table_name": table_name, "row_count": row_count, "is_available": is_available, "check_time": check_time.isoformat()})
            log_table_info(table_name, row_count, check_time)

        return jsonify(all_info)
    except Exception as e:
        app.logger.error(f"Error in check_all endpoint: {e}")
        return jsonify({"error": str(e)}), 500

def delete_old_logs():
    try:
        two_hours_ago = datetime.utcnow() - timedelta(hours=2)
        old_entities = []
        for entity in logs_table_client.list_entities():
            check_time = datetime.fromisoformat(entity["CheckTime"])
            if check_time < two_hours_ago:
                old_entities.append(entity)
        
        for entity in old_entities:
            logs_table_client.delete_entity(entity["PartitionKey"], entity["RowKey"])
        return len(old_entities)
    except Exception as e:
        app.logger.error(f"Error deleting old logs: {e}")
        raise

@app.route('/delete_old_logs', methods=['DELETE'])
def delete_old_logs_endpoint():
    try:
        deleted_count = delete_old_logs()
        return jsonify({"deleted_count": deleted_count})
    except Exception as e:
        app.logger.error(f"Error in delete_old_logs endpoint: {e}")
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=True)
