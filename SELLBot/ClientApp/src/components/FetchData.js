import React, { Component } from 'react';

export class FetchData extends Component {
    static displayName = FetchData.name;

    constructor(props) {
        super(props);
        this.state = { coins: [], coinsRender: [], loading: true };
        this.onValueChange = this.onValueChange.bind(this);
    }

    onValueChange(event) {
        let coins = this.state.coins;
        let coinsRender = coins[0];

        this.setState({
            exchange: event.target.value,
            coinsRender: coinsRender,
        });
    }

    componentDidMount() {
        this.interval = setInterval(() => this.populateCoinsData(), 20000);
    }

    componentWillUnmount() {
        clearInterval(this.interval);
    }

    static renderCoinsTable(coinsRender, exchange) {

        function sendOrder(val1, val2, val3, val4, val5, val6, exchange, percentage) {
            let obj = JSON.stringify({ seller: val1, buyer: val2, price: val3, quantity: val4, ask: val5, lastPrice: val6, exchange: exchange, percentage: percentage });
            const requestOptions = {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: obj
            };
            fetch('arbitrage', requestOptions)
                .then(response => response.json())
                .then(response => {
                    console.log(response.message);

                    alert(response.message);
                });
        }

        function onValChange(e, current) {
            e.quantity = current.currentTarget.value;
        };

        return (
            <div>
                <br />
                <div className='rowC'>
                    <h4 className='rowC-elementA'>BTC/USDT: <br /> ${coinsRender.btc}</h4>
                    <h4 className='rowC-elementA'>ETH/USDT: <br /> ${coinsRender.eth}</h4>
                    <h4 className='rowC-elementA'>Time: <br />{coinsRender.timeToFinish}</h4><br />
                </div>
                <div className='rowC'>
                    <h4 className='rowC-elementA'>Difference: <br /> {coinsRender.difference}</h4>
                    <h4 className='rowC-elementA'>Last price Difference: <br /> {coinsRender.lastPriceDifference}</h4>
                    <h4 className='rowC-elementA'>Directional Ratio: <br />{coinsRender.directionalRatio}</h4><br />
                    <h4 className='rowC-elementA'>Can Operate: <br /> <div className="dot" style={{ backgroundColor: coinsRender.candleCanOperate == true ? '#D72C3A' : '#00740B' }}></div></h4><br />
                </div>
                <br />
                <table style={{ textAlign: 'center' }} className='table table-striped' aria-labelledby="tabelLabel">
                    <thead>
                        <tr>
                            <th className="tableTitle">Coin Name</th>
                            <th className="tableTitle">Arbitrage Percentage</th>
                            <th className="tableTitle">State</th>
                            <th className="tableTitle">Quantity</th>
                            <th className="tableTitle">Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        {coinsRender?.coins?.map(
                            coin =>
                                <tr key={coin.symbol}>
                                    <td className='tableData'>{coin.symbol}</td>
                                    <td className='tableData'>{coin.percentage}</td>

                                    <td>
                                        <div className="dot" style={{ backgroundColor: coin.hasOpendOrders == true ? '#D72C3A' : '#00740B' }}>
                                        </div>
                                    </td>
                                    <td>
                                        <input style={{ textAlign: 'center' }} type="text" defaultValue={coin.quantity} onChange={(event) => onValChange(coin, event)} />
                                    </td>
                                    <td>
                                        <button
                                            className="btn btn-primary"
                                            onClick={(event) => sendOrder(coin.usdt, coin.btc, coin.price, coin.quantity, coin.firstQuantity, coin.lastPrice, exchange, coin.percentage)}>
                                            Send Order
                                        </button>
                                    </td>
                                </tr>
                        )}
                    </tbody>
                </table>
            </div>
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em><div style={{ color: '#FFFFFF' }}>Loading...</div></em></p>
            : FetchData.renderCoinsTable(this.state.coinsRender, this.state.exchange, this.onValueChange, this.componentDidMount);

        return (
            <div>
                {contents}
            </div>
        );
    }

    async populateCoinsData() {
        const response = await fetch('arbitrage');
        const data = await response.json();
        this.setState({ coins: data, coinsRender: data[0], loading: false });
    }
}
