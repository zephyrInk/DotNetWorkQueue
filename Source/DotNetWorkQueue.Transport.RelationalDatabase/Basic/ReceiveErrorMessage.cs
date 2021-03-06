﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;
using QueueDelay = DotNetWorkQueue.Queue.QueueDelay;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <inheritdoc />
    public class ReceiveErrorMessage : IReceiveMessagesError
    {
        #region Member Level Variables
        private readonly ILog _log;
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IQueryHandler<GetErrorRetryCountQuery, int> _queryErrorRetryCount;
        private readonly ICommandHandler<SetErrorCountCommand> _commandSetErrorCount;
        private readonly ICommandHandler<MoveRecordToErrorQueueCommand> _commandMoveRecord;
        private readonly IIncreaseQueueDelay _headers; 

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveErrorMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="queryErrorRetryCount">The query error retry count.</param>
        /// <param name="commandSetErrorCount">The command set error count.</param>
        /// <param name="commandMoveRecord">The command move record.</param>
        /// <param name="log">The log.</param>
        /// <param name="headers">The headers.</param>
        public ReceiveErrorMessage(QueueConsumerConfiguration configuration,
            IQueryHandler<GetErrorRetryCountQuery, int> queryErrorRetryCount, ICommandHandler<SetErrorCountCommand> commandSetErrorCount,
            ICommandHandler<MoveRecordToErrorQueueCommand> commandMoveRecord,
            ILogFactory log,
            IIncreaseQueueDelay headers)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => queryErrorRetryCount, queryErrorRetryCount);
            Guard.NotNull(() => commandSetErrorCount, commandSetErrorCount);
            Guard.NotNull(() => commandMoveRecord, commandMoveRecord);
            Guard.NotNull(() => log, log);

            _configuration = configuration;
            _queryErrorRetryCount = queryErrorRetryCount;
            _commandSetErrorCount = commandSetErrorCount;
            _commandMoveRecord = commandMoveRecord;
            _log = log.Create();
            _headers = headers;
        }
        #endregion

        #region IReceiveMessagesError
        /// <inheritdoc />
        public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            //message failed to process
            if (context.MessageId == null || !context.MessageId.HasValue) return ReceiveMessagesErrorResult.NoActionPossible;

            var info =
                _configuration.TransportConfiguration.RetryDelayBehavior.GetRetryAmount(exception);
            string exceptionType = null;
            if (info.ExceptionType != null)
            {
                exceptionType = info.ExceptionType.ToString();
            }

            var bSendErrorQueue = false;
            if (string.IsNullOrEmpty(exceptionType) || info.MaxRetries <= 0)
            {
                bSendErrorQueue = true;
            }
            else
            {
                //determine how many times this exception has been seen for this message
                var retries =
                    _queryErrorRetryCount.Handle(
                        new GetErrorRetryCountQuery(exceptionType,
                            (long) context.MessageId.Id.Value));
                if (retries >= info.MaxRetries)
                {
                    bSendErrorQueue = true;
                }
                else
                {
                    context.Set(_headers.QueueDelay, new QueueDelay(info.Times[retries]));
                    //note zero based index - use the current count not count +1
                    _commandSetErrorCount.Handle(
                        new SetErrorCountCommand(
                            exceptionType, (long) context.MessageId.Id.Value));
                }
            }

            if (!bSendErrorQueue) return ReceiveMessagesErrorResult.Retry;

            _commandMoveRecord.Handle(
                new MoveRecordToErrorQueueCommand(exception, (long)context.MessageId.Id.Value, context));
            //we are done doing any processing - remove the messageID to block other actions
            context.MessageId = null;
            _log.ErrorException("Message with ID {0} has failed and has been moved to the error queue", exception,
                message.MessageId);
            return ReceiveMessagesErrorResult.Error;
        }
        #endregion
    }
}
