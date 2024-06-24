# using SendGrid's Python Library
# https://github.com/sendgrid/sendgrid-python
import os
from sendgrid import SendGridAPIClient
from sendgrid.helpers.mail import Mail

message = Mail(
    from_email='sophie.gigl_1@stud.th-rosenheim.de',
    to_emails='sophie.gigl_1@stud.th-rosenheim.de',
    subject='Sending with Twilio SendGrid is Fun',
    html_content='<strong>and easy to do anywhere, even with Python</strong>')
try:
    sg = SendGridAPIClient(os.environ.get('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'))
    response = sg.send(message)
    print(response.status_code)
    print(response.body)
    print(response.headers)
except Exception as e:
    print(e.message)