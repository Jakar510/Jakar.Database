// Jakar.Extensions :: Jakar.Database
// 09/10/2023  10:12 PM

namespace Jakar.Database;


public interface IUserSubscription : IUniqueID
{
    public DateTimeOffset? SubscriptionExpires { get; }
}



public abstract record UserSubscription<TSelf> : OwnedTableRecord<TSelf>, IUserSubscription
    where TSelf : UserSubscription<TSelf>, ITableRecord<TSelf>
{
    public DateTimeOffset? SubscriptionExpires { get; init; }


    protected UserSubscription( DateTimeOffset? subscriptionExpires, RecordID<TSelf> ID, RecordID<UserRecord> UserID, DateTimeOffset DateCreated, DateTimeOffset? LastModified = null ) : base(in UserID, in ID, in DateCreated, in LastModified) => SubscriptionExpires = subscriptionExpires;


    [Pure] public override PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(SubscriptionExpires), SubscriptionExpires);
        return parameters;
    }
}
