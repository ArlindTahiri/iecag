import os
from flask import Flask, jsonify
from azure.data.tables import TableServiceClient, TableClient
from azure.core.exceptions import ResourceNotFoundError, HttpResponseError
from datetime import datetime

app = Flask(__name__)

connection_string = os.getenv("AZURE_STORAGE_CONNECTION_STRING")

if not connection_string:
    raise ValueError("Azure Storage connection string is not set in the environment variables.")
else:
    print(f"Using connection string: {connection_string}")

try:
    table_service_client = TableServiceClient.from_connection_string(conn_str=connection_string)
    logs_table_client = table_service_client.get_table_client("logs")
except Exception as e:
    app.logger.error(f"Error creating TableServiceClient: {e}")
    raise

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
            tables_info.append({"table_name": table_name, "row_count": row_count, "check_time": check_time.isoformat()})
            log_table_info(table_name, row_count, check_time)  # Protokollieren der Tabelle und Zeilenanzahl
        return jsonify(tables_info)
    except Exception as e:
        app.logger.error(f"Error in check_all_tables endpoint: {e}")
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(debug=True)
