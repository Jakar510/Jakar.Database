namespace Jakar.Database;


public static class Migrations
{
    private const            string       MIGRATIONS = "/_migrations";
    public static readonly   IdGenerator  Ids        = new();
    internal static readonly RecordValues Records    = new();


    public static bool HasFlagValue( this ColumnOptions type, ColumnOptions flag ) => ( type & flag ) != 0;


    public static string CreateTableSql<TSelf>()
        where TSelf : class, ITableRecord<TSelf> => SqlTableBuilder<TSelf>.Default;


    public static IEnumerable<MigrationRecord> BuiltIns( IdGenerator ids )
    {
        yield return MigrationRecord.CreateTable(ids.Current);

        yield return MigrationRecord.SetLastModified(ids.Current);

        yield return MigrationRecord.FromEnum<MimeType>(ids.Current);
        yield return MigrationRecord.FromEnum<SupportedLanguage>(ids.Current);
        yield return MigrationRecord.FromEnum<SubscriptionStatus>(ids.Current);
        yield return MigrationRecord.FromEnum<DeviceCategory>(ids.Current);
        yield return MigrationRecord.FromEnum<DevicePlatform>(ids.Current);
        yield return MigrationRecord.FromEnum<DeviceTypes>(ids.Current);
        yield return MigrationRecord.FromEnum<DistanceUnit>(ids.Current);
        yield return MigrationRecord.FromEnum<ProgrammingLanguage>(ids.Current);
        yield return MigrationRecord.FromEnum<Status>(ids.Current);

        yield return FileRecord.CreateTable(ids.Current);

        yield return UserRecord.CreateTable(ids.Current);

        yield return RecoveryCodeRecord.CreateTable(ids.Current);
        yield return UserRecoveryCodeRecord.CreateTable(ids.Current);

        yield return RoleRecord.CreateTable(ids.Current);
        yield return UserRoleRecord.CreateTable(ids.Current);

        yield return GroupRecord.CreateTable(ids.Current);
        yield return UserGroupRecord.CreateTable(ids.Current);

        yield return AddressRecord.CreateTable(ids.Current);
        yield return UserAddressRecord.CreateTable(ids.Current);
    }



    extension( WebApplication self )
    {
        public async ValueTask ApplyMigrations( CancellationToken token = default )
        {
            await using AsyncServiceScope scope       = self.Services.CreateAsyncScope();
            Database                      db          = scope.ServiceProvider.GetRequiredService<Database>();
            MigrationRecord[]             applied     = await All(db, token);
            await using NpgsqlConnection  connection  = await db.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);
            HashSet<MigrationRecord>      pending     = [..Records.Values];
            pending.ExceptWith(applied);


