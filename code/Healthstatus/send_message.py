import os
import logging
from azure.cosmosdb.table.tableservice import TableService
from sendgrid import SendGridAPIClient
from sendgrid.helpers.mail import Mail

def send_email(to_email, content):
    sendgrid_api_key = os.getenv("SENDGRID_API_KEY")
    from_email = 'ladysophie96@gmx.de'
    subject = 'IECAG threshold'
    
    message = Mail(
        from_email=from_email,
        to_emails=to_email,
        subject=subject,
        plain_text_content=content
    )
    
    try:
        sg = SendGridAPIClient(sendgrid_api_key)
        response = sg.send(message)
        logging.info(f'Sending email to {to_email} with content: {content}')
        logging.info(f'Status Code: {response.status_code}')
        logging.info(f'Body: {response.body}')
        logging.info(f'Headers: {response.headers}')
    except Exception as e:
        logging.error(f"An error occurred while sending email: {e}")

def send_message():
    logging.info('Running local version to notify threshold.')

    storage_account_name = os.getenv('STORAGE_ACCOUNT_NAME')
    storage_account_key = os.getenv('STORAGE_ACCOUNT_KEY')
    table_name_notifications = os.getenv('TABLE_NAME_NOTIFICATIONS')
    table_name_currentprices = os.getenv('TABLE_NAME_CURRENTPRICES')

    if not all([storage_account_name, storage_account_key, table_name_notifications, table_name_currentprices]):
        logging.error('One or more required environment variables are missing.')
        return "One or more required environment variables are missing."

    table_service = TableService(account_name=storage_account_name, account_key=storage_account_key)

    output_messages = []

    try:
        entities_notifications = table_service.query_entities(table_name_notifications)
        entities_currentprices = table_service.query_entities(table_name_currentprices)
        
        values = {entity.PartitionKey: float(entity.price) for entity in entities_currentprices}

        for entity in entities_notifications:
            partition_key = entity.PartitionKey
            threshold = float(entity.price)
            trend = entity.trend
            coin = entity.coin
            to_email = partition_key

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
    # Hier wird die Funktion notify_threshold direkt aufgerufen, wenn das Skript lokal ausgeführt wird.
    result = send_message()
    print(result)
