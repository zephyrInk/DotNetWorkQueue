﻿using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Connection settings for Rpc
    /// </summary>
    public class SqlServerRpcConnection: BaseRpcConnection
    {
        private readonly string _sendConnection;
        private readonly string _sendQueue;
        private readonly string _receiveConnection;
        private readonly string _receiveQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerRpcConnection"/> class.
        /// </summary>
        /// <param name="sendConnection">The send connection.</param>
        /// <param name="sendQueue">The send queue.</param>
        /// <param name="receiveConnection">The receive connection.</param>
        /// <param name="receiveQueue">The receive queue.</param>
        public SqlServerRpcConnection(string sendConnection, string sendQueue, string receiveConnection, string receiveQueue)
        {
            Guard.NotNullOrEmpty(() => sendConnection, sendConnection);
            Guard.NotNullOrEmpty(() => sendQueue, sendQueue);
            Guard.NotNullOrEmpty(() => receiveConnection, receiveConnection);
            Guard.NotNullOrEmpty(() => receiveQueue, receiveQueue);

            _sendConnection = sendConnection;
            _sendQueue = sendQueue;
            _receiveConnection = receiveConnection;
            _receiveQueue = receiveQueue;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="connectionType">Type of the connection.</param>
        /// <returns></returns>
        public override IConnectionInformation GetConnection(ConnectionTypes connectionType)
        {
            switch (connectionType)
            {
                case ConnectionTypes.NotSpecified:
                case ConnectionTypes.Send:
                    return new SqlConnectionInformation(_sendQueue, _sendConnection);
                case ConnectionTypes.Receive:
                    return new SqlConnectionInformation(_receiveQueue, _receiveConnection);
            }
            throw new DotNetWorkQueueException($"unhandled type {connectionType}");
        }
    }
}
