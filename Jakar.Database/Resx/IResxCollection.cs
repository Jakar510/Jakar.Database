// Jakar.Extensions :: Jakar.Database.Resx
// 10/07/2022  9:53 PM

namespace Jakar.Database;


public interface IResxCollection : IReadOnlyCollection<ResxRowRecord>
{
    public ResxSet GetSet( in SupportedLanguage language );


    public ValueTask          Init( IConnectableDb        db,         IResxProvider     provider,    CancellationToken token                             = default );
    public ValueTask          Init( DbConnectionContext context, IResxProvider     provider, CancellationToken token = default );
    public ValueTask<ResxSet> GetSetAsync( IConnectableDb db,         IResxProvider     provider,    SupportedLanguage language, CancellationToken token = default );
}
