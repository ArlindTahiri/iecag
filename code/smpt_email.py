import azure.functions as func
import logging
import os
import smtplib
from email.mime.text import MIMEText

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)

SMTP_SERVER = os.getenv('SMTP_SERVER')
SMTP_PORT = int(os.getenv('SMTP_PORT', 587))
SMTP_USERNAME = os.getenv('SMTP_USERNAME')
SMTP_PASSWORD = os.getenv('SMTP_PASSWORD')

def send_email(subject, body, to_email):
    from_email = SMTP_USERNAME
    msg = MIMEText(body)
    msg['Subject'] = subject
    msg['From'] = from_email
    msg['To'] = to_email

    with smtplib.SMTP(SMTP_SERVER, SMTP_PORT) as server:
        server.starttls()
        server.login(SMTP_USERNAME, SMTP_PASSWORD)
        server.sendmail(from_email, [to_email], msg.as_string())

@app.route(route="valuetrigger")
def valuetrigger(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    name = req.params.get('name')
    if not name:
        try:
            req_body = req.get_json()
        except ValueError:
            req_body = None
        name = req_body.get('name') if req_body else None

    if name:
        return func.HttpResponse(f"Hello, {name}. This HTTP triggered function executed successfully.")
    else:
        return func.HttpResponse(
             "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.",
             status_code=200
        )

@app.route(route="notify_email", auth_level=func.AuthLevel.FUNCTION)
def notify_email(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    try:
        req_body = req.get_json()
    except ValueError:
        return func.HttpResponse(
             "Invalid JSON in the request body.",
             status_code=400
        )

    price = req_body.get('price')
    below_limit = req_body.get('below_limit')
    above_limit = req_body.get('above_limit')
    email = req_body.get('email')

    if not price or not email:
        return func.HttpResponse(
             "Request must include 'price' and 'email'.",
             status_code=400
        )

    if below_limit is not None and price < below_limit:
        subject = "Price Alert: Below Limit"
        body = f"The price has dropped below your limit of {below_limit}. Current price: {price}"
        send_email(subject, body, email)
        return func.HttpResponse(f"Email sent for price below limit. Current price: {price}")

    if above_limit is not None and price > above_limit:
        subject = "Price Alert: Above Limit"
        body = f"The price has exceeded your limit of {above_limit}. Current price: {price}"
        send_email(subject, body, email)
        return func.HttpResponse(f"Email sent for price above limit. Current price: {price}")

    return func.HttpResponse(
         "No email sent. Ensure 'below_limit' or 'above_limit' is set correctly.",
         status_code=200
    )
