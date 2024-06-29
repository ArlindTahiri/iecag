import os
from sendgrid import SendGridAPIClient
from sendgrid.helpers.mail import Mail

def send_email():
    # Setze hier deinen SendGrid API-Schlüssel
    sendgrid_api_key = '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'
    
    # Setze die E-Mail-Parameter
    from_email = 'ladysophie96@gmx.de'
    to_email = 'sophie.gigl@web.de'
    subject = 'Hello!'
    content = 'Hey, hier bin ich'
    
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
        print(f'Status Code: {response.status_code}')
        print(f'Body: {response.body}')
        print(f'Headers: {response.headers}')
    except Exception as e:
        print(e.message)

# Aufruf der Funktion
send_email()
