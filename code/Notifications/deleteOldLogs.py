import os
from datetime import datetime, timedelta
import azure.functions as func
from azure.data.tables import TableServiceClient

def delete_old_logs(table_service_client):
    try:
        two_hours_ago = datetime.utcnow() - timedelta(hours=2)
        logs_table_client = table_service_client.get_table_client("logs")
        
        old_entities = []
        for entity in logs_table_client.list_entities():
            check_time = datetime.fromisoformat(entity["CheckTime"])
            if check_time < two_hours_ago:
                old_entities.append(entity)
        
        for entity in old_entities:
            logs_table_client.delete_entity(entity["PartitionKey"], entity["RowKey"])
        
        return len(old_entities)
    
    except Exception as e:
        return str(e)

def main(req: func.HttpRequest) -> func.HttpResponse:
    connection_string = os.getenv("AZURE_STORAGE_CONNECTION_STRING")
    if not connection_string:
        return func.HttpResponse("Azure Storage connection string is not set in the environment variables.", status_code=500)
    
    try:
        table_service_client = TableServiceClient.from_connection_string(conn_str=connection_string)
        deleted_count = delete_old_logs(table_service_client)
        
        return func.HttpResponse(f"Deleted {deleted_count} old log entries.", status_code=200)
    
    except Exception as e:
        return func.HttpResponse(f"Error deleting old logs: {str(e)}", status_code=500)
