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

    # Initialisiere den TableService
    table_service = TableService(account_name=storage_account_name, account_key=storage_account_key)

    try:
        # Hole alle Entities aus der Tabelle
        entities = table_service.query_entities(table_name)
        
        # Definiere den Schwellenwert
        threshold = 1  # Beispielwert

        for entity in entities:
            partition_key = entity.PartitionKey
            value = float(entity.price)  # Wert, der überprüft werden soll

            if value > threshold:
                # Bereite die E-Mail vor
                message = Mail(
                    from_email=from_email,
                    to_emails=to_email,#partition_key,  # E-Mail wird an den PartitionKey (E-Mail-Adresse) gesendet
                    subject='Wert überschritten',
                    plain_text_content=f'Der Wert {value} hat den Schwellenwert {threshold} überschritten.'
                )

                # Sende die E-Mail
                sg = SendGridAPIClient(sendgrid_api_key)
                response = sg.send(message)

                logging.info(f'Email sent to {partition_key}: {response.status_code}')

        return func.HttpResponse("Emails sent successfully if thresholds were exceeded.")

    except Exception as e:
        logging.error(f"An error occurred: {e}")
        return func.HttpResponse(f"An error occurred: {e}", status_code=500)