            try
            {
                foreach ( MigrationRecord record in pending.OrderBy(static x => x.MigrationID) ) { await record.Apply(db, token); }

                await transaction.CommitAsync(token);
            }
            catch ( Exception e )
            {
                await transaction.RollbackAsync(token);
                throw new InvalidOperationException("Failed to apply Records", e);
            }
        }
        public async Task RunWithMigrationsAsync( string[]? urls = null, string endpoint = MIGRATIONS, CancellationToken token = default )
        {
            self.TryUseMigrationsEndPoint(endpoint);

            try
            {
                await self.ApplyMigrations(token);
                if ( urls is not null ) { self.UseUrls(urls); }

                await self.StartAsync(token)
                          .ConfigureAwait(false);

                await self.WaitForShutdownAsync(token)
                          .ConfigureAwait(false);
            }
            finally
            {
                await self.DisposeAsync()
                          .ConfigureAwait(false);
            }
        }
        public void TryUseMigrationsEndPoint( string endpoint = MIGRATIONS )
        {
            if ( self.Environment.IsDevelopment() ) { self.UseMigrationsEndPoint(endpoint); }
        }
        public void UseMigrationsEndPoint( string endpoint = MIGRATIONS ) => self.MapGet(endpoint, GetMigrationsAndRenderHtml);
    }



    private static async Task<ContentHttpResult> GetMigrationsAndRenderHtml( [FromServices] Database db, CancellationToken token )
    {
        ReadOnlySpan<MigrationRecord> records = await All(db, token);
        string                        html    = records.CreateHtml();
        return TypedResults.Content(html, "text/html", Encoding.UTF8);
    }


    public static string CreateHtml( this ReadOnlySpan<MigrationRecord> records )
    {
        StringWriter writer = new();
        writer.CreateHtml(records);
        return writer.ToString();
    }



    extension( TextWriter self )
    {
        public void CreateHtml( params ReadOnlySpan<MigrationRecord> records )
        {
            self.WriteLine("<!DOCTYPE html>");
            self.WriteLine("<html lang=\"en\">");
            self.WriteLine("  <head>");
            self.WriteLine("    <meta charset=\"UTF-8\"/>");
            self.WriteLine("    <title>Migration Records</title>");
            self.WriteLine("    <style>");
            self.WriteLine("      body { font-family: system-ui, sans-serif; background: #fafafa; color: #222; margin: 2em; }");
            self.WriteLine("      h2 { border-bottom: 2px solid #ccc; padding-bottom: 0.25em; }");
            self.WriteLine("      table { border-collapse: collapse; width: 100%; margin-top: 1em; }");
            self.WriteLine("      th, td { border: 1px solid #ddd; padding: 6px 10px; }");
            self.WriteLine("      th { background-color: #f4f4f4; text-align: left; }");
            self.WriteLine("      tr:nth-child(even) { background-color: #f9f9f9; }");
            self.WriteLine("      tr:hover { background-color: #f1f1f1; }");
            self.WriteLine("    </style>");
            self.WriteLine("  </head>");
            self.WriteLine("  <body>");
            self.WriteLine("    <h2>Migration Records</h2>");
            self.WriteLine("    <table>");
            self.WriteLine("      <thead>");
            self.WriteLine("        <tr>");
            self.WriteLine("          <th>ID</th>");
            self.WriteLine("          <th>Table</th>");
            self.WriteLine("          <th>Description</th>");
            self.WriteLine("          <th>Applied On</th>");
            self.WriteLine("        </tr>");
            self.WriteLine("      </thead>");
            self.WriteLine("      <tbody>");

            foreach ( ref readonly MigrationRecord migration in records )
            {
                self.Write("        <tr>");
                self.Write("<td>");
                self.Write(migration.MigrationID);
                self.Write("</td><td>");
                self.HtmlEncode(migration.TableID);
                self.Write("</td><td>");
                self.HtmlEncode(migration.Description);
                self.Write("</td><td>");
                self.Write(migration.AppliedOn.ToString("u"));
                self.WriteLine("</td></tr>");
            }

            self.WriteLine("      </tbody>");
            self.WriteLine("    </table>");
            self.WriteLine("  </body>");
            self.WriteLine("</html>");
        }
        private void HtmlEncode( string? value )
        {
            if ( string.IsNullOrEmpty(value) ) { return; }

            ReadOnlySpan<char> span  = value.AsSpan();
            int                start = 0;

            for ( int i = 0; i < span.Length; i++ )
            {
                string? entity = span[i] switch
                                 {
                                     '&'  => "&amp;",
                                     '<'  => "&lt;",
                                     '>'  => "&gt;",
                                     '"'  => "&quot;",
                                     '\'' => "&#39;",
                                     _    => null
                                 };

                if ( entity is not null )
                {
                    if ( i > start ) { self.Write(span.Slice(start, i - start)); }

                    self.Write(entity);
                    start = i + 1;
                }
            }

            if ( start < span.Length ) { self.Write(span.Slice(start)); }
        }
    }



    public static PostgresType ToDbPropertyType( this DbType type ) => type switch
                                                                       {
                                                                           DbType.AnsiString            => PostgresType.String,
                                                                           DbType.Binary                => PostgresType.Binary,
                                                                           DbType.Byte                  => PostgresType.Byte,
                                                                           DbType.Boolean               => PostgresType.Boolean,
                                                                           DbType.Currency              => PostgresType.Decimal,
                                                                           DbType.Date                  => PostgresType.Date,
                                                                           DbType.Decimal               => PostgresType.Decimal,
                                                                           DbType.Double                => PostgresType.Double,
                                                                           DbType.Guid                  => PostgresType.Guid,
                                                                           DbType.Int16                 => PostgresType.Short,
                                                                           DbType.Int32                 => PostgresType.Int,
                                                                           DbType.Int64                 => PostgresType.Long,
                                                                           DbType.SByte                 => PostgresType.SByte,
                                                                           DbType.Single                => PostgresType.Double,
                                                                           DbType.String                => PostgresType.String,
                                                                           DbType.StringFixedLength     => PostgresType.String,
                                                                           DbType.Time                  => PostgresType.Time,
                                                                           DbType.UInt16                => PostgresType.UShort,
                                                                           DbType.UInt32                => PostgresType.UInt,
                                                                           DbType.UInt64                => PostgresType.Long,
                                                                           DbType.VarNumeric            => PostgresType.Decimal,
                                                                           DbType.Xml                   => PostgresType.Xml,
                                                                           DbType.AnsiStringFixedLength => PostgresType.String,
                                                                           DbType.DateTime              => PostgresType.DateTime,
                                                                           DbType.DateTime2             => PostgresType.DateTime,
                                                                           DbType.DateTimeOffset        => PostgresType.DateTimeOffset,
                                                                           DbType.Object                => PostgresType.Json,
                                                                           _                            => throw new OutOfRangeException(type)
                                                                       };
    public static PostgresType? ToDbPropertyType( this DbType? type ) => type?.ToDbPropertyType();


    public static async ValueTask<MigrationRecord[]> All( Database db, CancellationToken token )
    {
        await using NpgsqlConnection connection = await db.ConnectAsync(token);
        await using NpgsqlCommand    cmd        = new(null, connection);
        cmd.Connection  = connection;
        cmd.CommandText = MigrationRecord.SelectSql;
        NpgsqlParameter parameter = cmd.CreateParameter();
        parameter.NpgsqlDbType = NpgsqlDbType.Text;

        await using NpgsqlDataReader reader  = await cmd.ExecuteReaderAsync(token);
        MigrationRecord[]            records = await reader.CreateAsync<MigrationRecord>(Records.Count, token);
        return records;
    }



    extension( MigrationRecord self )
    {
        public async ValueTask Apply( Database db, CancellationToken token )
        {
            await using NpgsqlConnection  connection  = await db.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

            try
            {
                await self.Apply(connection, transaction, token);
                await transaction.CommitAsync(token);
            }
            catch ( Exception e )
            {
                await transaction.RollbackAsync(token);
                throw new SqlException<MigrationRecord>(MigrationRecord.ApplySql, e);
            }
        }
        public async ValueTask Apply( NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken token )
        {
            PostgresParameters parameters = PostgresParameters.Create<MigrationRecord>();
            parameters.Add(nameof(MigrationRecord.MigrationID),    self.MigrationID);
            parameters.Add(nameof(MigrationRecord.Description),    self.Description);
            parameters.Add(nameof(MigrationRecord.TableID),        self.TableID);
            parameters.Add(nameof(MigrationRecord.AppliedOn),      self.AppliedOn);
            parameters.Add(nameof(MigrationRecord.AdditionalData), self.AdditionalData);

            CommandDefinition command = new(MigrationRecord.ApplySql, parameters, transaction, null, null, CommandFlags.Buffered, token);
            await connection.ExecuteAsync(command);
        }
    }



    public sealed class IdGenerator
    {
        public ulong Current => ++field;
        internal IdGenerator() { }
    }



    public sealed class RecordValues : IReadOnlyDictionary<ulong, MigrationRecord>
    {
        private readonly ConcurrentDictionary<ulong, MigrationRecord> __records = new();
        public           int                                          Count => __records.Count;

        public MigrationRecord this[ ulong key ] => __records[key];
        public IEnumerable<ulong>           Keys   => __records.Keys;
        public IEnumerable<MigrationRecord> Values => __records.Values;
        public RecordValues()
        {
            foreach ( MigrationRecord record in BuiltIns(Ids) ) { Add(record); }
        }


        public void Add( MigrationRecord record )
        {
            if ( record.MigrationID == 0 ) { throw new ArgumentOutOfRangeException(nameof(record), "MigrationID cannot be 0"); }

            if ( !__records.TryAdd(record.MigrationID, record) ) { throw new InvalidOperationException($"A record with the MigrationID {record.MigrationID} already exists"); }
        }

        public IEnumerator<KeyValuePair<ulong, MigrationRecord>> GetEnumerator()                                                            => __records.GetEnumerator();
        IEnumerator IEnumerable.                                 GetEnumerator()                                                            => ( (IEnumerable)__records ).GetEnumerator();
        public bool                                              ContainsKey( ulong key )                                                   => __records.ContainsKey(key);
        public bool                                              TryGetValue( ulong key, [MaybeNullWhen(false)] out MigrationRecord value ) => __records.TryGetValue(key, out value);
    }
}
