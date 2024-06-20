"""
import pyodbc

# https://learn.microsoft.com/de-de/azure/azure-sql/database/connect-query-python?view=azuresql

server = '<server>.database.windows.net'
database = '<database>'
username = '<username>'
password = '{<password>}'
driver= '{ODBC Driver 17 for SQL Server}'

with pyodbc.connect(f'DRIVER={driver};SERVER=tcp:{server};PORT=1433;DATABASE={database};UID={username};PWD={password}') as conn:
    with conn.cursor() as cursor:
        cursor.execute("SELECT TOP 3 name, collation_name FROM sys.databases")
        row = cursor.fetchone()
        while row:
            print(str(row[0]) + " " + str(row[1]))
            row = cursor.fetchone()
"""
import os
import uuid

from azure.core import MatchConditions
from azure.data.tables import TableServiceClient, TableEntity, TableClient, UpdateMode
from azure.core.credentials import AzureNamedKeyCredential
from azure.core.exceptions import HttpResponseError, ResourceExistsError

import logging

endpoint = f"https://{os.getenv('AZURE_ACCOUNT_NAME')}.table.core.windows.net/"
credential = AzureNamedKeyCredential(os.getenv("AZURE_ACCOUNT_NAME"), os.getenv("AZURE_ACCESS_KEY"))
table_name = os.getenv("AZURE_TABLE_NAME")

class AzureCache:
    def cache_price(self, timestamp, coin, price):
        logging.warning(f"Caching price {timestamp} {coin} {price}")

        with TableServiceClient(
                endpoint=endpoint, credential=credential
        ) as table_service_client:
            #properties = table_service_client.get_service_properties()

            table_service_client.create_table_if_not_exists("pricehistory")

            e = TableEntity({
                "PartitionKey": str(uuid.uuid4()),
                "RowKey": coin,
                'timestamp': timestamp,
                'coin': coin,
                'price': price
            }
            )
            table = table_service_client.get_table_client("pricehistory")
            table.create_entity(e)

            table_service_client.create_table_if_not_exists("currentprices")

            table = table_service_client.get_table_client("currentprices")
            #results = table_service_client.query_tables(f"PartitionKey eq '{coin}'")
            #logging.warning(results)

            c = TableEntity({
                "PartitionKey": str(coin),
                "RowKey": "",
                'coin': coin,
                'price': price
            })

            try:
                table.create_entity(c)
            except ResourceExistsError:
                e = table.get_entity(coin, "")
                logging.warning(e)
                table.delete_entity(coin, "")
                table.create_entity(c)