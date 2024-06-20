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
from datetime import datetime
import decimal

endpoint = f"https://{os.getenv('AZURE_ACCOUNT_NAME')}.table.core.windows.net/"
logging.warning(f"endpoint: {endpoint}")
credential = AzureNamedKeyCredential(os.getenv("AZURE_ACCOUNT_NAME"), os.getenv("AZURE_ACCESS_KEY"))
logging.warning(f"AZURE_ACCOUNT_NAME: {len('AZURE_ACCOUNT_NAME')}\nAZURE_ACCESS_KEY: {len('AZURE_ACCESS_KEY')}")

ctx = decimal.Context()
ctx.prec = 10


def float_to_str(f):
    """Hack to get a better representation
    """
    return format(ctx.create_decimal(repr(f)), 'f')


class AzureCache:
    def cache_price(self, timestamp, coin, price, history_table):
        logging.warning(f"Caching price {timestamp} {coin} {price} into {history_table}")
        price = float_to_str(price)

        logging.warning("Setting up TableServiceClient")
        with TableServiceClient(
                endpoint=endpoint, credential=credential
        ) as table_service_client:
            # properties = table_service_client.get_service_properties()
            logging.warning("TableServiceClient set up")
            if history_table:
                logging.warning(f"Inserting into {history_table}")
                table_service_client.create_table_if_not_exists(history_table)

                e = TableEntity({
                    "PartitionKey": str(coin).title(),
                    "RowKey": str(uuid.uuid4()),
                    'PriceDate': datetime.fromtimestamp(timestamp).strftime('%Y-%m-%dT%H:%M:%S.%fZ'),
                    'price': price
                })
                table = table_service_client.get_table_client(history_table)
                table.create_entity(e)

            logging.warning("Inserting into currentprices")
            table_service_client.create_table_if_not_exists("currentprices")

            table = table_service_client.get_table_client("currentprices")
            # results = table_service_client.query_tables(f"PartitionKey eq '{coin}'")
            # logging.warning(results)

            c = TableEntity({
                "PartitionKey": str(coin).title(),
                "RowKey": "",
                'coin': coin.title(),
                'price': price
            })

            try:
                table.create_entity(c)
            except ResourceExistsError:
                e = table.get_entity(str(coin).title(), "")
                logging.warning(f"Entity already existed: {e}")
                table.delete_entity(str(coin).title(), "")
                table.create_entity(c)
