namespace MassTransit.ServiceBus.SubscriptionsManager
{
    using System;
    using Messages;
    using Subscriptions;

    public class SubscriptionServiceBus : ServiceBus
    {
        private ISubscriptionRepository _repository;


        public SubscriptionServiceBus(IReadWriteEndpoint endpoint, ISubscriptionStorage subscriptionStorage, ISubscriptionRepository repository)
            : base(endpoint, subscriptionStorage)
        {
            _repository = repository;
            this.Consumer<SubscriptionMessage>().Subscribe(OnSubscriptionMessageReceived);
            this.Consumer<RequestCacheUpdate>().Subscribe(OnRequestCacheUpdate);
            this.Consumer<RequestCacheUpdateForMessage>().Subscribe(OnRequestSubscribersForMessage);
        }


        public void OnSubscriptionMessageReceived(MessageContext<SubscriptionMessage> ctx)
        {
            // Add / Remove Subscription to Repository
            switch(ctx.Message.ChangeType)
            {
                case SubscriptionMessage.SubscriptionChangeType.Add:
                    _repository.Add(SubscriptionMapper.MapFrom(ctx.Message));
                    break;
                case SubscriptionMessage.SubscriptionChangeType.Remove:
                    _repository.Deactivate(SubscriptionMapper.MapFrom(ctx.Message));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Publish it so others get it?
            this.Publish(ctx.Message);
        }

        public void OnRequestCacheUpdate(MessageContext<RequestCacheUpdate> ctx)
        {
            //return a complete list of SubscriptionMessages
            ctx.Reply(new CacheUpdateResponse(SubscriptionMapper.MapFrom(_repository.List())));
        }


        public void OnRequestSubscribersForMessage(MessageContext<RequestCacheUpdateForMessage> ctx)
        {
            //return a complete list of SubscriptionMessages
            ctx.Reply(new CacheUpdateResponse(SubscriptionMapper.MapFrom(_repository.List(ctx.Message.Message))));
        }
    }
}