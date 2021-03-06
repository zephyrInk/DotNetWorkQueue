﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    internal class BuildDequeueCommand
    {
        private readonly CreateDequeueStatement _createDequeueStatement;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;

        public BuildDequeueCommand(ISqlServerMessageQueueTransportOptionsFactory optionsFactory,
            CreateDequeueStatement createDequeueStatement)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => createDequeueStatement, createDequeueStatement);

            _options = new Lazy<SqlServerMessageQueueTransportOptions>(optionsFactory.Create);
            _createDequeueStatement = createDequeueStatement;
        }
        public void BuildCommand(SqlCommand selectCommand, ReceiveMessageQuery<SqlConnection, SqlTransaction> query)
        {
            BuildCommandInternal(selectCommand, query.Transaction, query.MessageId, query.Routes);
        }
        public void BuildCommand(SqlCommand selectCommand, ReceiveMessageQueryAsync<SqlConnection, SqlTransaction> query)
        {
            BuildCommandInternal(selectCommand, query.Transaction, query.MessageId, query.Routes);
        }

        private void BuildCommandInternal(SqlCommand selectCommand, 
             SqlTransaction transaction, IMessageId messageId, List<string> routes)
        {
            selectCommand.Transaction = transaction;
            if (messageId != null && messageId.HasValue)
            {
                selectCommand.CommandText =
                    _createDequeueStatement.GetDeQueueCommand(true, routes);
                selectCommand.Parameters.Add("@QueueID", SqlDbType.BigInt);
                selectCommand.Parameters["@QueueID"].Value = messageId.Id.Value;
            }
            else
            {
                selectCommand.CommandText =
                    _createDequeueStatement.GetDeQueueCommand(false, routes);
            }
            if (_options.Value.EnableRoute && routes != null && routes.Count > 0)
            {
                var routeCounter = 1;
                foreach (var route in routes)
                {
                    selectCommand.Parameters.Add("@Route" + routeCounter, SqlDbType.VarChar);
                    selectCommand.Parameters["@Route" + routeCounter].Value = route;
                    routeCounter++;
                }
            }
        }
    }
}
