// Jakar.Extensions :: Jakar.Database.Resx
// 10/07/2022  9:53 PM

namespace Jakar.Database.Resx;


public interface IResxCollection : IReadOnlyCollection<ResxRowRecord>
{
    public ResxSet GetSet( in SupportedLanguage language );


    public ValueTask          Init( IConnectableDb        db,         IResxProvider     provider,    CancellationToken token                             = default );
    public ValueTask          Init( NpgsqlConnection      connection, NpgsqlTransaction transaction, IResxProvider     provider, CancellationToken token = default );
    public ValueTask<ResxSet> GetSetAsync( IConnectableDb db,         IResxProvider     provider,    SupportedLanguage language, CancellationToken token = default );
}
