class Wrapper:
    rate_limited = False
    token = ""

    def __init__(self, token):
        self.token = token

    # only prices so far
    def get_price(self, currency, vs_currency):
        pass

    def test(self):
        pass
