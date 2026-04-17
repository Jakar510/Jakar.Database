using ILogger = Microsoft.Extensions.Logging.ILogger;



namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
public class MigrationManager
{
    public const       string                                              MIGRATIONS = "/_migrations";
    protected readonly Database                                            _db;
    private readonly   SortedDictionary<long, Func<long, MigrationRecord>> __migrationFactories     = new(Comparer<long>.Default);
    private readonly   Lock                                                __migrationFactoriesLock = new();
    protected          FrozenSet<MigrationRecord>?                         _records;
    public             long                                                LastMigrationID => __migrationFactories.Count;


    public FrozenSet<MigrationRecord> Records
    {
        get
        {
            FrozenSet<MigrationRecord>? result = Volatile.Read(ref _records);
            if ( result is not null ) { return result; }

            lock ( __migrationFactoriesLock )
            {
                result = __migrationFactories.Select(static pair => pair.Value(pair.Key)).ToFrozenSet();
                return Interlocked.CompareExchange(ref _records, result, null) ?? result;
            }
        }
    }


    public MigrationManager( Database db )
    {
        _db                     = db;
        __migrationFactories[0] = MigrationRecord.SetLastModified;
        __migrationFactories[1] = MigrationRecord.FromEnum<MimeType>;
        __migrationFactories[2] = MigrationRecord.FromEnum<SupportedLanguage>;
        __migrationFactories[3] = MigrationRecord.FromEnum<SubscriptionStatus>;
        __migrationFactories[4] = MigrationRecord.FromEnum<DeviceCategory>;
        __migrationFactories[5] = MigrationRecord.FromEnum<DevicePlatform>;
        __migrationFactories[6] = MigrationRecord.FromEnum<DeviceTypes>;
        __migrationFactories[7] = MigrationRecord.FromEnum<DistanceUnit>;
        __migrationFactories[8] = MigrationRecord.FromEnum<ProgrammingLanguage>;
        __migrationFactories[9] = MigrationRecord.FromEnum<Status>;


        Add(10, ResxRowRecord.MetaData.CreateTable);
        Add(11, FileRecord.MetaData.CreateTable);
        Add(12, UserRecord.MetaData.CreateTable);
        Add(13, RecoveryCodeRecord.MetaData.CreateTable);
        Add(14, UserRecoveryCodeRecord.MetaData.CreateTable);
        Add(15, RoleRecord.MetaData.CreateTable);
        Add(16, UserRoleRecord.MetaData.CreateTable);
        Add(17, GroupRecord.MetaData.CreateTable);
        Add(18, UserGroupRecord.MetaData.CreateTable);
        Add(19, AddressRecord.MetaData.CreateTable);
        Add(20, UserAddressRecord.MetaData.CreateTable);


        Add(ResxRowRecord.MetaData.IndexedColumns.Union(FileRecord.MetaData.IndexedColumns)
                         .Union(UserRecord.MetaData.IndexedColumns)
                         .Union(RecoveryCodeRecord.MetaData.IndexedColumns)
                         .Union(UserRecoveryCodeRecord.MetaData.IndexedColumns)
                         .Union(RoleRecord.MetaData.IndexedColumns)
                         .Union(UserRoleRecord.MetaData.IndexedColumns)
                         .Union(GroupRecord.MetaData.IndexedColumns)
                         .Union(UserGroupRecord.MetaData.IndexedColumns)
                         .Union(AddressRecord.MetaData.IndexedColumns)
                         .Union(UserAddressRecord.MetaData.IndexedColumns));
    }


    public MigrationManager Add<TEnumerator>( ValueEnumerable<TEnumerator, Func<long, MigrationRecord>> enumerable )
        where TEnumerator : struct, IValueEnumerator<Func<long, MigrationRecord>>, allows ref struct
    {
        Interlocked.Exchange(ref _records, null);

        lock ( __migrationFactoriesLock )
        {
            foreach ( Func<long, MigrationRecord> func in enumerable ) { AddInternal(LastMigrationID, func); }
        }

        return this;
    }
    public MigrationManager Add( long migrationID, Func<long, MigrationRecord> func )
    {
        Interlocked.Exchange(ref _records, null);
        lock ( __migrationFactoriesLock ) { AddInternal(migrationID, func); }

        return this;
    }
    protected internal MigrationManager AddInternal( long migrationID, Func<long, MigrationRecord> func )
    {
        if ( __migrationFactories.Values.Contains(func) ) { throw new InvalidOperationException("migration factory method has already been added"); }

        __migrationFactories.Add(migrationID, func);
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
        await using DbConnectionContext context = await _db.ConnectAsync(token);
        return await AllMigrations(context, token);
    }
    public virtual async ValueTask<ImmutableArray<MigrationRecord>> AllMigrations( DbConnectionContext context, CancellationToken token )
    {
        ImmutableArray<MigrationRecord> records = await context.ExecuteAsync<MigrationRecord>(MigrationRecord.SelectSql, token).ToImmutableArray(__migrationFactories.Count, token);
        return records;
    }


    public async ValueTask ApplyMigrations( ILogger logger, CancellationToken token = default )
    {
        await using DbConnectionContext context = await _db.ConnectAsync(token);

        await context.EnsureTableExistsAsync<MigrationRecord>(token);

        await context.StartTransactionAsync(_db.TransactionIsolationLevel, token);

        try
        {
            ImmutableArray<MigrationRecord> applied    = await AllMigrations(context, token);
            HashSet<long>                   appliedIds = applied.Select(static x => x.MigrationID).ToHashSet();
            FrozenSet<MigrationRecord>      records    = Records;

            foreach ( MigrationRecord record in records.OrderBy(static x => x.MigrationID) )
            {
                if ( appliedIds.Contains(record.MigrationID) )
                {
                    logger.LogDebug("Migration {MigrationID} has already been applied; skipping.", record.MigrationID);
                    continue;
                }

                await Apply(context, record, token);
            }

            await context.CommitAsync(token);
        }
        catch ( Exception e )
        {
            logger.LogCritical(e, "{Source} has failed: {Message}", nameof(ApplyMigrations), e.Message);
            await context.RollbackAsync(token);
        }
    }
    public virtual async Task Apply( DbConnectionContext context, MigrationRecord self, CancellationToken token )
    {
        try
        {
            await context.ExecuteNonQueryAsync(self.SQL, token);
            self.AppliedOn = DateTimeOffset.UtcNow;
        }
        catch ( Exception e ) { throw new DbSqlException(self.SQL, e); }

        SqlCommand applySql = self.ApplySql();

        try { await context.ExecuteNonQueryAsync(applySql, token); }
        catch ( Exception e ) { throw new DbSqlException(applySql, e) { RollbackID = self.RollbackID }; }
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
