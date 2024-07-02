import os
import azure.functions as func
from azure.data.tables import TableServiceClient
from datetime import datetime

connection_string = os.getenv("AZURE_STORAGE_CONNECTION_STRING")
if not connection_string:
    raise ValueError("Azure Storage connection string is not set in the environment variables.")

table_service_client = TableServiceClient.from_connection_string(conn_str=connection_string)
logs_table_client = table_service_client.get_table_client("logs")

def count_table_rows(table_name):
    try:
        table_client = table_service_client.get_table_client(table_name)
        entities = table_client.list_entities()
        return sum(1 for _ in entities)
    except Exception as e:
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
        raise

def main(req: func.HttpRequest) -> func.HttpResponse:
    try:
        tables_info = []
        check_time = datetime.utcnow()

        for table in table_service_client.list_tables():
            table_name = table.name
            row_count = count_table_rows(table_name)
            tables_info.append({"table_name": table_name, "row_count": row_count, "is_available": True, "check_time": check_time.isoformat()})
            log_table_info(table_name, row_count, check_time)

        return func.HttpResponse(body=json.dumps(tables_info), status_code=200, mimetype="application/json")

    except Exception as e:
        return func.HttpResponse(body=json.dumps({"error": str(e)}), status_code=500, mimetype="application/json")
