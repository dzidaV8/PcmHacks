﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flash411
{
    class Query<T>
    {
        private Device device;
        private Func<Message> generator;
        private Func<Message, Response<T>> filter;
        private ILogger logger;

        public Query(Device device, Func<Message> generator, Func<Message, Response<T>> filter, ILogger logger)
        {
            this.device = device;
            this.generator = generator;
            this.filter = filter;
            this.logger = logger;
        }

        public async Task<Response<T>> Execute()
        {
            this.device.ClearMessageQueue();

            Message request = this.generator();

            bool success = false;
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                success = await this.device.SendMessage(request);

                if (success)
                {
                    break;
                }

                this.logger.AddDebugMessage("Send failed. Attempt #" + attempt.ToString());
            }

            if (!success)
            {
                this.logger.AddDebugMessage("Too many send failures, giving up.");
                return Response.Create(ResponseStatus.Error, default(T));
            }

            for(int attempt = 1; attempt <= 5; attempt++)
            {
                Message received = await this.device.ReceiveMessage();
                Response<T> result = this.filter(received);
                if (result.Status == ResponseStatus.Success)
                {
                    return result;
                }

                this.logger.AddDebugMessage(
                    string.Format(
                        "Send failed. Attempt #{0}, status {1}.", 
                        attempt.ToString(),
                        result.Status));
            }

            return Response.Create(ResponseStatus.Error, default(T));
        }
    }
}