<div align="center">
  <h1>IeCAG</h1>
  <img src="assets/Logo_IeCAG.png" alt="logo" width="400"/>
</div>

## Description
A small simulated cryptocurrency trading platform.

## Visuals
![Architectural Design](assets/IeCAG_for_light_theme_nerds.jpg "Architectural Design")

## Usage

## Support
[Yell at us](https://inf-git.fh-rosenheim.de/int-ca/sose2024/iecag/-/issues)

## Features
- User creation & managment
  - collection and managing of user information
  - including name, associated wallets, transactions
  - **Acceptance criteria**:
    - user must be able to create an account with a chosen name
    - user must be able to login with his credentials
    - user must be able to view his wallets and value

- Crypto Wallet simulation
  - contents and value are simulated
  - **Acceptance criteria**:
    - user must be able to create wallets

- Faux Trading
  - backend runs a simulation based on real-world prices.
  - users can buy and sell cryptocurrencies with fiat currency
  - **Acceptance criteria**:
    - user must able to trade with his wallets, (sell and buy cryptocurrencies)
    - simulation must check the impact on price and the wallet value

- Price History
  - historical data of cryptocurrencies are cached
  - **Acceptance criteria**:
    - retention policy of price data for 180 days (even if the project isn't this old)
    - user must be able to view a price history chart over at least the last 30 days (if data is available)

- Notifications to user
  - IFTTT (**I**f **T**his **T**hen **T**hat) interface for users to be notified about e.g. prices passing a threshold
  - **Acceptance criteria**:
    - user must be able to create rules like: if "$cryptocurrency" "$price" "is_lower_than" "1337"

## Roadmap
- [x] 2024-04-09: Teams and topic defined: A project with the team members was created on GitLab
- [x] 2024-05-07: Project description, use cases, features are defined and described and checked in on GitLab
- [ ] 2024-06-04: Interim project status - presentation and hardware procurement is completed
- [ ] 2024-07-09: Final presentation

## Authors and acknowledgment
- [Arlind Tahiri](https://inf-git.fh-rosenheim.de/studtahiar5766)
- [Lukas Ruf](https://inf-git.fh-rosenheim.de/studrufzlu7742)
- [Sophie Gigl](https://inf-git.fh-rosenheim.de/studgiglso3560)

## Project status
Active

