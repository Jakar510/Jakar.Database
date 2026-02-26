namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public class MigrationManager
{
    public const       string                                              MIGRATIONS           = "/_migrations";
    private readonly   SortedDictionary<long, Func<long, MigrationRecord>> __migrationFactories = new(Comparer<long>.Default);
    protected readonly Database                                            _db;
    protected          FrozenSet<MigrationRecord>?                         _records;
    internal static    long                                                MigrationID => Interlocked.Add(ref field, 1);


    public FrozenSet<MigrationRecord> Records
    {
        get
        {
            FrozenSet<MigrationRecord>? result = Interlocked.CompareExchange(ref _records, null, null);
            if ( result is not null ) { return result; }

            result = __migrationFactories.Select(static pair => pair.Value(pair.Key)).ToFrozenSet();
            Interlocked.Exchange(ref _records, result);
            return result;
        }
    }


    public MigrationManager( Database db )
    {
        _db                               = db;
        __migrationFactories[MigrationID] = MigrationRecord.SetLastModified;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<MimeType>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<SupportedLanguage>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<SubscriptionStatus>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<DeviceCategory>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<DevicePlatform>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<DeviceTypes>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<DistanceUnit>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<ProgrammingLanguage>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<Status>;

        Add<ResxRowRecord>();
        Add<FileRecord>();
        Add<UserRecord>();
        Add<RecoveryCodeRecord>();
        Add<UserRecoveryCodeRecord>();
        Add<RoleRecord>();
        Add<UserRoleRecord>();
        Add<GroupRecord>();
        Add<UserGroupRecord>();
        Add<AddressRecord>();
        Add<UserAddressRecord>();
    }


    public MigrationManager Add<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Add(TSelf.MetaData);
    public MigrationManager Add( ITableMetaData data ) => Add(data.CreateTable).Add(data.IndexedColumns);
    public MigrationManager Add( [HandlesResourceDisposal] in PooledArray<Func<long, MigrationRecord>> funcs )
    {
        using ( funcs ) { Add(funcs.Span); }

        return this;
    }
    public MigrationManager Add( params ReadOnlySpan<Func<long, MigrationRecord>> span )
    {
        Interlocked.Exchange(ref _records, null);
        foreach ( Func<long, MigrationRecord> func in span ) { AddInternal(func); }

        return this;
    }
    public MigrationManager Add( Func<long, MigrationRecord> func )
    {
        Interlocked.Exchange(ref _records, null);
        return AddInternal(func);
    }
    public MigrationManager Add<TEnumerator>( ValueEnumerable<TEnumerator, Func<long, MigrationRecord>> enumerable )
        where TEnumerator : struct, IValueEnumerator<Func<long, MigrationRecord>>, allows ref struct
    {
        Interlocked.Exchange(ref _records, null);
        foreach ( Func<long, MigrationRecord> func in enumerable ) { AddInternal(func); }

        return this;
    }


    protected MigrationManager AddInternal( Func<long, MigrationRecord> func )
    {
        if ( __migrationFactories.Values.Contains(func) ) { throw new InvalidOperationException("migration factory method has already been added"); }

        __migrationFactories.Add(MigrationID, func);
        return this;
    }


    [Conditional("DEBUG")] public static void PrintSql( string sql, [CallerArgumentExpression(nameof(sql))] string variableName = EMPTY )
    {
        const string BOUNDARY = "================================";
        Console.WriteLine(BOUNDARY);
        Console.WriteLine();
        Console.WriteLine(variableName);
        Console.WriteLine();
        Console.WriteLine(sql);
        Console.WriteLine();
        Console.WriteLine(BOUNDARY);
    }


    public virtual async ValueTask<ContentHttpResult> AppliedMigrations( CancellationToken token )
    {
        ImmutableArray<MigrationRecord> records = await AllMigrations(token);
        string                          html    = CreateHtml(in records);
        return TypedResults.Content(html, "text/html", Encoding.UTF8);
    }


    public virtual async ValueTask<ImmutableArray<MigrationRecord>> AllMigrations( CancellationToken token )
    {
        await using NpgsqlConnection connection = await _db.ConnectAsync(token);
        return await AllMigrations(connection, null, token);
    }
    public virtual async ValueTask<ImmutableArray<MigrationRecord>> AllMigrations( NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken token )
    {
        SqlCommand<MigrationRecord> command = MigrationRecord.SelectSql;

        ImmutableArray<MigrationRecord> records = await command.ExecuteAsync(connection, transaction, token).ToImmutableArray(__migrationFactories.Count, token);

        return records;
    }


    public async ValueTask ApplyMigrations( CancellationToken token = default )
    {
        await using NpgsqlConnection connection = await _db.ConnectAsync(token);

        SqlCommand<MigrationRecord> command = MigrationRecord.TryCreateSql;
        await command.ExecuteNonQueryAsync(connection, null, token);

        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

        try
        {
            ImmutableArray<MigrationRecord> applied = await AllMigrations(connection, transaction, token);
            HashSet<MigrationRecord>        pending = new(Records);
            pending.ExceptWith(applied);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach ( MigrationRecord record in pending.OrderBy(static x => x.MigrationID) ) { await Apply(connection, transaction, record, token); }

            await transaction.CommitAsync(token);
        }

        // catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
        // {
        //     await transaction.RollbackAsync(e.RollbackID, token);
        //     throw;
        // }
        catch ( Exception )
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }
    public virtual async Task Apply( NpgsqlConnection connection, NpgsqlTransaction transaction, MigrationRecord self, CancellationToken token )
    {
        try
        {
            SqlCommand<MigrationRecord> command = self.SQL;
            await command.ExecuteNonQueryAsync(connection, transaction, token);
            self.AppliedOn = DateTimeOffset.UtcNow;
        }
        catch ( Exception e ) { throw new DbSqlException(self.SQL, e); }

        PostgresParameters parameters = PostgresParameters.Create<MigrationRecord>();
        parameters.Add(nameof(MigrationRecord.MigrationID), self.MigrationID);
        parameters.Add(nameof(MigrationRecord.Description), self.Description);
        parameters.Add(nameof(MigrationRecord.ReferenceID), self.ReferenceID);
        parameters.Add(nameof(MigrationRecord.AppliedOn),   self.AppliedOn);

        try
        {
            SqlCommand<MigrationRecord> migrationRecord = new(MigrationRecord.ApplySql, parameters);
            await migrationRecord.ExecuteNonQueryAsync(connection, transaction, token);
            await transaction.SaveAsync(self.RollbackID, token);
        }
        catch ( Exception e ) { throw new DbSqlException(MigrationRecord.ApplySql, e, parameters) { RollbackID = self.RollbackID }; }
    }


    public virtual string CreateHtml( ref readonly ImmutableArray<MigrationRecord> records )
    {
        StringWriter writer = new();
        CreateHtml(writer, in records);
        return writer.ToString();
    }
    public virtual void CreateHtml( TextWriter self, ref readonly ImmutableArray<MigrationRecord> records )
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

        foreach ( ref readonly MigrationRecord migration in records.AsSpan() )
        {
            self.Write("        <tr>");
            self.Write("<td>");
            self.Write(migration.MigrationID);
            self.Write("</td><td>");
            HtmlEncode(self, migration.ReferenceID);
            self.Write("</td><td>");
            HtmlEncode(self, migration.Description);
            self.Write("</td><td>");
            self.Write(migration.AppliedOn?.ToString("u"));
            self.WriteLine("</td></tr>");
        }

        self.WriteLine("      </tbody>");
        self.WriteLine("    </table>");
        self.WriteLine("  </body>");
        self.WriteLine("</html>");
    }
    protected virtual void HtmlEncode( TextWriter self, string? value )
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

        if ( start < span.Length ) { self.Write(span[start..]); }
    }
}
