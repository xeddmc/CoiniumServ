﻿/*
 *   CoiniumServ - crypto currency pool software - https://github.com/CoiniumServ/CoiniumServ
 *   Copyright (C) 2013 - 2014, Coinium Project - http://www.coinium.org
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

// stratum server uses json-rpc 2.0 (over raw sockets) & json-rpc.net (http://jsonrpc2.codeplex.com/)
// classic server handles getwork & getblocktemplate miners over http.

using Coinium.Common.Attributes;
using Coinium.Core.Mining.Miner;
using Coinium.Core.Server.Config;
using Coinium.Core.Server.Stratum.Config;
using Coinium.Net.Server.Sockets;
using Serilog;

namespace Coinium.Core.Server.Stratum
{
    /// <summary>
    /// Stratum protocol server implementation.
    /// </summary>
    [DefaultInstance]
    public class StratumServer : SocketServer, IMiningServer
    {

        public IServerConfig Config { get; private set; }

        private readonly IMinerManager _minerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="StratumServer"/> class.
        /// </summary>
        /// <param name="minerManager">The miner manager.</param>
        public StratumServer(IMinerManager minerManager)
        {
            _minerManager = minerManager;
        }

        /// <summary>
        /// Initializes the specified pool.
        /// </summary>
        /// <param name="pool">The pool.</param>
        /// <param name="config">The configuration.</param>
        public void Initialize(IServerConfig config)
        {
            this.Config = config;

            this.BindIP = ((IStratumServerConfig)config).BindIp;
            this.Port = config.Port;

            this.OnConnect += Stratum_OnConnect;
            this.OnDisconnect += Stratum_OnDisconnect;
            this.DataReceived += Stratum_DataReceived;
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <returns></returns>
        public override bool Start()
        {
            var success = this.Listen(this.BindIP, this.Port);

            if(success)
                Log.Information("Stratum server listening on {0}:{1}", this.BindIP, this.Port);            

            return true;
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        /// <returns></returns>
        public override bool Stop()
        {
            return true;
        }

        /// <summary>
        /// Client on connectin handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Stratum_OnConnect(object sender, ConnectionEventArgs e)
        {
            Log.Verbose("Stratum client connected: {0}", e.Connection.ToString());

            var miner = _minerManager.Create<StratumMiner>(e.Connection);
            e.Connection.Client = miner;           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Stratum_OnDisconnect(object sender, ConnectionEventArgs e)
        {
            Log.Verbose("Stratum client disconnected: {0}", e.Connection.ToString());
        }

        /// <summary>
        /// Client data recieve handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Stratum_DataReceived(object sender, ConnectionDataEventArgs e)
        {
            var connection = (Connection)e.Connection;
            ((StratumMiner)connection.Client).Parse(e);
        }        
    }
}
