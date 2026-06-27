// Jakar.Extensions :: Jakar.Extensions
// 05/17/2022  4:16 PM

using ILogger = Microsoft.Extensions.Logging.ILogger;



namespace Jakar.Database;


public abstract class Service : BaseClass, IAsyncDisposable, IValidator
{
    private readonly SynchronizedValue<bool> __isAlive = new(false);
    public           string                  ClassName { get; }
    public           string                  FullName  { get; }


    public virtual bool IsAlive
    {
        get => __isAlive.Value;
        protected set
        {
            __isAlive.Value = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsValid));
        }
    }
    public virtual bool IsValid => IsAlive;


    protected Service()
    {
        Type type = GetType();
        ClassName = type.Name;
        FullName  = type.AssemblyQualifiedName ?? type.FullName ?? ClassName;
    }


    public virtual ValueTask DisposeAsync() => default;


    public static Task Delay( in double   days,    in CancellationToken token ) => Delay(TimeSpan.FromDays(days),       token);
    public static Task Delay( in float    minutes, in CancellationToken token ) => Delay(TimeSpan.FromMinutes(minutes), token);
    public static Task Delay( in long     seconds, in CancellationToken token ) => Delay(TimeSpan.FromSeconds(seconds), token);
    public static Task Delay( in int      ms,      in CancellationToken token ) => Delay(TimeSpan.FromMilliseconds(ms), token);
    public static Task Delay( in TimeSpan delay,   in CancellationToken token ) => delay.Delay(token);


    [StackTraceHidden] [DoesNotReturn] protected virtual void ThrowDisabled( Exception? inner = null, [CallerMemberName] string? caller = null ) => throw new ApiDisabledException($"{ClassName}.{caller}", inner);
    [StackTraceHidden] [DoesNotReturn] protected         void ThrowDisposed( Exception? inner = null, [CallerMemberName] string? caller = null ) => throw new ObjectDisposedException($"{ClassName}.{caller}", inner);
}



public abstract class HostedService : Service, IHostedService
{
    public abstract Task StartAsync( CancellationToken token );
    public abstract Task StopAsync( CancellationToken  token );
}



public static class HostedServiceExtensions
{
    public static ServiceThread StartInThread( this IHostedService service, ILogger<ServiceThread> logger, CancellationToken token )
    {
        ServiceThread thread = new(service, logger, token);
        thread.Start();
        return thread;
    }



    public sealed class ServiceThread : Service
    {
        private readonly CancellationToken        __token;
        private readonly IHostedService           __service;
        private readonly ILogger                  __logger;
        private readonly Thread                   __thread;
        private          CancellationTokenSource? __source;
        private volatile bool                     __started;


        public ServiceThread( IHostedService service, ILoggerFactory factory, CancellationToken token = default ) : this(service, factory.CreateLogger<ServiceThread>(), token) { }
        public ServiceThread( IHostedService service, ILogger logger, CancellationToken token = default )
        {
            __service = service;
            __logger  = logger;
            __token   = token;

            __thread = new Thread(ThreadStart)
                       {
                           Name         = $"{nameof(ServiceThread)}.{service.GetType().Name}",
                           IsBackground = true
                       };
        }
        public bool Stop( in TimeSpan timeout )
        {
            if ( !__started ) { return true; }

            __source?.Cancel();
            bool joined = __thread.Join(timeout);

            if ( joined )
            {
                __source?.Dispose();
                __source = null;
            }

            return joined;
        }
        public override ValueTask DisposeAsync()
        {
            Stop();
            return default;
        }


        public void Start()
        {
            if ( __started ) { return; }

            // Create the cancellation source before the thread starts so a Stop() that races Start() cannot observe a null source.
            __started = true;
            __source  = CancellationTokenSource.CreateLinkedTokenSource(__token);
            IsAlive   = true;
            __thread.Start();
        }


        public void Stop()
        {
            if ( !__started ) { return; }

            __source?.Cancel();
            __thread.Join();
            __source?.Dispose();
            __source = null;
        }

        // Runs synchronously on the dedicated thread and blocks until the hosted service stops, so Stop()'s Join() actually waits for completion (an `async void` body would return at the first await and the thread would exit immediately).
        private void ThreadStart()
        {
            CancellationTokenSource? source = __source;
            if ( source is null ) { return; }

            try
            {
                try { __service.StartAsync(source.Token).GetAwaiter().GetResult(); }
                finally { __service.StopAsync(CancellationToken.None).GetAwaiter().GetResult(); }
            }
            catch ( OperationCanceledException ) { }
            catch ( Exception e ) { DbLog.ServiceError(__logger, e, this, __service); }
            finally
            {
                DbLog.ServiceStopped(__logger, this, __service, __token);
                IsAlive = false;
            }
        }
    }
}
