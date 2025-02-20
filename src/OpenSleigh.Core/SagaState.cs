﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using OpenSleigh.Core.Messaging;
using OpenSleigh.Core.Persistence;

namespace OpenSleigh.Core
{
    public abstract record SagaState
    {
        private readonly List<IMessage> _outbox = new();
                
        private HashSet<Guid> _processedMessages = new();
        
        protected SagaState(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; init; }

        [JsonInclude]
        public bool IsCompleted { get; private set; }

        [JsonInclude]
        public IReadOnlyCollection<Guid> ProcessedMessagesIds
        {
            get { return _processedMessages; }
            private set { _processedMessages = new HashSet<Guid>(value); }
        }

        [JsonIgnore]
        public IReadOnlyCollection<IMessage> Outbox => _outbox;

        public void SetAsProcessed<TM>(TM message) where TM : IMessage
        {
            if (message == null) 
                throw new ArgumentNullException(nameof(message));

            // the Message correlation id should match the Saga State because it is used to recover the State when processing the Message
            if (this.Id != message.CorrelationId)
                throw new ArgumentException($"invalid message correlation id", nameof(message));
            
            _processedMessages.Add(message.Id);
        }

        public bool CheckWasProcessed<TM>(TM message) where TM : IMessage
        {
            if (message == null) 
                throw new ArgumentNullException(nameof(message));
            return _processedMessages.Contains(message.Id);
        }        

        public void MarkAsCompleted() => this.IsCompleted = true;

        internal void AddToOutbox<TM>(TM message) where TM : IMessage
        {
            if (message == null) 
                throw new ArgumentNullException(nameof(message));
            _outbox.Add(message);
        }

        internal async Task PersistOutboxAsync(IOutboxRepository outboxRepository, CancellationToken cancellationToken = default)
        {
            foreach (var message in _outbox)
                await outboxRepository.AppendAsync(message, cancellationToken);
            _outbox.Clear();
        }
    }
}