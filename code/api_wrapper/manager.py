import time

from endpoints.coingecko import CoinGecko
from cache.azure import AzureCache

class Manager:
    def __init__(self, coingecko_token=""):
        self.coingecko_token = coingecko_token

        self.cache = AzureCache()
        self.wrapper = CoinGecko()

    def get_price(self, curr, vs_curr):
        # call the cache
        price = self.cache.get_price(curr, vs_curr)
        if price:
            return price
        else:
            return None

    def call_api(self, curr):
        price = self.wrapper.get_price(curr, "eur")
        self.cache.cache_price(time.time(), curr, price)
