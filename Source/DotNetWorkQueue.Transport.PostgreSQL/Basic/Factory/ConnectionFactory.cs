﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Factory
{
    /// <summary>
    /// Creates new instances of <see cref="Connection"/>
    /// </summary>
    internal class ConnectionFactory : IConnectionFactory
    {
        private readonly IConnectionInformation _connectionInfo;
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionFactory" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="options">The options.</param>
        public ConnectionFactory(IConnectionInformation connectionInfo,
            IPostgreSqlMessageQueueTransportOptionsFactory options)
        {
            Guard.NotNull(() => connectionInfo, connectionInfo);
            Guard.NotNull(() => options, options);

            _connectionInfo = connectionInfo;
            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(options.Create);
        }

        /// <summary>
        /// Creates a new instance of <see cref="Connection" />
        /// </summary>
        /// <returns></returns>
        public Connection Create()
        {
            return new Connection(_connectionInfo, _options.Value);
        }
    }
}