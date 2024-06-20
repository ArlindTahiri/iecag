import os
import logging
import sched
import threading
import time

import schedule
from flask import Flask, jsonify, request, Blueprint
from flask_restx import Resource, Api, reqparse

# from flask_swagger_ui import get_swaggerui_blueprint

import manager

COINGECKO_TOKEN = os.environ.get('COINGECKO_TOKEN', '')
FETCH_THREAD = None

app = Flask(__name__)
api = Api(app)

blueprint = Blueprint('api', __name__, url_prefix='/api')
app.register_blueprint(blueprint, doc='/doc/')  # '/api/doc/'

manager = manager.Manager(coingecko_token=COINGECKO_TOKEN)


@api.route('/price', endpoint='price')
@api.doc(params={'currency': 'Currency', 'vs_currency': 'Currency compared against'})
class Prices(Resource):
    def get(self):
        parser = reqparse.RequestParser()
        parser.add_argument('currency', type=str)
        parser.add_argument('vs_currency', type=str)
        p = parser.parse_args()

        return {'price': manager.get_price(p.get("currency"), p.get("vs_currency"))}


@api.route('/health', endpoint='health')
@api.doc(params={})
class Health(Resource):
    def get(self):
        try:
            thread_alive = FETCH_THREAD.is_alive()
        except Exception as e:
            logging.error(e)
            thread_alive = None
        return {"healthy": True, "thread": thread_alive}


api.add_resource(Prices, '/price')
api.add_resource(Health, '/health')


def get_prices(table_name=""):
    try:
        logging.warning(f"Running scheduled task for {table_name!r}")
        timestamp = time.time()
        manager.call_api("bitcoin", table_name, timestamp)
        manager.call_api("ethereum", table_name, timestamp)
        manager.call_api("cronos", table_name, timestamp)
    except Exception as e:
        logging.error(e)


def start_fetch():
    get_prices()
    schedule.every(2).minutes.do(get_prices)
    schedule.every(30).minutes.do(get_prices, table_name="pricehistory7days")
    schedule.every(2).hours.do(get_prices, table_name="pricehistory30days")
    schedule.every(12).hours.do(get_prices, table_name="pricehistory180days")
    while True:
        try:
            schedule.run_pending()
        except Exception as e:
            logging.error(e)
        time.sleep(1)


FETCH_THREAD = threading.Thread(target=lambda: start_fetch(), daemon=True)
FETCH_THREAD.start()

if __name__ == "__main__":
    threading.Thread(target=lambda: app.run(host="0.0.0.0", port=8080, debug=False), daemon=True).start()
