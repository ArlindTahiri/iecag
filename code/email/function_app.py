import logging
import os
import azure.functions as func
from azure.cosmosdb.table.tableservice import TableService


@app.route(route="as_email", auth_level=func.AuthLevel.FUNCTION)
def perEmail(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    # Azure Table Storage Verbindungsinformationen und Tabellenname
    storage_account_name = os.getenv('STORAGE_ACCOUNT_NAME')
    storage_account_key = os.getenv('STORAGE_ACCOUNT_KEY')
    table_name = os.getenv('TABLE_NAME')

    # Initialisiere den TableService
    table_service = TableService(account_name=storage_account_name, account_key=storage_account_key)

    output_messages = []

    try:
        # Hole alle Entities aus der Tabelle
        entities = table_service.query_entities(table_name)
        
        # Definiere den Schwellenwert
        threshold = 1  # Beispielwert

        for entity in entities:
            partition_key = entity.PartitionKey
            value = float(entity.price)  # Wert, der überprüft werden soll

            if value > threshold:
                message = f'Der Wert {value} hat den Schwellenwert {threshold} überschritten für {partition_key}.'
                output_messages.append(message)
                logging.info(message)

        # Rückgabe aller Nachrichten als HTTP-Antwort
        if output_messages:
            return func.HttpResponse("\n".join(output_messages))
        else:
            return func.HttpResponse("No values exceeded the threshold.")

    except Exception as e:
        logging.error(f"An error occurred: {e}")
        return func.HttpResponse(f"An error occurred: {e}", status_code=500)
