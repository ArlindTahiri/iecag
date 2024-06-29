import os
from azure.cosmosdb.table.tableservice import TableService
import logging

def access_azure_table():
    # Azure Table Storage Verbindungsinformationen und Tabellenname aus Umgebungsvariablen
    storage_account_name = 'iecagstorage'
    storage_account_key = 'BRwtv3f9inw7XtCPxsXr2ALqcg7eBaMzs6HvsgkY6CC8DCF7tjJ/NEb3edd8nSxzKT3Ph6M6+EqN+AStoxIzYA=='
    table_name = 'notifications'

    if not all([storage_account_name, storage_account_key, table_name]):
        logging.error('One or more required environment variables are missing.')
        return "One or more required environment variables are missing."

    # Initialisiere den TableService
    table_service = TableService(account_name=storage_account_name, account_key=storage_account_key)

    output_messages = []

    try:
        # Hole alle Entities aus der Tabelle
        entities = table_service.query_entities(table_name)
        
        # Definiere den Schwellenwert
        threshold = 1000000000  # Beispielwert

        for entity in entities:
            partition_key = entity.PartitionKey
            value = float(entity.price)  # Wert, der überprüft werden soll
            trend = entity.trend  # Trend-Wert auslesen
            coin = entity.coin  # Coin-Wert auslesen

            if trend == "above" and value > threshold:
                message = f'Der Wert {value} der Kryptowährung {coin} hat den Schwellenwert {threshold} überschritten für {partition_key}.'
                output_messages.append(message)
                logging.info(message)
            elif trend == "below" and value < threshold:
                message = f'Der Wert {value} der Kryptowährung {coin} hat den Schwellenwert {threshold} unterschritten für {partition_key}.'
                output_messages.append(message)
                logging.info(message)

        # Rückgabe aller Nachrichten
        if output_messages:
            return "\n".join(output_messages)
        else:
            return "No values exceeded the threshold."

    except Exception as e:
        logging.error(f"An error occurred: {e}")
        return f"An error occurred: {e}"

if __name__ == "__main__":
    result = access_azure_table()
    print(result)
