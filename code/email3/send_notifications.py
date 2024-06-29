import os
from azure.cosmosdb.table.tableservice import TableService
import logging
from sendgrid import SendGridAPIClient
from sendgrid.helpers.mail import Mail

def send_email(to_email, content):
    # Setze hier deinen SendGrid API-Schlüssel
    sendgrid_api_key = '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'
    
    # Setze die E-Mail-Parameter
    from_email = 'ladysophie96@gmx.de'
    subject = 'IECAG threshold'
    
    # Erstelle die E-Mail
    message = Mail(
        from_email=from_email,
        to_emails=to_email,
        subject=subject,
        plain_text_content=content
    )
    
    try:
        # Sende die E-Mail
        sg = SendGridAPIClient(sendgrid_api_key)
        response = sg.send(message)
        print(f'Sending email to {to_email} with content: {content}')
        print(f'Status Code: {response.status_code}')
        print(f'Body: {response.body}')
        print(f'Headers: {response.headers}')
    except Exception as e:
        print(e.message)

def access_azure_table():
    # Azure Table Storage Verbindungsinformationen und Tabellenname aus Umgebungsvariablen
    storage_account_name = 'iecagstorage'
    storage_account_key = 'BRwtv3f9inw7XtCPxsXr2ALqcg7eBaMzs6HvsgkY6CC8DCF7tjJ/NEb3edd8nSxzKT3Ph6M6+EqN+AStoxIzYA=='
    table_name_notifications = 'notifications'
    table_name_currentprices = 'currentprices'

    if not all([storage_account_name, storage_account_key, table_name_notifications, table_name_currentprices]):
        logging.error('One or more required environment variables are missing.')
        return "One or more required environment variables are missing."

    # Initialisiere den TableService
    table_service = TableService(account_name=storage_account_name, account_key=storage_account_key)

    output_messages = []

    try:
        # Hole alle Entities aus der notifications Tabelle
        entities_notifications = table_service.query_entities(table_name_notifications)
        
        # Hole alle Entities aus der currentprices Tabelle
        entities_currentprices = table_service.query_entities(table_name_currentprices)
        
        # Erstelle ein Dictionary, um aktuelle Preise basierend auf PartitionKey (coin) zu speichern
        values = {entity.PartitionKey: float(entity.price) for entity in entities_currentprices}

        for entity in entities_notifications:
            partition_key = entity.PartitionKey
            threshold = float(entity.price)  # Schwellenwert, der überprüft werden soll
            trend = entity.trend  # Trend-Wert auslesen
            coin = entity.coin  # Coin-Wert auslesen
            to_email = partition_key  # E-Mail-Adresse aus PartitionKey

            # Vergleiche den Coin mit dem PartitionKey in der currentprices Tabelle
            if coin in values:
                value = values[coin]

                if trend == "above" and threshold < value:
                    message = f'Der Wert {value} der Kryptowährung {coin} hat den Schwellenwert {threshold} überschritten für {partition_key}.'
                    output_messages.append(message)
                    logging.info(message)
                    send_email(to_email, message)
                elif trend == "below" and threshold > value:
                    message = f'Der Wert {value} der Kryptowährung {coin} hat den Schwellenwert {threshold} unterschritten für {partition_key}.'
                    output_messages.append(message)
                    logging.info(message)
                    send_email(to_email, message)

        # Rückgabe aller Nachrichten
        if output_messages:
            return "Values exceeded the threshold."
        else:
            send_email('ladysophie96@gmx.de', "No values exceeded the threshold.")
            return "No values exceeded the threshold."

    except Exception as e:
        logging.error(f"An error occurred: {e}")
        send_email('ladysophie96@gmx.de', f"An error occurred: {e}")
        return f"An error occurred: {e}"

if __name__ == "__main__":
    result = access_azure_table()
    print(result)
