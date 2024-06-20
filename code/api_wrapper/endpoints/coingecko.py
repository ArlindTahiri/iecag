import logging
import os
import urllib3

from endpoints.wrapper import Wrapper

API_ENDPOINT = "https://api.coingecko.com/api/v3"
POOL_MANAGER = urllib3.PoolManager()


class CoinGecko(Wrapper):
    def __init__(self, token):
        self.token = token

    def get_price(self, currency, vs_currency):
        url = f"{API_ENDPOINT}/simple/price?ids={currency}&vs_currencies={vs_currency}"
        logging.warning(url)

        response = POOL_MANAGER.request("GET", url, headers={"accept": "application/json",
                                                             "x_cg_demo_api_key": self.token
                                                             })
        serial_resp = response.json()
        logging.warning(serial_resp)
        try:
            return serial_resp[currency][vs_currency]
        except KeyError as e:
            logging.error(e)

    def test(self):
        pass
