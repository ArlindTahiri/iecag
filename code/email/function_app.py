import logging
import os
import azure.functions as func
from azure.cosmosdb.table.tableservice import TableService
from azure.cosmosdb.table.models import Entity
from sendgrid import SendGridAPIClient
from sendgrid.helpers.mail import Mail

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)

@app.route(route="perEmail")
def perEmail(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    # Werte für SendGrid API und E-Mail Adressen aus den Umgebungsvariablen laden
    sendgrid_api_key = os.getenv('SENDGRID_API_KEY')
    from_email = os.getenv('FROM_EMAIL')
    to_email = os.getenv('TO_EMAIL')

    # Azure Table Storage Verbindungsinformationen und Tabellenname
    storage_account_name = os.getenv('STORAGE_ACCOUNT_NAME')
    storage_account_key = os.getenv('STORAGE_ACCOUNT_KEY')
    table_name = os.getenv('TABLE_NAME')
    partition_key = os.getenv('PARTITION_KEY')
    row_key = os.getenv('ROW_KEY')

    # Initialisiere den TableService
    table_service = TableService(account_name=storage_account_name, account_key=storage_account_key)

    try:
        # Hole das Entity aus der Tabelle
        entity = table_service.get_entity(table_name, partition_key, row_key)
        value = entity.Value  # Wert, der überprüft werden soll

        # Definiere den Schwellenwert
        threshold = 1  # Beispielwert

        if value > threshold:
            # Bereite die E-Mail vor
            message = Mail(
                from_email=from_email,
                to_emails=to_email,
                subject='Wert überschritten',
                plain_text_content=f'Der Wert {value} hat den Schwellenwert {threshold} überschritten.'
            )

            # Sende die E-Mail
            sg = SendGridAPIClient(sendgrid_api_key)
            response = sg.send(message)

            logging.info(f'Email sent: {response.status_code}')

            return func.HttpResponse(f"Email sent successfully. The value {value} exceeded the threshold {threshold}.")
        else:
            return func.HttpResponse(f"The value {value} did not exceed the threshold {threshold}. No email sent.")

    except Exception as e:
        logging.error(f"An error occurred: {e}")
        return func.HttpResponse(f"An error occurred: {e}", status_code=500)

