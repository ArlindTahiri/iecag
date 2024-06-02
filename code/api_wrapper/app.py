import os
import logging
import sched
import threading
import time

from flask import Flask, jsonify, request, Blueprint
from flask_restx import Resource, Api, reqparse

# from flask_swagger_ui import get_swaggerui_blueprint

import manager

COINGECKO_TOKEN = os.environ.get('COINGECKO_TOKEN', '')

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


api.add_resource(Prices, '/price')

if __name__ == "__main__":
    scheduler = sched.scheduler(time.time, time.sleep)
    scheduler.enter(60, 1, manager.call_api, ("bitcoin",))
    app.run(host="0.0.0.0", port=8080)
