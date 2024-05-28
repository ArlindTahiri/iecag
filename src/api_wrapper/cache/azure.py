import os

from azure.data.tables import TableServiceClient, TableEntity
from azure.core.credentials import AzureNamedKeyCredential

endpoint = f"https://{os.getenv('AZURE_ACCOUNT_NAME')}.table.core.windows.net/"
credential = AzureNamedKeyCredential(os.getenv("AZURE_ACCOUNT_NAME"), os.getenv("AZURE_ACCESS_KEY"))
table_name = os.getenv("AZURE_TABLE_NAME")


class AzureCache:
    def cache_price(self, timestamp, coin, price):
        with TableServiceClient(
                endpoint=endpoint, credential=credential
        ) as table_service_client:
            properties = table_service_client.get_service_properties()

            e = TableEntity({
                'timestamp': timestamp,
                'coin': coin,
                'price': price}
            )

            table = table_service_client.get_table_client(table_name)
            table.create_entity(e)
