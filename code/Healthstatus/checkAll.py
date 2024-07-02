import os
import json
from datetime import datetime
import azure.functions as func
from azure.data.tables import TableServiceClient

def check_availability(url):
    try:
        response = requests.get(url)
        return response.status_code == 200
    except Exception as e:
        return False

def log_app_info(logs_table_client, app_name, app_type, is_available, check_time):
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

def count_table_rows(table_client):
    try:
        entities = table_client.list_entities()
        return sum(1 for _ in entities)
    except Exception as e:
        raise

def log_table_info(logs_table_client, table_name, row_count, check_time):
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
        raise

def delete_old_logs(logs_table_client):
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
        raise

def check_all(req: func.HttpRequest) -> func.HttpResponse:
    try:
        connection_string = os.getenv("AZURE_STORAGE_CONNECTION_STRING")
        if not connection_string:
            return func.HttpResponse("Azure Storage connection string is not set in the environment variables.", status_code=500)
        
        table_service_client = TableServiceClient.from_connection_string(conn_str=connection_string)
        logs_table_client = table_service_client.get_table_client("logs")
        
        apps = [
            {"name": "APIWrapper", "url": "https://iecagapiwrapper.azurewebsites.net/", "type": "Web App"},
            {"name": "iecag", "url": "https://iecag.azurewebsites.net/", "type": "Web App"},
            # Add more apps here
        ]
        
        all_info = {"apps": [], "tables": []}
        check_time = datetime.utcnow()

        # Check app availability
        for app in apps:
            app_name = app["name"]
            app_url = app["url"]
            app_type = app["type"]
            is_available = check_availability(app_url)
            all_info["apps"].append({"app_name": app_name, "app_type": app_type, "is_available": is_available, "check_time": check_time.isoformat()})
            log_app_info(logs_table_client, app_name, app_type, is_available, check_time)

        # Check table row counts
        for table in table_service_client.list_tables():
            table_client = table_service_client.get_table_client(table.name)
            try:
                row_count = count_table_rows(table_client)
                is_available = True
            except Exception as e:
                row_count = 0
                is_available = False
                app.logger.error(f"Error checking table {table.name}: {e}")
            
            all_info["tables"].append({"table_name": table.name, "row_count": row_count, "is_available": is_available, "check_time": check_time.isoformat()})
            log_table_info(logs_table_client, table.name, row_count, check_time)
        
        # Delete old logs
        deleted_count = delete_old_logs(logs_table_client)

        return func.HttpResponse(json.dumps(all_info), status_code=200, mimetype="application/json")
    
    except Exception as e:
        return func.HttpResponse(f"Error in check_all endpoint: {str(e)}", status_code=500)
